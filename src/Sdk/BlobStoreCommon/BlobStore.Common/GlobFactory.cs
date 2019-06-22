using DotNet.Globbing;
using GitHub.Services.Common;
using GitHub.Services.Content.Common;
using GitHub.Services.Content.Common.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHub.Services.BlobStore.Common
{
    /// <summary>
    /// This class encapsulates all the globbing functionality provided for .artifactignore file support.
    /// globbing semantics are similar to: https://git-scm.com/docs/gitignore.
    /// The code below leverages the OSS globbing library: https://github.com/dazinator/DotNet.Glob.
    /// The reason for choosing this library is the higher perf compared to Regex: https://github.com/dazinator/DotNet.Glob/wiki/Benchmarks-(vs-Compiled-Regex).
    /// 
    /// Developer Note:
    /// foo             -> evaluates to a file or a folder.
    /// foo/            -> evaluates to a folder only.
    /// /foo            -> ties the scope of search to the current folder.
    /// #               -> used for adding comments, will be skipped.
    ///                 -> empty lines will simply be skipped.
    /// **              -> used to search across several folder hierarchies.
    /// !               -> negates the operation.
    /// ?,*             -> carry the regular regex semantics.
    /// [0-9]           -> can be used to specify char ranges.
    /// [!5]            -> can be used to exclude certain char ranges.
    /// *foo            -> can be used to look up any file name that ends with foo.
    /// *foo/           -> can be used to look up any folder name that ends with foo.
    /// **/foo/**/bar   -> ** can be used to look deep down into the directory structure. Depth allowed is tied to the underlying filesystem.
    ///
    /// CASE SENSITIVITY Semantics:
    /// 
    /// For *NIX/MAC systems - the case sensitivity is honoured. (DEFAULT).
    /// For Windows systems - the case sensitivity will be ignored.
    ///
    /// PATHING semantics:
    ///
    /// For *NIX/MAC systems - the '/' character is identified as the directory separator.
    /// For Windows systems - the '\' character is identified as the directory separator.
    ///
    /// The .artifactignore file passed in is expected to provide the correct pathing semantics corresponding to the underlying file system/OS platform.
    /// Incorrect pathing may generate non-deterministic results.
    ///
    /// LONG PATHS:
    ///
    /// Depending on the depth of the directory structure it is possible that the caller may encouter 0X80010135 PATH TOO LONG errors. This is however,
    /// orthogonal to the globbing functionality. The end-user may choose to enable long paths, and with that the globbing library is expected to function
    /// as expected.
    ///
    /// SPECIAL Characters:
    ///
    /// On Windows, special file characters: \, /, :, *, ?, ", |, &lt;, &gt; are not allowed for file / folder names.
    /// On *NIX/MAC, special file characters: \0, / are not allowed.
    ///
    /// SCALABILITY notes:
    ///
    /// The class makes heavy use of collections inorder to manage the list of files/folders that fall under the given source directory.
    /// DEVNOTE: It is possible that for a large drop with millions of files, we may run into OOM issues. This is currently not addressed in this code.
    /// These collections are scoped to the PerformGlobbing() method. Calling this method 'N' times with the same input (i.e., source directory) will
    /// produce the same result i.e., the operation is idempotent.
    /// </summary>
    public class GlobFactory
    {
        #region Fields.

        // Const.
        // Note: The file name shall not contain file extensions for e.g., .artifactignore.txt is not a valid file and will not take effect.
        private const string ArtifactIgnoreFileName = ".artifactignore";

        // Glob control knobs.
        private StringComparer defaultStringComparer;
        private GlobOptions defaultGlobOptions = GlobOptions.Default; //Case sensitivity is ON by default. 

        // Glob control flow.
        private bool globUsingArtifactIgnoreFile;
        private bool isWindowsPlatform;
        private string artifactIgnoreFilePath;        

        // Collections.
        private HashSet<string> files;
        private HashSet<string> directories;
        private HashSet<string> sourceDirectoryItems;
        private HashSet<string> nonEmptyDirectories;
        private HashSet<string> emptyDirectories;

        // The effective set of files to be ignored post globbing.
        private HashSet<string>  ignoreFileSet; 

        // Glob collections.
        private HashSet<string> ignoreFileGlobSet;
        private HashSet<string> doNotIgnoreFileGlobSet;

        #endregion

        #region Properties

        // Init in constructor.
        public IFileSystem TargetFileSystem { get; }

        private IAppTraceSource Tracer { get; }

        private bool GlobEmptyDirectories { get; }

        #endregion

        #region Collections

        /// <summary>
        /// Glob patterns and the corresponding monikers for DOS based systems.
        /// </summary>
        private readonly Dictionary<string, string> GlobPatternsWindows = new Dictionary<string, string>()
        {
            {"Comment", @"^#"},
            {"DoNotIgnoreWithEscaping", @"^!{1}[\\]{1}(!|#)"},
            {"DoNotIgnore", @"^!{1}"},
            {"IgnoreWithEscaping", @"^\\{1}(!|#)"},
            {"Ignore", @"(.*?)"},
        };

        /// <summary>
        /// Glob patterns and the corresponding monikers for *NIX based systems.
        /// </summary>
        private readonly Dictionary<string, string> GlobPatternsNix = new Dictionary<string, string>()
        {
            {"Comment", @"^#"},
            {"DoNotIgnoreWithEscaping", @"^!{1}[\/]{1}(!|#)"},
            {"DoNotIgnore", @"^!{1}"},
            {"IgnoreWithEscaping", @"^\/{1}(!|#)"},
            {"Ignore", @"(.*?)"},
        };

        /// <summary>
        /// By default, in absence of a .artifactignore file, we ignore .git folders.
        /// In the future, if other such defaults are to be added, they should be added here.
        /// </summary>
        private readonly IEnumerable<string> DefaultGlobList = new List<string>()
        {
            ".git", // Ignore .git files or folders.
        };

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        private GlobFactory()
        {
            // Enforce no-init object instantiation.
        }

        /// <summary>
        /// Glob Factory constructor.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="tracer"></param>
        /// <param name="globEmptyDirectories"></param>
        public GlobFactory(IFileSystem fileSystem, IAppTraceSource tracer,
            bool globEmptyDirectories = false)
        {
            this.TargetFileSystem = fileSystem;
            this.Tracer = tracer;
            this.GlobEmptyDirectories = globEmptyDirectories;
        }

        #endregion

        /// <summary>
        /// Initializes all the necessary constructs required for globbing
        /// the files and folders under the given source directory. The method also
        /// instantiates/populates collections and controls the flow of logic further
        /// down the stack.
        /// </summary>
        /// <param name="sourceDirectory">The source directory.</param>
        private void Init(string sourceDirectory)
        {
            // DEVNOTE:
            // Pick the .artifactignore file which is immediately under the source directory.
            // For e.g., c:\sourcedirectory\.artifactignore         - [1] 
            //           c:\sourcedirectory\foo\.artifactignore     - [2]
            // [1] is used for the operation. 
            //
            // For *NIX systems, where file casing is important, we expect the .artifactignore filename to be all lowercase.
            // For e.g., \usr\file\.artifactignore      -[1]
            //           \usr\file\.Artifactignore      -[2]
            //           \usr\file\.ARTIFACTIGNORE      -[3]
            // [1] is used for the operation.
            //
            // If there is no .artifactignore file immediately under the source directory then the defaults are applied. 
            //
            // In the future, if we choose to add support for multiple .artifactignore files, then this is where it should be handled.
            if (TargetFileSystem.FileExists((artifactIgnoreFilePath = Path.Combine(sourceDirectory, ArtifactIgnoreFileName))))
            {
                globUsingArtifactIgnoreFile = true;
                this.Tracer.Info("Using .artifactignore file located at: {0} for globbing", artifactIgnoreFilePath);
            }

            // Account for the case sensitivity semantics in accordance with the underlying file system.
            defaultStringComparer = Helpers.FileSystemStringComparer(Environment.OSVersion);

            // For Windows, override the glob evaluation settings.
            if (isWindowsPlatform = Helpers.IsWindowsPlatform(Environment.OSVersion))
            {
                defaultGlobOptions = new GlobOptions() {Evaluation = new EvaluationOptions() {CaseInsensitive = true}};
            }

#if NET_STANDARD
            files = Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories).ToHashSet(x => x, defaultStringComparer);
#else
            files = TargetFileSystem.EnumerateFiles(sourceDirectory, recursiveSearch: true).ToHashSet(x => x, defaultStringComparer);
#endif
            // Honor case sensitivity semantics for the underlying file system.
            ignoreFileGlobSet = new HashSet<string>(defaultStringComparer);
            doNotIgnoreFileGlobSet = new HashSet<string>(defaultStringComparer);
            ignoreFileSet = new HashSet<string>(defaultStringComparer);
            sourceDirectoryItems = new HashSet<string>(defaultStringComparer);

            // Account for all the files under this source directory.
            sourceDirectoryItems.AddRange(files);

            if (!GlobEmptyDirectories) return;

#if NET_STANDARD
            directories = Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories).ToHashSet(x => x, defaultStringComparer);
#else
            directories = TargetFileSystem.EnumerateDirectories(sourceDirectory, recursiveSearch: true).ToHashSet(x => x, defaultStringComparer);
#endif
            nonEmptyDirectories = new HashSet<string>(defaultStringComparer);
            emptyDirectories = new HashSet<string>(defaultStringComparer);

            // Top level directories.
            foreach (var path in files)
            {
                nonEmptyDirectories.Add(Path.GetDirectoryName(path));
            }

            // Inner directories.
            foreach (var dir in directories)
            {
                nonEmptyDirectories.Add(Path.GetDirectoryName(dir));
            }

            // All directories - Non-empty directories = Empty directories.
            emptyDirectories.AddRange(directories.Where(d => !nonEmptyDirectories.Contains(d)));

            // Account for empty directories.
            sourceDirectoryItems.AddRange(emptyDirectories);
        }

        /// <summary>
        /// Performs the globbing operation given a .artifactignore file containing file-globbing patterns.
        /// </summary>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <returns>A collection of files/folders returned after the globbing operation.</returns>
        public IEnumerable<string> PerformGlobbing(string sourceDirectory)
        {
            // Init constructs.
            Init(sourceDirectory);

            // No .artifactignore file found.
            if (!globUsingArtifactIgnoreFile)
            {
                // File doesn't exist, simply use defaults (if the switch is ON).                    
                ignoreFileGlobSet.AddRange(DefaultGlobList);                    
            }

            var retList = DispatchToGlobbingStation(sourceDirectory);           

            if (retList != null && retList.LongCount() > 0)
            {
                this.Tracer.Info(
                    $"Processing .artifactignore file surfaced {retList.LongCount()} files. Total files under source directory: {sourceDirectoryItems.LongCount()}");

                this.Tracer.Verbose("Ignoring the following files/folders:");

                foreach (var file in retList)
                {
                    this.Tracer.Verbose("{0}", file);
                }
            }

            return retList;
        }

        /// <summary>
        /// This method parses and dispatches files for globbing.
        /// When multi .artifactignore file support is added this is where
        /// the dispatching of batches should occur.
        /// </summary>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <returns>A collection of files/folders to be ignored.</returns>
        private IEnumerable<string> DispatchToGlobbingStation(string sourceDirectory)
        {
            // If .artifactignore exists parse else dispatch to GlobbingStation. 
            if (globUsingArtifactIgnoreFile)
            {
                ParseArtifactIgnore();
            }           
            
            // Dispatch to GlobbingStation.
            return GlobbingStation(sourceDirectory);
        }

        /// <summary>
        /// Parses the .artifactignore file and generates ignore and do not ignore glob segments.
        /// </summary>
        private void ParseArtifactIgnore()
        {            
            using (var artifactIgnoreStream = TargetFileSystem.OpenText(artifactIgnoreFilePath))
            {
                var globPattern = isWindowsPlatform ? GlobPatternsWindows : GlobPatternsNix;
        
                string globLine;
                while ((globLine = artifactIgnoreStream.ReadLine()) != null)
                {
                    // Case: Empty/Blank line, skip and move on.
                    if (string.IsNullOrWhiteSpace(globLine))
                    {
                        continue;
                    }
                    // Case: Comments or empty lines.
                    if (new Regex(globPattern["Comment"]).IsMatch(globLine))
                    {
                        this.Tracer.Verbose("Skipping comment.");
                        continue;
                    }
                    // Case: Negation with escaping and glob pattern.
                    else if (new Regex(globPattern["DoNotIgnoreWithEscaping"]).IsMatch(globLine) &&
                             IsSpecialExtentGlob(globLine))
                    {
                        GlobAccounting(globLine.Substring(2, globLine.Length - 2), false);
                        this.Tracer.Verbose("Not ignoring file/folders matching: {0}", globLine);
                    }
                    // Case: Negation with glob pattern.
                    else if (new Regex(globPattern["DoNotIgnore"]).IsMatch(globLine) &&
                             IsSpecialExtentGlob(globLine))
                    {
                        GlobAccounting(globLine.Substring(1, globLine.Length - 1), false);
                        this.Tracer.Verbose("Not ignoring file/folders matching: {0}", globLine);
                    }
                    // Case: Glob pattern with escape sequences.
                    else if (new Regex(globPattern["IgnoreWithEscaping"]).IsMatch(globLine) &&
                             IsSpecialExtentGlob(globLine))
                    {
                        GlobAccounting(globLine.Substring(1, globLine.Length - 1), true);
                        this.Tracer.Verbose("Ignoring file/folders matching: {0}", globLine);
                    }
                    // Case: Special glob pattern. 
                    else if (IsSpecialExtentGlob(globLine))
                    {
                        GlobAccounting(globLine, true);
                        this.Tracer.Verbose("Ignoring file/folders matching: {0}", globLine);
                    }
                    // Case: Negation on files/folders that are escaping ! or # in file/folder names.
                    else if (new Regex(globPattern["DoNotIgnoreWithEscaping"]).IsMatch(globLine))
                    {
                        GlobAccounting(globLine.Substring(2, globLine.Length - 2), false);
                        this.Tracer.Verbose("Not ignoring file/folder: {0}", globLine);
                    }
                    // Case: Escaping ! or # in file/folder name.
                    else if (new Regex(globPattern["IgnoreWithEscaping"]).IsMatch(globLine))
                    {
                        GlobAccounting(globLine.Substring(1, globLine.Length - 1), true);
                        this.Tracer.Verbose("Ignoring file/folder: {0}", globLine);
                    }
                    // Case: Negation on regular file/folder.
                    else if (new Regex(globPattern["DoNotIgnore"]).IsMatch(globLine))
                    {
                        GlobAccounting(globLine.Substring(1, globLine.Length - 1), false);
                        this.Tracer.Verbose("Not ignoring file/folder: {0}", globLine);
                    }                    
                    // Case: Ignore regular file/folder. If this is a bit of gibberish for e.g., ?*&^%$@#$! -> the globbing will fail.
                    else
                    {
                        GlobAccounting(globLine, true);
                        this.Tracer.Verbose("Ignoring file/folder: {0}", globLine);
                    }
                }

                artifactIgnoreStream.Close();
            }
        }
        
        /// <summary>
        /// Perform glob accounting for processing later.
        /// </summary>
        /// <param name="glob">The glob itself.</param>
        /// <param name="ignore">Whether the glob implies ignoring a file/folder or otherwise.</param>
        private void GlobAccounting(string glob, bool ignore = true)
        {
            if (ignore)
            {
                // File is to be ignored, if it is not in the list, add it.
                if (!ignoreFileGlobSet.Contains(glob))
                {
                    ignoreFileGlobSet.Add(glob);
                }
            }
            else
            {
                // File is NOT to be ignored. If it doesn't exist in the do not ignore list, add it.
                if (!doNotIgnoreFileGlobSet.Contains(glob))
                {
                    doNotIgnoreFileGlobSet.Add(glob);
                }
            }
        }

        /// <summary>
        /// Glob segments arrive here for the final filtering phase.
        /// </summary>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <returns>A collection of files/folders to be ignored.</returns>
        private IEnumerable<string> GlobbingStation(string sourceDirectory)
        {
            GlobFilter(sourceDirectory, ignoreFiles: true);

            GlobFilter(sourceDirectory, ignoreFiles: false);

            return ignoreFileSet;
        }

        /// <summary>
        /// Perform glob based filtering.
        /// </summary>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <param name="ignoreFiles">The collection of files/folders to be ignored.</param>
        private void GlobFilter(string sourceDirectory, bool ignoreFiles = true)
        {
            var fileIterationSet = ignoreFiles ? ignoreFileGlobSet : doNotIgnoreFileGlobSet;

            // Go through every file that is to be ignored and add it to the ignore list.
            foreach (var ignoreFile in fileIterationSet)
            {
                // First off, distinguish whether this is a pathed glob or non-pathed glob.
                //
                var effectiveIgnoreFileGlob = string.Empty;
                var effectiveIgnoreFileGlobWide = string.Empty;

                // Path-ed globs.
                if (ignoreFile.Contains(Path.DirectorySeparatorChar))
                {
                    effectiveIgnoreFileGlob = string.Empty;

                    // Case 1: /foo/ (directory only).
                    if (ignoreFile.StartsWith(Path.DirectorySeparatorChar.ToString()) &&
                        ignoreFile.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        // This must be a directory and not a file.
                        // Only search directories not files.                        
                        effectiveIgnoreFileGlob = sourceDirectory + ignoreFile + "**";
                    }
                    // Case 2: /foo (this can either be a file or a directory).
                    else if (ignoreFile.StartsWith(Path.DirectorySeparatorChar.ToString()) &&
                             !ignoreFile.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        // Only search in the .artifactignore directory. Hence append the path. 
                        // Phase 1: Just look for files.
                        effectiveIgnoreFileGlob = sourceDirectory + ignoreFile;

                        // The glob potentially implies any of these possibilities:
                        // E.g., /foo OR /foo* OR /foo** OR /foo/** OR /foo? OR /foo[0-9]
                        //
                        // If the user specifies the glob extent don't bother. Otherwise widen the scope. 
                        if (!ignoreFile.EndsWith("**") && !ignoreFile.EndsWith("*"))
                        {
                            // Phase 2: Also look for directory.
                            effectiveIgnoreFileGlobWide = Path.Combine(effectiveIgnoreFileGlob, "**");
                        }
                    }
                    // Case 3: foo/ (this must be a directory).
                    else if (ignoreFile.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                             !ignoreFile.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        // Ignore everything under this directory path.
                        // E.g., foo/ -> foo/** OR **/foo/ -> **/foo/** OR *foo/ -> **/*foo/** and *foo/**
                        effectiveIgnoreFileGlob = ignoreFile + "**";

                        // Unless the user explicitly wants this folder path to be ignored everywhere, 
                        // only scope the search to the .artifactignore directory.
                        if (!ignoreFile.StartsWith("**"))
                        {
                            effectiveIgnoreFileGlob = Path.Combine(sourceDirectory, effectiveIgnoreFileGlob);
                        }
                    }
                    // The case where the glob doesn't start or end with '/' or '\' but contains these in the middle.
                    // E.g., foo/bar.blah OR foo/bar OR **/foo/bar OR **/foo/bar/*
                    else if (!ignoreFile.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                             !ignoreFile.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        // E.g., foo/bar.blah -> <.artifactIgnorePath>/foo/bar.blah
                        // If the glob doesn't start with ** then only search in the immediate directory.
                        if (!ignoreFile.StartsWith("**"))
                        {
                            effectiveIgnoreFileGlob = Path.Combine(sourceDirectory, ignoreFile);
                        }

                        // If the user specifies the glob extent don't bother. Otherwise widen the scope. 
                        if (!ignoreFile.EndsWith("**") && !ignoreFile.EndsWith("*"))
                        {
                            // Phase 2: Also look for folders.
                            effectiveIgnoreFileGlobWide =
                                Path.Combine(
                                    string.IsNullOrWhiteSpace(effectiveIgnoreFileGlob) ? ignoreFile : effectiveIgnoreFileGlob,
                                    "**");
                        }
                    }

                    Glob ignoreGlob = Glob.Parse(string.IsNullOrWhiteSpace(effectiveIgnoreFileGlob)
                        ? ignoreFile
                        : effectiveIgnoreFileGlob, defaultGlobOptions);

                    Glob ignoreGlobWide = !string.IsNullOrWhiteSpace(effectiveIgnoreFileGlobWide)
                        ? Glob.Parse(effectiveIgnoreFileGlobWide, defaultGlobOptions)
                        : null;

                    PopulateFilter(ignoreGlob, ignoreGlobWide, ignoreFiles);
                }
                // Non-path-ed globs.
                else
                {
                    Glob ignoreGlobWide = null;

                    // No anchor point, search everywhere.
                    if (!ignoreFile.StartsWith("**"))
                    {
                        effectiveIgnoreFileGlob = Path.Combine("**", ignoreFile);
                    }

                    // Some possibilities:
                    // E.g., *.pdb OR foo.* OR debug?.log OR foo.txt OR debug[18].log OR debug[!18].log OR debug[a-z].log OR debug[0-9].log
                    Glob ignoreGlob =
                        Glob.Parse(string.IsNullOrWhiteSpace(effectiveIgnoreFileGlob)
                                ? ignoreFile
                                : effectiveIgnoreFileGlob,
                            defaultGlobOptions);

                    // If the user specifies the glob extent don't bother. Otherwise widen the scope. 
                    if (!ignoreFile.EndsWith("**") && !ignoreFile.EndsWith("*"))
                    {
                        effectiveIgnoreFileGlobWide =
                            Path.Combine(
                                string.IsNullOrWhiteSpace(effectiveIgnoreFileGlob)
                                    ? ignoreFile
                                    : effectiveIgnoreFileGlob,
                                "**");

                        ignoreGlobWide = Glob.Parse(effectiveIgnoreFileGlobWide, defaultGlobOptions);
                    }

                    PopulateFilter(ignoreGlob, ignoreGlobWide, ignoreFiles);
                }
            }
        }

        /// <summary>
        /// Populate the files to be ignored.
        /// </summary>
        /// <param name="ignoreGlob">The ignore glob.</param>
        /// <param name="ignoreGlobWide">The wider extent ignore glob.</param>
        private void PopulateFilter(Glob ignoreGlob, Glob ignoreGlobWide, bool ignoreFiles)   
        {
            if (ignoreFiles)
            {
                ignoreFileSet.AddRangeIfRangeNotNull(sourceDirectoryItems.Where(ignoreGlob.IsMatch));

                if (ignoreGlobWide != null)
                {
                    ignoreFileSet.AddRangeIfRangeNotNull(sourceDirectoryItems.Where(ignoreGlobWide.IsMatch));
                }
            }
            else
            {
                ignoreFileSet.ExceptWith(sourceDirectoryItems.Where(ignoreGlob.IsMatch));

                if (ignoreGlobWide != null)
                {
                    ignoreFileSet.ExceptWith(sourceDirectoryItems.Where(ignoreGlobWide.IsMatch));
                }
            }
        }

        /// <summary>
        /// Detect special glob extent patterns.
        /// </summary>
        /// <param name="globLine">The glob line.</param>
        /// <returns>True if its an extent glob, False otherwise.</returns>
        private static bool IsSpecialExtentGlob(string globLine)
        {
            return (globLine.Contains("*") || globLine.Contains("**") || globLine.Contains("?"));
        }
    }
}

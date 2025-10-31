#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    /// <summary>
    /// Loads a YAML file, and returns the parsed TemplateToken
    /// </summary>
    internal sealed class YamlTemplateLoader
    {
        public YamlTemplateLoader(
            ParseOptions parseOptions,
            IFileProvider fileProvider)
        {
            m_parseOptions = new ParseOptions(parseOptions);
            m_fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        /// <summary>
        /// Parses a workflow <c ref="TemplateToken" /> template.
        ///
        /// Check <c ref="TemplateToken.Errors" /> for errors.
        /// </summary>
        public TemplateToken ParseWorkflow(
            TemplateContext context,
            String path)
        {
            var result = default(TemplateToken);
            try
            {
                result = LoadFile(context, path, WorkflowTemplateConstants.WorkflowRoot);
            }
            catch (Exception ex)
            {
                context.Errors.Add(ex);
            }

            return result;
        }

        private TemplateToken LoadFile(
            TemplateContext context,
            String path,
            String templateType)
        {
            if (context.Errors.Count > 0)
            {
                throw new InvalidOperationException("Expected error count to be 0 when attempting to load a new file");
            }

            // Is entry file?
            var isEntryFile = m_referencedFiles.Count == 0;

            // Root the path
            path = m_fileProvider.ResolvePath(null, path);

            // Validate max files
            m_referencedFiles.Add(path);
            if (m_parseOptions.MaxFiles > 0 && m_referencedFiles.Count > m_parseOptions.MaxFiles)
            {
                throw new InvalidOperationException($"The maximum file count of {m_parseOptions.MaxFiles} has been exceeded");
            }

            // Get the file ID
            var fileId = context.GetFileId(path);

            // Check the cache
            if (!m_cache.TryGetValue(path, out String fileContent))
            {
                // Fetch the file
                context.CancellationToken.ThrowIfCancellationRequested();
                fileContent = m_fileProvider.GetFileContent(path);

                // Validate max file size
                if (fileContent.Length > m_parseOptions.MaxFileSize)
                {
                    throw new InvalidOperationException($"{path}: The maximum file size of {m_parseOptions.MaxFileSize} characters has been exceeded");
                }

                // Cache
                m_cache[path] = fileContent;
            }

            // Deserialize
            var token = default(TemplateToken);
            using (var stringReader = new StringReader(fileContent))
            {
                var yamlObjectReader = new YamlObjectReader(fileId, stringReader, m_parseOptions.AllowAnchors, context.Telemetry);
                token = TemplateReader.Read(context, templateType, yamlObjectReader, fileId, out _);
            }

            // Trace
            if (!isEntryFile)
            {
                context.TraceWriter.Info(String.Empty);
            }
            context.TraceWriter.Info("# ");
            context.TraceWriter.Info("# {0}", path);
            context.TraceWriter.Info("# ");

            return token;
        }

        /// <summary>
        /// Cache of file content
        /// </summary>
        private readonly Dictionary<String, String> m_cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly IFileProvider m_fileProvider;

        private readonly ParseOptions m_parseOptions;

        /// <summary>
        /// Tracks unique file references
        /// </summary>
        private readonly HashSet<String> m_referencedFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
    }
}

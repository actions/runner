using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public static class CodeCoverageUtilities
    {
        public static void CopyFilesFromFileListWithDirStructure(List<string> files, ref string destinatonFilePath)
        {
            string commonPath = null;
            if (files != null)
            {
                files.RemoveAll(q => q == null);

                if (files.Count > 1)
                {
                    files.Sort();
                    commonPath = SharedSubstring(files[0], files[files.Count - 1]);
                }

                foreach (var file in files)
                {
                    string newFile = null;

                    if (!string.IsNullOrEmpty(commonPath))
                    {
                        newFile = file.Replace(commonPath, "");
                    }
                    else
                    {
                        newFile = Path.GetFileName(file);
                    }

                    newFile = Path.Combine(destinatonFilePath, newFile);
                    Directory.CreateDirectory(Path.GetDirectoryName(newFile));
                    File.Copy(file, newFile, true);
                }
            }
        }

        public static XmlDocument ReadSummaryFile(IExecutionContext context, string summaryXmlLocation)
        {
            string xmlContents = "";

            //read xml contents
            if (!File.Exists(summaryXmlLocation))
            {
                throw new ArgumentException(StringUtil.Loc("FileDoesNotExist", summaryXmlLocation));
            }

            xmlContents = File.ReadAllText(summaryXmlLocation);


            if (string.IsNullOrWhiteSpace(xmlContents))
            {
                return null;
            }

            XmlDocument doc = new XmlDocument();
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore
                };

                using (XmlReader reader = XmlReader.Create(summaryXmlLocation, settings))
                {
                    doc.Load(reader);
                }
            }
            catch (XmlException ex)
            {
                context.Warning(StringUtil.Loc("FailedToReadFile", summaryXmlLocation, ex.Message));
                return null;
            }

            return doc;
        }

        public static int GetPriorityOrder(string coverageUnit)
        {
            if (!string.IsNullOrEmpty(coverageUnit))
            {
                switch (coverageUnit.ToLower())
                {
                    case "instruction":
                        return (int)Priority.Instruction;
                    case "line":
                        return (int)Priority.Line;
                    case "complexity":
                        return (int)Priority.Complexity;
                    case "class":
                        return (int)Priority.Class;
                    case "method":
                        return (int)Priority.Method;
                    default:
                        return (int)Priority.Other;
                }
            }

            return (int)Priority.Other;
        }

        public static string ThrowIfParameterEmpty(string parameterValue, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterValue))
            {
                throw new ArgumentException(StringUtil.Loc("ArgumentNeeded", parameterName));
            }
            return parameterValue.Trim();
        }

        public static void ThrowIfClassFilesDirectoriesIsInvalid(string classFilesDirectories)
        {
            //check if classFilesDirectories contains an invalid character
            if ((!string.IsNullOrWhiteSpace(classFilesDirectories)) && (classFilesDirectories.Contains("*")))
            {
                throw new ArgumentException(StringUtil.Loc("InvalidClassFilesDirectory"));
            }
        }

        public static string SetCurrentDirectoryIfDirectoriesParameterIsEmpty(IExecutionContext context, string directoriesParameter, string warningMessage)
        {
            if (string.IsNullOrWhiteSpace(directoriesParameter))
            {
                directoriesParameter = ".";
                context.Output(warningMessage);
            }
            else
            {
                directoriesParameter = directoriesParameter.Trim();
            }
            return directoriesParameter;
        }

        public static string GetSourceDataForJacoco(string sourceFilesDirectories)
        {
            var srcDirectories = sourceFilesDirectories.Split(',');
            var srcData = string.Empty;
            foreach (var srcDirectory in srcDirectories)
            {
                if (!string.IsNullOrWhiteSpace(srcDirectory))
                {
                    srcData += @"              <fileset dir='" + srcDirectory + @"'/>" + Environment.NewLine;
                }
            }

            if (string.IsNullOrWhiteSpace(srcData))
            {
                // if srcdata is empty add current  directory.
                srcData += @"              <fileset dir='.'/>" + Environment.NewLine;
            }
            return srcData;
        }

        public static string GetClassDataForAnt(string include, string exclude, string classFilesDirectories)
        {
            var classDirecories = classFilesDirectories.Split(',');
            var classData = string.Empty;
            var includeData = string.Empty;
            var excludeData = string.Empty;

            if (!string.IsNullOrWhiteSpace(include))
            {
                include = string.Join(",", include.Split(':').Select(includePackage => "**/" + includePackage.Replace('.', '/') + (includePackage.EndsWith("*") ? "/**" : ".class")));
                includeData = @" includes='" + include + @"' ";
            }

            if (!string.IsNullOrWhiteSpace(exclude))
            {
                exclude = string.Join(",", exclude.Split(':').Select(excludePackage => "**/" + excludePackage.Replace('.', '/') + (excludePackage.EndsWith("*") ? "/**" : ".class")));
                excludeData = @" excludes='" + exclude + @"' ";
            }

            foreach (var classDirectory in classDirecories)
            {
                if (!string.IsNullOrWhiteSpace(classDirectory))
                {
                    classData += @"              <fileset dir='" + classDirectory + "' " + includeData + excludeData + @" />" + Environment.NewLine;
                }
            }

            if (string.IsNullOrWhiteSpace(classData))
            {
                // if classdata is empty add current  directory.
                classData = @"              <fileset dir='.' " + includeData + excludeData + @" />" + Environment.NewLine;
            }

            return classData;
        }

        public static void PrependDataToFile(string filePath, string data)
        {
            string str;
            using (FileStream stream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                using (StreamReader sreader = new StreamReader(stream))
                {
                    str = sreader.ReadToEnd();
                }
            }

            File.Delete(filePath);
            using (FileStream stream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                using (StreamWriter swriter = new StreamWriter(stream))
                {
                    swriter.Write("{0}{1}{2}", data, Environment.NewLine, str);
                }
            }
        }

        public static string TrimToEmptyString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }
            return input.Trim();
        }

        public static XElement GetClassDataForMaven(string filterList, string filterTag, string filterSubTag, bool addRecurse = false)
        {
            var filterData = string.Empty;
            string[] listFilters = new string[0];

            if (!string.IsNullOrWhiteSpace(filterList))
            {
                filterData = string.Join(",", filterList.Split(':').Select(filterPackage => (addRecurse ? "**/" : string.Empty) + filterPackage.Replace('.', '/') + (filterPackage.EndsWith("*") ? "/**" : ".class")));
                listFilters = filterData.Split(',');
            }

            var filterElement = new XElement(filterTag);

            foreach (var filter in listFilters)
            {
                filterElement.Add(new XElement(filterSubTag, filter));
            }

            return filterElement;
        }

        public static void GetFilters(string classFilter, out string includeFilter, out string excludeFilter)
        {
            includeFilter = string.Empty;
            excludeFilter = string.Empty;
            if (!string.IsNullOrWhiteSpace(classFilter))
            {
                var filters = classFilter.Split(',');
                foreach (var filter in filters)
                {
                    if (!string.IsNullOrWhiteSpace(filter) && filter.Length >= 2)
                    {
                        // check for the format.
                        if (filter[1] != ':')
                        {
                            throw new ArgumentException(StringUtil.Loc("InvalidClassFilter", classFilter));
                        }

                        switch (filter[0])
                        {
                            case '+':
                                // filters for jacoco coverage should be of the format filter1:filter2:filter3  
                                includeFilter += string.IsNullOrWhiteSpace(includeFilter)
                                    ? GetFilterSubString(filter, 2)
                                    : GetFilterSubString(filter, 1);
                                break;
                            case '-':
                                excludeFilter += string.IsNullOrWhiteSpace(excludeFilter)
                                    ? GetFilterSubString(filter, 2)
                                    : GetFilterSubString(filter, 1);
                                break;
                            default:
                                // filter should start with + or -
                                throw new ArgumentException(StringUtil.Loc("InvalidClassFilter", classFilter));
                        }
                    }
                    else
                    {
                        throw new ArgumentException(StringUtil.Loc("InvalidClassFilter", classFilter));
                    }
                }
            }
        }

        private static string GetFilterSubString(string filter, int startIndex)
        {
            return filter.Substring(startIndex, filter.Length - startIndex);
        }

        private enum Priority
        {
            Class = 1,
            Complexity = 2,
            Method = 3,
            Line = 4,
            Instruction = 5,
            Other = 6
        }

        private static string SharedSubstring(string string1, string string2)
        {
            string ret = string.Empty;

            int index = 1;
            while (string1.Substring(0, index) == string2.Substring(0, index))
            {
                ret = string1.Substring(0, index);
                index++;
            }

            return ret;
        }
    }
}

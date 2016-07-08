namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public static class CodeCoverageConstants
    {
        // version keeps updating . Make sure we use the latest version.
        public const string JacocoVersion = "0.7.5.201505241946";
        public const string MavenAntRunPluginVersion = "1.8";
        public const string CoberturaVersion = "2.7";

        // Cobertura Ant
        #region Ant-Cobertura
        public const string CoberturaClassPathString = "cobertura-classpath-3d368d85-30d7-4f8f-94ec-555eed0714a8";
        public const string CoberturaAntReport =
                            @"<?xml version='1.0'?>" +
                            @"<project name='CoberturaReport'>" +
                            @"{0}" +
                            @"{1}" +
                            @"{2}" +
                            @"<target name='" + @"{3}" + @"'>" +
                            @"     <cobertura-report format = ""html"" destdir = """ +
                                                            @"{4}" +
                                                            @"""" + @" datafile = """ +
                                                            @"{5}" +
                                                            @""" srcdir = """ + @"{6}" + @""" />" +
                            @"     <cobertura-report format = ""xml"" destdir = """ +
                                                            @"{4}" +
                                                            @"""" + @" datafile = """ +
                                                            @"{5}" +
                                                            @""" srcdir = """ + @"{6}" + @""" />" +
                            @"</target>" +
                            @"</project>";

        public const string CoberturaTaskDef = @"<taskdef classpathref = """ + CoberturaClassPathString + @""" resource = ""tasks.properties"" /> ";
        public const string CoberturaEnvProperty = @"<property environment=""env""/>";
        public const string CoberturaClassPath =
                           @"<path id=""" + CoberturaClassPathString + @""" description=""classpath for instrumenting classes"">" +
                          @"    <fileset dir = ""${env.COBERTURA_HOME}""> " +
                          @"         <include name = ""cobertura*.jar"" /> " +
                          @"         <include name = ""**/lib/**/*.jar"" /> " +
                          @"    </fileset> " +
                          @"</path> ";
        public const string CoberturaInstrumentNode =
                            @"   <cobertura-instrument todir=""" + @"{0}" + @""" datafile=""" + @"{1}" + @""">" +
                            @"{2}" +
                            @"   </cobertura-instrument>";
        #endregion

        #region Ant-Jacoco

        public const string JacocoAntReport =
            @"<?xml version='1.0'?>" +
            @"<project name='JacocoReport'>" +
            @"  <target name='" + @"{0}" + @"'>" +
            @"      <jacoco:report xmlns:jacoco='antlib:org.jacoco.ant'>" +
            @"          <executiondata>" +
            @"{1}" +
            @"          </executiondata>" +
            @"          <structure name = 'Jacoco report' >" +
            @"              <classfiles >" +
            @"{2}" +
            @"              </classfiles>" +
            @"              <sourcefiles encoding = 'UTF-8' >" +
            @"{3}" +
            @"              </sourcefiles>" +
            @"          </structure>" +
            @"          <html destdir='" + @"{4}" + @"' />" +
            @"          <csv destfile = '" + @"{5}" + @"' />" +
            @"          <xml destfile='" + @"{6}" + @"' />" +
            @"      </jacoco:report>" +
            @"  </target>" +
            @"</project>";

        #endregion

        #region PublishCodeCoverage
        public const string RawFilesDirectory = "Code Coverage Files";
        public const string ReportDirectory = "Code Coverage Report";
        public const string SummaryFileDirectory = "summary";
        public const string DefaultIndexFile = "index.html";
        public const string NewIndexFile = "indexnew.html";
        //This file name is dependent on the outputs produced by the cobertura tool.
        //The name can change in future and should be updated if required
        public const string DefaultNonFrameFileCobertura = "frame-summary.html";
        #endregion

        #region VerboseStrings
        public const string SettingAttributeTemplate = "Setting attribute '{0}' = '{1}' for '{2}' task.";
        public const string EnablingEditingTemplate = "Enabling '{0}' code coverage for '{1}' by editing '{2}'.";
        public const string InvalidXMLTemplate = "Invalid build xml '{0}'. Error '{1}' occurred while parsing the file. ";
        public const string MavenMultiModule = "This is a multi module project. Generating code coverage reports using ant task.";
        #endregion
    }
}

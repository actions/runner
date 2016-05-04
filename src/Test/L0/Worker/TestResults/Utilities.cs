using System.Xml;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.TestResults
{
    public class Utilities
    {
        public static string GetDtdExceptionMessage(string filePath)
        {
            var exceptionMessage = string.Empty;
            XmlDocument doc = new XmlDocument();
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };

                using (XmlReader reader = XmlReader.Create(filePath, settings))
                {
                    doc.Load(reader);
                }
            }
            catch (XmlException ex)
            {
                exceptionMessage = ex.Message;
            }
            return exceptionMessage;
        }        
            
        public static string dtdInvalidXml = @"<?xml version='1.0' encoding='UTF-8' standalone='yes'?><!DOCTYPE report PUBLIC '-//JACOCO//DTD Report 1.0//EN' 'report.dtd'></report>";
    }
}

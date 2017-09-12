using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.TestResults;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.TestResults
{
    public class XUnitResultReaderTests : IDisposable
    {
        private string _xUnitResultFile;
        private XUnitResultReader _xUnitReader;
        private Mock<IExecutionContext> _ec;

        private const string _xunitResultsFull = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<assemblies>" +
        "<assembly name=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\bin\\Debug\\ClassLibrary2.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:15\" config-file=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" time=\"0.233\" errors=\"0\">" +
        "<errors />" +
        "<collection total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class1\" time=\"0.044\">" +
        "<test name=\"MyFirstUnitTests.Class1.FailingTest\" type=\"MyFirstUnitTests.Class1\" method=\"FailingTest\" time=\"0.0422319\" result=\"Fail\">" +
        "<failure exception-type=\"Xunit.Sdk.EqualException\" >" +
        "<message><![CDATA[Assert.Equal() Failure" +
        "Expected: 5" +
        "Actual: 4]]></message >" +
        "<stack-trace><![CDATA[at MyFirstUnitTests.Class1.FailingTest() in C: \\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 17]]></stack-trace>" +
        "</failure >" +
        "</test>" +
        "<test name=\"MyFirstUnitTests.Class1.PassingTest\" type=\"MyFirstUnitTests.Class1\" method=\"PassingTest\" time=\"0.0014079\" result=\"Pass\">" +
        "<traits>" +
        "<trait name=\"priority\" value=\"0\" />" +
        "<trait name=\"owner\" value=\"asdf\" />" +
        "</traits>" +
        "</test>" +
        "</collection>" +
        "</assembly>" +
        "<assembly name=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary1\\bin\\Debug\\ClassLibrary1.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:16\" config-file=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" time=\"0.152\" errors=\"0\">" +
        "<errors />" +
        "<collection total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class2\" time=\"0.006\">" +
        "<test name=\"MyFirstUnitTests.Class2.tset2\" type=\"MyFirstUnitTests.Class2\" method=\"tset2\" time=\"0.0056931\" result=\"Pass\" />" +
        "<test name=\"MyFirstUnitTests.Class2.test1\" type=\"MyFirstUnitTests.Class2\" method=\"test1\" time=\"0.0001093\" result=\"Pass\">" +
        "<traits>" +
        "<trait name=\"priority\" value=\"0\" />" +
        "</traits>" +
        "</test>" +
        "</collection>" +
        "</assembly>" +
        "</assemblies>";

        private const string c_parallelXunitResults = "<?xml version =\"1.0\" encoding=\"utf-8\"?>" +
           "<assemblies>" +
           "<assembly name=\"c:\\Users\\vimegh\\Documents\\Visual Studio 2013\\Projects\\ClassLibrary2\\ClassLibrary2\\bin\\Debug\\ClassLibrary2.DLL\" environment=\"64-bit .NET 4.0.30319.34014 [collection-per-class, parallel (4 threads)]\" test-framework=\"xUnit.net 2.1.0.3179\" run-date=\"2016-06-08\" run-time=\"07:12:09\" config-file=\"c:\\Users\\vimegh\\Documents\\Visual Studio 2013\\Projects\\ClassLibrary2\\packages\\xunit.runner.console.2.1.0\\tools\\xunit.console.exe.Config\" total=\"5\" passed=\"3\" failed=\"2\" skipped=\"0\" time=\"5.433\" errors=\"0\">" +
           "<errors />" +
           "<collection total=\"5\" passed=\"3\" failed=\"2\" skipped=\"0\" name=\"Test collection for ClassLibrary2.Class1\" time=\"5.068\">" +
           "<test name=\"ClassLibrary2.Class1.MyFirstTheory(value: 5)\" type=\"ClassLibrary2.Class1\" method=\"MyFirstTheory\" time=\"0.0398995\" result=\"Pass\" />" +
           "<test name=\"ClassLibrary2.Class1.MyFirstTheory(value: 3)\" type=\"ClassLibrary2.Class1\" method=\"MyFirstTheory\" time=\"0.0000573\" result=\"Pass\" />" +
           "<test name=\"ClassLibrary2.Class1.MyFirstTheory(value: 6)\" type=\"ClassLibrary2.Class1\" method=\"MyFirstTheory\" time=\"0.0047208\" result=\"Fail\">" +
           "<failure exception-type=\"Xunit.Sdk.TrueException\">" +
           "<message><![CDATA[Assert.True() Failure\r\nExpected: True\r\nActual:   False]]></message>" +
           "<stack-trace><![CDATA[   at ClassLibrary2.Class1.MyFirstTheory(Int32 value) in c:\\Users\\vimegh\\Documents\\Visual Studio 2013\\Projects\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 37]]></stack-trace>" +
           "</failure>" +
           "</test>" +
           "<test name=\"ClassLibrary2.Class1.FailingTest\" type=\"ClassLibrary2.Class1\" method=\"FailingTest\" time=\"0.0106463\" result=\"Fail\">" +
           "<failure exception-type=\"Xunit.Sdk.EqualException\">" +
           "<message><![CDATA[Assert.Equal() Failure\r\nExpected: 5\r\nActual:   4]]></message>" +
           "<stack-trace><![CDATA[   at ClassLibrary2.Class1.FailingTest() in c:\\Users\\vimegh\\Documents\\Visual Studio 2013\\Projects\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 23]]></stack-trace>" +
           "</failure>" +
           "</test>" +
           "<test name=\"ClassLibrary2.Class1.PassingTest\" type=\"ClassLibrary2.Class1\" method=\"PassingTest\" time=\"5.0128822\" result=\"Pass\" />" +
            "</collection>" +
           "</assembly>" +
           "<assembly name=\"c:\\Users\\vimegh\\Documents\\Visual Studio 2013\\Projects\\ClassLibrary2\\ClassLibrary1\\bin\\Debug\\ClassLibrary1.DLL\" environment=\"64-bit .NET 4.0.30319.34014 [collection-per-class, parallel (4 threads)]\" test-framework=\"xUnit.net 2.1.0.3179\" run-date=\"2016-06-08\" run-time=\"07:12:09\" config-file=\"c:\\Users\\vimegh\\Documents\\Visual Studio 2013\\Projects\\ClassLibrary2\\packages\\xunit.runner.console.2.1.0\\tools\\xunit.console.exe.Config\" total=\"5\" passed=\"3\" failed=\"2\" skipped=\"0\" time=\"5.417\" errors=\"0\">" +
           "<errors />" +
           "<collection total=\"5\" passed=\"3\" failed=\"2\" skipped=\"0\" name=\"Test collection for ClassLibrary1.Classvs\" time=\"5.067\">" +
           "<test name=\"ClassLibrary1.Classvs.PassingTest1\" type=\"ClassLibrary1.Classvs\" method=\"PassingTest1\" time=\"5.0549175\" result=\"Pass\" />" +
           "<test name=\"ClassLibrary1.Classvs.FailingTest1\" type=\"ClassLibrary1.Classvs\" method=\"FailingTest1\" time=\"0.0102656\" result=\"Fail\">" +
           "<failure exception-type=\"Xunit.Sdk.EqualException\">" +
           "<message><![CDATA[Assert.Equal() Failure\r\nExpected: 5\r\nActual:   4]]></message>" +
           "<stack-trace><![CDATA[   at ClassLibrary1.Classvs.FailingTest1() in c:\\Users\\vimegh\\Documents\\Visual Studio 2013\\Projects\\ClassLibrary2\\ClassLibrary1\\Classvs.cs:line 23]]></stack-trace>" +
           "</failure>" +
           "</test>" +
           "<test name=\"ClassLibrary1.Classvs.MySecondTheory(value: 5)\" type=\"ClassLibrary1.Classvs\" method=\"MySecondTheory\" time=\"0.0010502\" result=\"Pass\" />" +
           "<test name=\"ClassLibrary1.Classvs.MySecondTheory(value: 3)\" type=\"ClassLibrary1.Classvs\" method=\"MySecondTheory\" time=\"0.0000131\" result=\"Pass\" />" +
           "<test name=\"ClassLibrary1.Classvs.MySecondTheory(value: 6)\" type=\"ClassLibrary1.Classvs\" method=\"MySecondTheory\" time=\"0.0008241\" result=\"Fail\">" +
           "<failure exception-type=\"Xunit.Sdk.TrueException\" > " +
           "<message><![CDATA[Assert.True() Failure\r\nExpected: True\r\nActual:   False]]></message>" +
           "<stack-trace><![CDATA[   at ClassLibrary1.Classvs.MySecondTheory(Int32 value) in c:\\Users\\vimegh\\Documents\\Visual Studio 2013\\Projects\\ClassLibrary2\\ClassLibrary1\\Classvs.cs:line 37]]></stack-trace>" +
           "</failure>" +
           "</test>" +
           "</collection>" +
           "</assembly>" +
           "</assemblies>";

        private const string _xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<assemblies>" +
        "<assembly name = \"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\bin\\Debug\\ClassLibrary2.DLL\">" +
        "<class name=\"MyFirstUnitTests.Class1\">" +
        "<test name=\"MyFirstUnitTests.Class1.FailingTest\">" +
        "</test>" +
        "</class>" +
        "</assembly>" +
        "</assemblies>";

        private const string _xunitResultsWithDtd = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<!DOCTYPE report PUBLIC '-//JACOCO//DTD Report 1.0//EN' 'report.dtd'>" +
        "<assemblies>" +
        "<assembly name = \"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\bin\\Debug\\ClassLibrary2.DLL\">" +
        "<class name=\"MyFirstUnitTests.Class1\">" +
        "<test name=\"MyFirstUnitTests.Class1.FailingTest\">" +
        "</test>" +
        "</class>" +
        "</assembly>" +
        "</assemblies>";

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ResultsWithoutMandatoryFieldsAreSkipped()
        {
            SetupMocks();
            var xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                  "<assemblies>" +
                                  "<assembly>" +
                                  "<collection>" +
                                  "<test name=\"\">" +
                                  "</test>" +
                                  "<test>" +
                                  "</test>" +
                                  "</collection>" +
                                  "</assembly>" +
                                  "</assemblies>";
            WriteXUnitFile(xunitResults);
            XUnitResultReader reader = new XUnitResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));   
            Assert.NotNull(runData);
            Assert.Equal(0, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadResultsReturnsCorrectValues()
        {
            SetupMocks();
            var xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<assemblies>" +
            "<assembly name=\"C:/Users/somerandomusername/Source/Workspaces/p1/ClassLibrary2/ClassLibrary2/bin/Debug/ClassLibrary2.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:15\" config-file=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" time=\"0.233\" errors=\"0\">" +
            "<errors />" +
            "<collection total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class1\" time=\"0.044\">" +
            "<test name=\"MyFirstUnitTests.Class1.FailingTest\" type=\"MyFirstUnitTests.Class1\" method=\"FailingTest\" time=\"1.0422319\" result=\"Fail\">" +
            "<failure exception-type=\"Xunit.Sdk.EqualException\" >" +
            "<message><![CDATA[Assert.Equal() Failure" +
            "Expected: 5" +
            "Actual: 4]]></message >" +
            "<stack-trace><![CDATA[at MyFirstUnitTests.Class1.FailingTest() in C: \\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 17]]></stack-trace>" +
            "</failure >" +
            "<output><![CDATA[This is standard console output for xunit.]]></output>" +
            "</test>" +
            "<test name=\"MyFirstUnitTests.Class1.PassingTest\" type=\"MyFirstUnitTests.Class1\" method=\"PassingTest\" time=\"0.0014079\" result=\"Pass\">" +
            "<traits>" +
            "<trait name=\"priority\" value=\"0\" />" +
            "<trait name=\"owner\" value=\"asdf\" />" +
            "</traits>" +
            "</test>" +
            "</collection>" +
            "</assembly>" +
            "<assembly name=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary1\\bin\\Debug\\ClassLibrary1.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:16\" config-file=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" time=\"0.152\" errors=\"0\">" +
            "<errors />" +
            "<collection total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class2\" time=\"0.006\">" +
            "<test name=\"MyFirstUnitTests.Class2.tset2\" type=\"MyFirstUnitTests.Class2\" method=\"tset2\" time=\"0.0056931\" result=\"Pass\" />" +
            "<test name=\"MyFirstUnitTests.Class2.test1\" type=\"MyFirstUnitTests.Class2\" method=\"test1\" time=\"0.0001093\" result=\"Pass\">" +
            "<traits>" +
            "<trait name=\"priority\" value=\"0\" />" +
            "</traits>" +
            "</test>" +
            "</collection>" +
            "</assembly>" +
            "</assemblies>";
            _xUnitResultFile = "XUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);
            XUnitResultReader reader = new XUnitResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));
            Assert.Equal("XUnit Test Run debug any cpu", runData.Name);
            Assert.Equal(4, runData.Results.Length);
            Assert.Equal("debug", runData.BuildFlavor);
            Assert.Equal("any cpu", runData.BuildPlatform);
            Assert.Equal("1", runData.Build.Id);
            Assert.Equal(1, runData.Attachments.Length);
            Assert.Equal("Failed", runData.Results[0].Outcome);
            Assert.Equal("FailingTest", runData.Results[0].TestCaseTitle);
            Assert.Equal("MyFirstUnitTests.Class1.FailingTest", runData.Results[0].AutomatedTestName);
            Assert.Equal("Assert.Equal() FailureExpected: 5Actual: 4", runData.Results[0].ErrorMessage);
            Assert.Equal("at MyFirstUnitTests.Class1.FailingTest() in C: \\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 17", runData.Results[0].StackTrace);
            Assert.Equal("This is standard console output for xunit.", runData.Results[0].ConsoleLog);
            Assert.Equal("Owner", runData.Results[0].RunBy.DisplayName);
            Assert.Equal("Completed", runData.Results[0].State);
            Assert.Equal("1042", runData.Results[0].DurationInMs.ToString());
            Assert.Equal("ClassLibrary2.DLL", runData.Results[0].AutomatedTestStorage);
            Assert.Equal("Passed", runData.Results[1].Outcome);
            Assert.Equal("0", runData.Results[1].Priority.ToString());
            Assert.Equal("asdf", runData.Results[1].Owner.DisplayName);
            Assert.Equal(null, runData.Results[0].AutomatedTestId);
            Assert.Equal(null, runData.Results[0].AutomatedTestTypeId);
            DateTime startDate;
            DateTime.TryParse(runData.StartDate, out startDate);
            DateTime completeDate;
            DateTime.TryParse(runData.CompleteDate, out completeDate);
            Assert.Equal((completeDate - startDate).TotalMilliseconds, 1152);
            Assert.Equal("releaseUri", runData.ReleaseUri);
            Assert.Equal("releaseEnvironmentUri", runData.ReleaseEnvironmentUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadsResultsReturnsCorrectValuesInDifferentCulture()
        {
            SetupMocks();
            CultureInfo current = CultureInfo.CurrentCulture;
            try
            {
                //German is used, as in this culture decimal seperator is comma & thousand seperator is dot
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                var xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<assemblies>" +
            "<assembly name=\"C:/Users/somerandomusername/Source/Workspaces/p1/ClassLibrary2/ClassLibrary2/bin/Debug/ClassLibrary2.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:15\" config-file=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" time=\"0.233\" errors=\"0\">" +
            "<errors />" +
            "<collection total=\"2\" passed=\"1\" failed=\"1\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class1\" time=\"0.044\">" +
            "<test name=\"MyFirstUnitTests.Class1.FailingTest\" type=\"MyFirstUnitTests.Class1\" method=\"FailingTest\" time=\"1.0422319\" result=\"Fail\">" +
            "<failure exception-type=\"Xunit.Sdk.EqualException\" >" +
            "<message><![CDATA[Assert.Equal() Failure" +
            "Expected: 5" +
            "Actual: 4]]></message >" +
            "<stack-trace><![CDATA[at MyFirstUnitTests.Class1.FailingTest() in C: \\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 17]]></stack-trace>" +
            "</failure >" +
            "<output><![CDATA[This is standard console output for xunit.]]></output>" +
            "</test>" +
            "<test name=\"MyFirstUnitTests.Class1.PassingTest\" type=\"MyFirstUnitTests.Class1\" method=\"PassingTest\" time=\"0.0014079\" result=\"Pass\">" +
            "<traits>" +
            "<trait name=\"priority\" value=\"0\" />" +
            "<trait name=\"owner\" value=\"asdf\" />" +
            "</traits>" +
            "</test>" +
            "</collection>" +
            "</assembly>" +
            "<assembly name=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary1\\bin\\Debug\\ClassLibrary1.DLL\" environment=\"64-bit .NET 4.0.30319.42000 [collection-per-class, parallel]\" test-framework=\"xUnit.net 2.0.0.2929\" run-date=\"2015-08-18\" run-time=\"06:17:16\" config-file=\"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\packages\\xunit.runner.console.2.0.0\\tools\\xunit.console.exe.Config\" total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" time=\"0.152\" errors=\"0\">" +
            "<errors />" +
            "<collection total=\"2\" passed=\"2\" failed=\"0\" skipped=\"0\" name=\"Test collection for MyFirstUnitTests.Class2\" time=\"0.006\">" +
            "<test name=\"MyFirstUnitTests.Class2.tset2\" type=\"MyFirstUnitTests.Class2\" method=\"tset2\" time=\"0.0056931\" result=\"Pass\" />" +
            "<test name=\"MyFirstUnitTests.Class2.test1\" type=\"MyFirstUnitTests.Class2\" method=\"test1\" time=\"0.0001093\" result=\"Pass\">" +
            "<traits>" +
            "<trait name=\"priority\" value=\"0\" />" +
            "</traits>" +
            "</test>" +
            "</collection>" +
            "</assembly>" +
            "</assemblies>";
                _xUnitResultFile = "XUnitResults.xml";
                File.WriteAllText(_xUnitResultFile, xunitResults);
                XUnitResultReader reader = new XUnitResultReader();
                TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));
                Assert.Equal("XUnit Test Run debug any cpu", runData.Name);
                Assert.Equal(4, runData.Results.Length);
                Assert.Equal("debug", runData.BuildFlavor);
                Assert.Equal("any cpu", runData.BuildPlatform);
                Assert.Equal("1", runData.Build.Id);
                Assert.Equal(1, runData.Attachments.Length);
                Assert.Equal("Failed", runData.Results[0].Outcome);
                Assert.Equal("FailingTest", runData.Results[0].TestCaseTitle);
                Assert.Equal("MyFirstUnitTests.Class1.FailingTest", runData.Results[0].AutomatedTestName);
                Assert.Equal("Assert.Equal() FailureExpected: 5Actual: 4", runData.Results[0].ErrorMessage);
                Assert.Equal("at MyFirstUnitTests.Class1.FailingTest() in C: \\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\Class1.cs:line 17", runData.Results[0].StackTrace);
                Assert.Equal("This is standard console output for xunit.", runData.Results[0].ConsoleLog);
                Assert.Equal("Owner", runData.Results[0].RunBy.DisplayName);
                Assert.Equal("Completed", runData.Results[0].State);
                Assert.Equal("1042", runData.Results[0].DurationInMs.ToString());
                Assert.Equal("ClassLibrary2.DLL", runData.Results[0].AutomatedTestStorage);
                Assert.Equal("Passed", runData.Results[1].Outcome);
                Assert.Equal("0", runData.Results[1].Priority.ToString());
                Assert.Equal("asdf", runData.Results[1].Owner.DisplayName);
                Assert.Equal(null, runData.Results[0].AutomatedTestId);
                Assert.Equal(null, runData.Results[0].AutomatedTestTypeId);
                DateTime startDate;
                DateTime.TryParse(runData.StartDate, out startDate);
                DateTime completeDate;
                DateTime.TryParse(runData.CompleteDate, out completeDate);
                Assert.Equal((completeDate - startDate).TotalMilliseconds, 1152);
                Assert.Equal("releaseUri", runData.ReleaseUri);
                Assert.Equal("releaseEnvironmentUri", runData.ReleaseEnvironmentUri);
            }
            finally
            {
                CultureInfo.CurrentCulture = current;
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyReadResultsReturnsDurationAsDiffBetweenMinStartAndMaxCompletedTime()
        {
            SetupMocks();
            string xunitResults = c_parallelXunitResults;
            XUnitResultReader reader = new XUnitResultReader();
            _xUnitResultFile = "XUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));
            Assert.Equal(40, (int)runData.Results[0].DurationInMs);
            DateTime startDate, completeDate;
            DateTime.TryParse(runData.StartDate, out startDate);
            Assert.Equal("2016-06-08T07:12:09.0000000", startDate.ToString("o"));
            DateTime.TryParse(runData.CompleteDate, out completeDate);
            Assert.Equal("2016-06-08T07:12:14.4330000", completeDate.ToString("o"));
            Assert.Equal((completeDate - startDate).TotalMilliseconds, 5433);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyReadResultsReturnsDurationAsSumOfAssemblyTimeWhenRunDateNotParseable()
        {
            SetupMocks();
            //improper date format
            string xunitResults = c_parallelXunitResults;
            XUnitResultReader reader = new XUnitResultReader();

            xunitResults = xunitResults.Replace(" run-date=\"2016-06-08\" ", " run-date=\"201-06--08\" ");
            _xUnitResultFile = "BadXUnitResults1.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));

            DateTime startDate, completeDate;
            Assert.Equal(40, (int)runData.Results[0].DurationInMs);
            DateTime.TryParse(runData.StartDate, out startDate);
            Assert.NotEqual("2016-06-08T07:12:09.0000000", startDate.ToString("o"));
            DateTime.TryParse(runData.CompleteDate, out completeDate);
            Assert.Equal((completeDate - startDate).TotalMilliseconds, 10850);

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyReadResultsReturnsDurationAsSumOfAssemblyTimeWhenRunDateNotAvailable()
        {
            SetupMocks();
            //no run-time tag present
            string xunitResults = c_parallelXunitResults;
            XUnitResultReader reader = new XUnitResultReader();
            xunitResults = xunitResults.Replace("run-time", "timer").Replace("run-date", "dater");
            _xUnitResultFile = "BadXUnitResults2.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));
            DateTime startDate, completeDate;
            Assert.Equal(40, (int)runData.Results[0].DurationInMs);
            DateTime.TryParse(runData.StartDate, out startDate);
            Assert.NotEqual("2016-06-08T07:12:09.0000000", startDate.ToString("o"));
            DateTime.TryParse(runData.CompleteDate, out completeDate);
            Assert.Equal((completeDate - startDate).TotalMilliseconds, 10850);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyReadResultsReturnsDurationAsSumOfTestCaseTimeWhenAssemblyTimeTagsNotAvailable()
        {
            SetupMocks();
            //no assembly time tag present
            string xunitResults = c_parallelXunitResults;
            XUnitResultReader reader = new XUnitResultReader();
            xunitResults = xunitResults.Replace("run-time", "timer").Replace("run-date", "dater").Replace("time=\"5.417\"", "t=\"5.417\"");
            _xUnitResultFile = "BadXUnitResults3.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext("Owner", "any cpu", "debug", 1, "", "releaseUri", "releaseEnvironmentUri"));
            DateTime startDate, completeDate;
            Assert.Equal(40, (int)runData.Results[0].DurationInMs);
            DateTime.TryParse(runData.StartDate, out startDate);
            Assert.NotEqual("2016-06-08T07:12:09.0000000", startDate.ToString("o"));
            DateTime.TryParse(runData.CompleteDate, out completeDate);
            Assert.Equal((completeDate - startDate).TotalMilliseconds, 10135);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void CustomRunTitleIsHonoured()
        {
            SetupMocks();
            var runData = ReadResults();
            Assert.Equal("My Run Title", runData.Name);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadResultsDoesNotFailForV1()
        {
            SetupMocks();
            var xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<assemblies>" +
            "<assembly name = \"C:\\Users\\somerandomusername\\Source\\Workspaces\\p1\\ClassLibrary2\\ClassLibrary2\\bin\\Debug\\ClassLibrary2.DLL\">" +
            "<class name=\"MyFirstUnitTests.Class1\">" +
            "<test name=\"MyFirstUnitTests.Class1.FailingTest\">" +
            "</test>" +
            "</class>" +
            "</assembly>" +
            "</assemblies>";
            _xUnitResultFile = "BadXUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);
            XUnitResultReader reader = new XUnitResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext(null, null, null, 0, null, null, null));
            Assert.Equal(0, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadResultsDoesNotFailForBadXml()
        {
            SetupMocks();
            var xunitResults = "<random>" +
            "</random>";
            _xUnitResultFile = "BadXUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);
            XUnitResultReader reader = new XUnitResultReader();
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile, new TestRunContext(null, null, null, 0, null, null, null));
            Assert.Equal(0, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void ReadResultsDoesNotFailForBareMinimumXml()
        {
            SetupMocks();
            var runData = GetTestRunData();
            Assert.Equal(1, runData.Results.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyRunTypeIsSet()
        {
            SetupMocks();
            var runData = GetTestRunData();
            Assert.Equal(_xUnitReader.Name, runData.Results[0].AutomatedTestType);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishBasicXUnitResultsAddsResultsFileByDefault()
        {
            SetupMocks();
            var runData = ReadResults();
            Assert.Equal(1, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void PublishBasicXUnitResultsSkipsAddingResultsFileWhenFlagged()
        {
            SetupMocks();
            var runData = ReadResults(false);
            Assert.Equal(0, runData.Attachments.Length);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestCaseStartDate()
        {
            SetupMocks();
            var runData = ReadResults(true, true);
            Assert.Equal(runData.StartDate, runData.Results[0].StartedDate.ToString("o"));

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyTestCaseCompletedDate()
        {
            SetupMocks();
            var runData = ReadResults(true, true);
            var testCase1CompletedDate = runData.Results[0].CompletedDate;
            var testCase2StartDate = runData.Results[1].StartedDate;
            Assert.True(testCase1CompletedDate <= testCase2StartDate, "first test case end should be before second test case start time");

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyLastTestCaseEndDateNotGreaterThanTestRunTotalTime()
        {
            SetupMocks();
            var runData = ReadResults(true, true);
            var testCaseCompletedDate = runData.Results[0].CompletedDate;
            var testRunCompletedDate = runData.Results[0].StartedDate.AddTicks(DateTime.Parse(runData.CompleteDate).Ticks);
            Assert.True(testCaseCompletedDate <= testRunCompletedDate, "first test case end should be within test run completed time");

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyStartDateIsNotEmpty()
        {
            SetupMocks();
            var resultsWithNoTime = _xunitResultsFull.Replace("run-time", "timer").Replace("run-date", "dater");
            var testRunData = ReadResults(resultsWithNoTime);
            Assert.NotEqual(string.Empty, testRunData.StartDate);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void VerifyEndDateIsNotEmpty()
        {
            SetupMocks();
            var resultsWithNoTime = _xunitResultsFull.Replace("run-time", "timer").Replace("run-date", "dater");
            var testRunData = ReadResults(resultsWithNoTime);
            Assert.NotEqual(string.Empty, testRunData.CompleteDate);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PublishTestResults")]
        public void Xunit_DtdProhibitedXmlShouldReturnNull()
        {
            SetupMocks();
            var testRunData = ReadResults(_xunitResultsWithDtd);
            Assert.NotNull(testRunData);
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_xUnitResultFile);
            }
            catch
            {
            }
        }

        private void SetupMocks([CallerMemberName] string name = "")
        {
            TestHostContext hc = new TestHostContext(this, name);
            _ec = new Mock<IExecutionContext>();
            List<string> warnings;
            var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
            _ec.Setup(x => x.Variables).Returns(variables);
        }

        private TestRunData GetTestRunData()
        {
            var xunitResults = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                  "<assemblies>" +
                                  "<assembly>" +
                                  "<collection>" +
                                  "<test name=\"MyFirstUnitTests.Class1.Test1\" method=\"asdasdf\">" +
                                  "</test>" +
                                  "</collection>" +
                                  "</assembly>" +
                                  "</assemblies>";

            _xUnitResultFile = "BareMinimumXUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, xunitResults);
            _xUnitReader = new XUnitResultReader();
            TestRunData runData = _xUnitReader.ReadResults(_ec.Object, _xUnitResultFile,
                new TestRunContext(null, null, null, 0, null, null, null));
            return runData;
        }

        private TestRunData ReadResults(string content)
        {
            WriteXUnitFile(content);
            return ReadResultsInternal(false);
        }

        private TestRunData ReadResults(bool attachRunLevelAttachments = true, bool fullResults = false)
        {
            WriteXUnitFile(fullResults ? _xunitResultsFull : _xunitResults);
            return ReadResultsInternal(attachRunLevelAttachments);
        }

        private void WriteXUnitFile(string content)
        {
            _xUnitResultFile = "BadXUnitResults.xml";
            File.WriteAllText(_xUnitResultFile, content);
        }

        private TestRunData ReadResultsInternal(bool attachRunLevelAttachments = true)
        {
            XUnitResultReader reader = new XUnitResultReader();
            reader.AddResultsFileToRunLevelAttachments = attachRunLevelAttachments;
            TestRunData runData = reader.ReadResults(_ec.Object, _xUnitResultFile,
                new TestRunContext(null, "any cpu", "debug", 0, null, null, null, "My Run Title"));
            return runData;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public class LoggingCommandL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "LoggingCommand")]
        public void CommandParserTest()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                string message;
                ActionCommand test;
                ActionCommand verify;
                HashSet<string> commands = new HashSet<string>() { "do-something" };
                //##[do-something k1=v1;]msg
                message = "##[do-something k1=v1;]msg";
                test = new ActionCommand("do-something")
                {
                    Data = "msg",
                };
                test.Properties.Add("k1", "v1");
                Assert.True(ActionCommand.TryParse(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //##[do-something]
                message = "##[do-something]";
                test = new ActionCommand("do-something");
                Assert.True(ActionCommand.TryParse(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //##[do-something k1=%3B=%0D=%0A=%5D;]%3B-%0D-%0A-%5D
                message = "##[do-something k1=%3B=%0D=%0A=%5D;]%3B-%0D-%0A-%5D";
                test = new ActionCommand("do-something")
                {
                    Data = ";-\r-\n-]",
                };
                test.Properties.Add("k1", ";=\r=\n=]");
                Assert.True(ActionCommand.TryParse(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //##[do-something k1=%253B=%250D=%250A=%255D;]%253B-%250D-%250A-%255D
                message = "##[do-something k1=%253B=%250D=%250A=%255D;]%253B-%250D-%250A-%255D";
                test = new ActionCommand("do-something")
                {
                    Data = "%3B-%0D-%0A-%5D",
                };
                test.Properties.Add("k1", "%3B=%0D=%0A=%5D");
                Assert.True(ActionCommand.TryParse(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //##[do-something k1=;k2=;]
                message = "##[do-something k1=;k2=;]";
                test = new ActionCommand("do-something");
                Assert.True(ActionCommand.TryParse(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //>>>   ##[do-something k1=;k2=;]
                message = ">>>   ##[do-something k1=v1;]msg";
                test = new ActionCommand("do-something")
                {
                    Data = "msg",
                };
                test.Properties.Add("k1", "v1");
                Assert.True(ActionCommand.TryParse(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "LoggingCommand")]
        public void CommandParserV2Test()
        {
            //Arrange
            using (var hc = new TestHostContext(this))
            {
                string message;
                ActionCommand test;
                ActionCommand verify;
                HashSet<string> commands = new HashSet<string>() { "do-something" };
                //::do-something k1=v1;]msg
                message = "::do-something k1=v1,::msg";
                test = new ActionCommand("do-something")
                {
                    Data = "msg",
                };
                test.Properties.Add("k1", "v1");
                Assert.True(ActionCommand.TryParseV2(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //::do-something::
                message = "::do-something::";
                test = new ActionCommand("do-something");
                Assert.True(ActionCommand.TryParseV2(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //::do-something k1=;=%2C=%0D=%0A=]=%3A,::;-%0D-%0A-]-:-,
                message = "::do-something k1=;=%2C=%0D=%0A=]=%3A,::;-%0D-%0A-]-:-,";
                test = new ActionCommand("do-something")
                {
                    Data = ";-\r-\n-]-:-,",
                };
                test.Properties.Add("k1", ";=,=\r=\n=]=:");
                Assert.True(ActionCommand.TryParseV2(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //::do-something k1=;=%252C=%250D=%250A=]=%253A,::;-%250D-%250A-]-:-,
                message = "::do-something k1=;=%252C=%250D=%250A=]=%253A,::;-%250D-%250A-]-:-,";
                test = new ActionCommand("do-something")
                {
                    Data = ";-%0D-%0A-]-:-,",
                };
                test.Properties.Add("k1", ";=%2C=%0D=%0A=]=%3A");
                Assert.True(ActionCommand.TryParseV2(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //::do-something k1=,k2=,::
                message = "::do-something k1=,k2=,::";
                test = new ActionCommand("do-something");
                Assert.True(ActionCommand.TryParseV2(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //::do-something k1=v1::
                message = "::do-something k1=v1::";
                test = new ActionCommand("do-something");
                test.Properties.Add("k1", "v1");
                Assert.True(ActionCommand.TryParseV2(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                test = null;
                verify = null;
                //   ::do-something k1=v1,::
                message = "   ::do-something k1=v1,::msg";
                test = new ActionCommand("do-something")
                {
                    Data = "msg",
                };
                test.Properties.Add("k1", "v1");
                Assert.True(ActionCommand.TryParseV2(message, commands, out verify));
                Assert.True(IsEqualCommand(hc, test, verify));

                message = "";
                verify = null;
                //   >>>   ::do-something k1=v1,::
                message = "   >>>   ::do-something k1=v1,::msg";
                Assert.False(ActionCommand.TryParseV2(message, commands, out verify));
            }
        }

        private bool IsEqualCommand(IHostContext hc, ActionCommand e1, ActionCommand e2)
        {
            try
            {
                if (!string.Equals(e1.Command, e2.Command, StringComparison.OrdinalIgnoreCase))
                {
                    hc.GetTrace("CommandEqual").Info("Command 1={0}, Command 2={1}", e1.Command, e2.Command);
                    return false;
                }

                if (!string.Equals(e1.Data, e2.Data, StringComparison.OrdinalIgnoreCase) && (!string.IsNullOrEmpty(e1.Data) && !string.IsNullOrEmpty(e2.Data)))
                {
                    hc.GetTrace("CommandEqual").Info("Data 1={0}, Data 2={1}", e1.Data, e2.Data);
                    return false;
                }

                if (e1.Properties.Count != e2.Properties.Count)
                {
                    hc.GetTrace("CommandEqual").Info("Logging events contain different numbers of Properties,{0} to {1}", e1.Properties.Count, e2.Properties.Count);
                    return false;
                }

                if (!e1.Properties.SequenceEqual(e2.Properties))
                {
                    hc.GetTrace("CommandEqual").Info("Logging events contain different Properties");
                    hc.GetTrace("CommandEqual").Info("Properties for event 1:");
                    foreach (var data in e1.Properties)
                    {
                        hc.GetTrace("CommandEqual").Info("Key={0}, Value={1}", data.Key, data.Value);
                    }

                    hc.GetTrace("CommandEqual").Info("Properties for event 2:");
                    foreach (var data in e2.Properties)
                    {
                        hc.GetTrace("CommandEqual").Info("Key={0}, Value={1}", data.Key, data.Value);
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                hc.GetTrace("CommandEqual").Info("Catch Exception during compare:{0}", ex.ToString());
                return false;
            }

            return true;
        }
    }
}

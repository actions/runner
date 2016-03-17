using Microsoft.VisualStudio.Services.Agent.Worker;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
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
                String vso;
                Command test;
                Command verify;
                //##vso[area.event k1=v1;]msg
                vso = "##vso[area.event k1=v1;]msg";
                test = new Command("area", "event")
                {
                    Data = "msg",
                };
                test.Properties.Add("k1", "v1");
                Assert.True(String.Equals(vso, test.ToString(), StringComparison.OrdinalIgnoreCase));
                Command.TryParse(vso, out verify);
                Assert.True(IsEqualCommand(hc, test, verify));

                vso = "";
                test = null;
                verify = null;
                //##vso[area.event]
                vso = "##vso[area.event]";
                test = new Command("area", "event");
                Assert.True(String.Equals(vso, test.ToString(), StringComparison.OrdinalIgnoreCase),
                    String.Format("Expect:{0}\nActual:{1}", vso, test.ToString()));
                Command.TryParse(vso, out verify);
                Assert.True(IsEqualCommand(hc, test, verify));

                vso = "";
                test = null;
                verify = null;
                //##vso[area.event k1=%3B=%0D=%0A;]%3B-%0D-%0A
                vso = "##vso[area.event k1=%3B=%0D=%0A;]%3B-%0D-%0A";
                test = new Command("area", "event")
                {
                    Data = ";-\r-\n",
                };
                test.Properties.Add("k1", ";=\r=\n");
                Assert.True(String.Equals(vso, test.ToString(), StringComparison.OrdinalIgnoreCase));
                Command.TryParse(vso, out verify);
                Assert.True(IsEqualCommand(hc, test, verify));

                vso = "";
                test = null;
                verify = null;
                //##vso[area.event k1=;k2=;]
                vso = "##vso[area.event k1=;k2=;]";
                test = new Command("area", "event");
                test.Properties.Add("k1", "");
                test.Properties.Add("k2", null);
                Assert.True(String.Equals("##vso[area.event]", test.ToString(), StringComparison.OrdinalIgnoreCase));
                Command.TryParse(vso, out verify);
                test = new Command("area", "event");
                Assert.True(IsEqualCommand(hc, test, verify));

                vso = "";
                test = null;
                verify = null;
                //>>>   ##vso[area.event k1=;k2=;]
                vso = ">>>   ##vso[area.event k1=v1;]msg";
                test = new Command("area", "event")
                {
                    Data = "msg",
                };
                test.Properties.Add("k1", "v1");
                Command.TryParse(vso, out verify);
                Assert.True(IsEqualCommand(hc, test, verify));
            }
        }

        private Boolean IsEqualCommand(IHostContext hc, Command e1, Command e2)
        {
            try
            {
                if (!String.Equals(e1.Area, e2.Area, StringComparison.OrdinalIgnoreCase))
                {
                    hc.GetTrace("CommandEqual").Info("Area 1={0}, Area 2={1}", e1.Area, e2.Area);
                    return false;
                }

                if (!String.Equals(e1.Event, e2.Event, StringComparison.OrdinalIgnoreCase))
                {
                    hc.GetTrace("CommandEqual").Info("Event 1={0}, Event 2={1}", e1.Event, e2.Event);
                    return false;
                }

                if (!String.Equals(e1.Data, e2.Data, StringComparison.OrdinalIgnoreCase) && (!String.IsNullOrEmpty(e1.Data) && !String.IsNullOrEmpty(e2.Data)))
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

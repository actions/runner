// using System;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using Microsoft.TeamFoundation.DistributedTask.WebApi;
// using Microsoft.VisualStudio.Services.Agent.Worker;
// using Moq;
// using Xunit;

// namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
// {
//     public sealed class TaskCommandExtensionL0
//     {
//         private TestHostContext _hc;
//         private Mock<IExecutionContext> _ec;
//         private ServiceEndpoint _endpoint;

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointAuthParameter()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             commandExtension.Initialize(_hc);
//             var cmd = new Command("task", "setEndpoint");
//             cmd.Data = "blah";
//             cmd.Properties.Add("field", "authParameter");
//             cmd.Properties.Add("id", Guid.Empty.ToString());
//             cmd.Properties.Add("key", "test");

//             commandExtension.ProcessCommand(_ec.Object, cmd);

//             Assert.Equal(_endpoint.Authorization.Parameters["test"], "blah");
//         }
        
//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointDataParameter()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             var cmd = new Command("task", "setEndpoint");
//             cmd.Data = "blah";
//             cmd.Properties.Add("field", "dataParameter");
//             cmd.Properties.Add("id", Guid.Empty.ToString());
//             cmd.Properties.Add("key", "test");

//             commandExtension.ProcessCommand(_ec.Object, cmd);

//             Assert.Equal(_endpoint.Data["test"], "blah");
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointUrlParameter()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             var cmd = new Command("task", "setEndpoint");
//             cmd.Data = "http://blah/";
//             cmd.Properties.Add("field", "url");
//             cmd.Properties.Add("id", Guid.Empty.ToString());

//             commandExtension.ProcessCommand(_ec.Object, cmd);

//             Assert.Equal(_endpoint.Url.ToString(), cmd.Data);
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointWithoutValue()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             var cmd = new Command("task", "setEndpoint");
//             Assert.Throws<Exception>(() => commandExtension.ProcessCommand(_ec.Object, cmd));
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointWithoutEndpointField()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             var cmd = new Command("task", "setEndpoint");

//             Assert.Throws<Exception>(() => commandExtension.ProcessCommand(_ec.Object, cmd));
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointInvalidEndpointField()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             var cmd = new Command("task", "setEndpoint");
//             cmd.Properties.Add("field", "blah");

//             Assert.Throws<Exception>(() => commandExtension.ProcessCommand(_ec.Object, cmd));
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointWithoutEndpointId()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             var cmd = new Command("task", "setEndpoint");
//             cmd.Properties.Add("field", "url");

//             Assert.Throws<Exception>(() => commandExtension.ProcessCommand(_ec.Object, cmd));
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointInvalidEndpointId()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             var cmd = new Command("task", "setEndpoint");
//             cmd.Properties.Add("field", "url");
//             cmd.Properties.Add("id", "blah");

//             Assert.Throws<Exception>(() => commandExtension.ProcessCommand(_ec.Object, cmd));
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointIdWithoutEndpointKey()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             var cmd = new Command("task", "setEndpoint");
//             cmd.Properties.Add("field", "authParameter");
//             cmd.Properties.Add("id", Guid.Empty.ToString());

//             Assert.Throws<Exception>(() => commandExtension.ProcessCommand(_ec.Object, cmd));
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public void SetEndpointUrlWithInvalidValue()
//         {
//             SetupMocks();
//             TaskCommandExtension commandExtension = new TaskCommandExtension();
//             var cmd = new Command("task", "setEndpoint");
//             cmd.Data = "blah";
//             cmd.Properties.Add("field", "url");
//             cmd.Properties.Add("id", Guid.Empty.ToString());

//             Assert.Throws<Exception>(() => commandExtension.ProcessCommand(_ec.Object, cmd));
//         }

//         private void SetupMocks([CallerMemberName] string name = "")
//         {
//             _hc = new TestHostContext(this, name);
//             _ec = new Mock<IExecutionContext>();

//             _endpoint = new ServiceEndpoint()
//             {
//                 Id = Guid.Empty,
//                 Url = new Uri("https://test.com"),
//                 Authorization = new EndpointAuthorization()
//                 {
//                     Scheme = "Test",
//                 }
//             };

//             _ec.Setup(x => x.Endpoints).Returns(new List<ServiceEndpoint> { _endpoint });
//         }
//     }
// }
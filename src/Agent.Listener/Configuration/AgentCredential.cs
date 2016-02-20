using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    public abstract class AgentCredential
    {
        public abstract void ReadCredential(IHostContext context, Dictionary<string, string> args, Boolean isUnattended);

        protected virtual String ReadCredentialConfig(
            IHostContext context,
            string parameterName,
            bool isSecret,
            Dictionary<String, ArgumentMetaData> metaData,
            Dictionary<String, String> args,
            Boolean isUnattended)
        {
            TraceSource m_trace = context.Trace["AgentCrendialManager"];
            m_trace.Info("Reading credential configuration {0}", parameterName);
            return context.GetService<IConsoleWizard>()
                .GetConfigurationValue(context, parameterName, metaData, args, isUnattended);
        }
    }

    public sealed class TokenCredential : AgentCredential
    {
        private Dictionary<String, ArgumentMetaData> CredentialMetaData = new Dictionary<String, ArgumentMetaData>
                                                                               {
                                                                                   {
                                                                                       "Token",
                                                                                       new ArgumentMetaData
                                                                                           {
                                                                                               Description = "Personal Access Token",
                                                                                               IsSercret = true,
                                                                                               Validator = Validators.NonEmptyValidator
                                                                                           }
                                                                                   }
                                                                               };
        public String Token { get; private set; }

        public override void ReadCredential(IHostContext context, Dictionary<String, String> args, Boolean isUnattended)
        {
            this.Token = this.ReadCredentialConfig(context, "Token", true, this.CredentialMetaData, args, isUnattended);
        }
    }

    public enum AuthScheme
    {
        Unknown,
        Pat
    }
}
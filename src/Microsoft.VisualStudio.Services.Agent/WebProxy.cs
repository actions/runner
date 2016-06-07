using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.IO;
using System.Net;

namespace Microsoft.VisualStudio.Services.Agent
{
    public class WebProxy : IWebProxy
    {
        private static bool _proxySettingsApplied = false;

        public WebProxy(Uri proxyAddress)
        {
            if (proxyAddress == null)
            {
                throw new ArgumentNullException(nameof(proxyAddress));
            }

            ProxyAddress = proxyAddress;
        }

        public Uri ProxyAddress { get; private set; }

        public ICredentials Credentials { get; set; }

        public Uri GetProxy(Uri destination) => ProxyAddress;

        public bool IsBypassed(Uri uri)
        { 
            return false;
        }        

        public static void ApplyProxySettings()
        {
            if (_proxySettingsApplied)
            {
                return;
            }
            _proxySettingsApplied = true;
            var proxyFilePath = IOUtil.GetProxyConfigFilePath();
            bool proxyConfigured = (new FileInfo(proxyFilePath)).Exists;
            if (!proxyConfigured)
            {
                return;
            }
            string proxyURI = File.ReadAllText(proxyFilePath);
            VssHttpMessageHandler.DefaultWebProxy = new WebProxy(new Uri(proxyURI));
        }
    }
}

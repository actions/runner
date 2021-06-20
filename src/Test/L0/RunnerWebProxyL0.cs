using GitHub.Runner.Common.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using System;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Tests
{
    public sealed class RunnerWebProxyL0
    {
        private static readonly Regex NewHttpClientHandlerRegex = new Regex("New\\s+HttpClientHandler\\s*\\(", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NewHttpClientRegex = new Regex("New\\s+HttpClient\\s*\\(\\s*\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly List<string> SkippedFiles = new List<string>()
        {
            "Runner.Common\\HostContext.cs",
            "Runner.Common/HostContext.cs",
            "Runner.Common\\HttpClientHandlerFactory.cs",
            "Runner.Common/HttpClientHandlerFactory.cs"
        };

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void IsNotUseRawHttpClientHandler()
        {
            List<string> sourceFiles = Directory.GetFiles(
                    TestUtil.GetProjectPath("Runner.Common"),
                    "*.cs",
                    SearchOption.AllDirectories).ToList();
            sourceFiles.AddRange(Directory.GetFiles(
                     TestUtil.GetProjectPath("Runner.Listener"),
                     "*.cs",
                     SearchOption.AllDirectories));
            sourceFiles.AddRange(Directory.GetFiles(
                    TestUtil.GetProjectPath("Runner.Worker"),
                    "*.cs",
                    SearchOption.AllDirectories));

            List<string> badCode = new List<string>();
            foreach (string sourceFile in sourceFiles)
            {
                // Skip skipped files.
                if (SkippedFiles.Any(s => sourceFile.Contains(s)))
                {
                    continue;
                }

                // Skip files in the obj directory.
                if (sourceFile.Contains(StringUtil.Format("{0}obj{0}", Path.DirectorySeparatorChar)))
                {
                    continue;
                }

                int lineCount = 0;
                foreach (string line in File.ReadAllLines(sourceFile))
                {
                    lineCount++;
                    if (NewHttpClientHandlerRegex.IsMatch(line))
                    {
                        badCode.Add($"{sourceFile} (line {lineCount})");
                    }
                }
            }

            Assert.True(badCode.Count == 0, $"The following code is using Raw HttpClientHandler() which will not follow the proxy setting agent have. Please use HostContext.CreateHttpClientHandler() instead.\n {string.Join("\n", badCode)}");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void IsNotUseRawHttpClient()
        {
            List<string> sourceFiles = Directory.GetFiles(
                    TestUtil.GetProjectPath("Runner.Common"),
                    "*.cs",
                    SearchOption.AllDirectories).ToList();
            sourceFiles.AddRange(Directory.GetFiles(
                     TestUtil.GetProjectPath("Runner.Listener"),
                     "*.cs",
                     SearchOption.AllDirectories));
            sourceFiles.AddRange(Directory.GetFiles(
                    TestUtil.GetProjectPath("Runner.Worker"),
                    "*.cs",
                    SearchOption.AllDirectories));

            List<string> badCode = new List<string>();
            foreach (string sourceFile in sourceFiles)
            {
                // Skip skipped files.
                if (SkippedFiles.Any(s => sourceFile.Contains(s)))
                {
                    continue;
                }

                // Skip files in the obj directory.
                if (sourceFile.Contains(StringUtil.Format("{0}obj{0}", Path.DirectorySeparatorChar)))
                {
                    continue;
                }

                int lineCount = 0;
                foreach (string line in File.ReadAllLines(sourceFile))
                {
                    lineCount++;
                    if (NewHttpClientRegex.IsMatch(line))
                    {
                        badCode.Add($"{sourceFile} (line {lineCount})");
                    }
                }
            }

            Assert.True(badCode.Count == 0, $"The following code is using Raw HttpClient() which will not follow the proxy setting agent have. Please use New HttpClient(HostContext.CreateHttpClientHandler()) instead.\n {string.Join("\n", badCode)}");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariables()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://127.0.0.1:8888");
                Environment.SetEnvironmentVariable("https_proxy", "http://user:pass@127.0.0.1:9999");
                Environment.SetEnvironmentVariable("no_proxy", "github.com, google.com,");
                var proxy = new RunnerWebProxy();

                Assert.Equal("http://127.0.0.1:8888", proxy.HttpProxyAddress);
                Assert.Null(proxy.HttpProxyUsername);
                Assert.Null(proxy.HttpProxyPassword);

                Assert.Equal("http://user:pass@127.0.0.1:9999", proxy.HttpsProxyAddress);
                Assert.Equal("user", proxy.HttpsProxyUsername);
                Assert.Equal("pass", proxy.HttpsProxyPassword);

                Assert.Equal(2, proxy.NoProxyList.Count);
                Assert.Equal("github.com", proxy.NoProxyList[0].Host);
                Assert.Equal("google.com", proxy.NoProxyList[1].Host);
            }
            finally
            {
                CleanProxyEnv();
            }
        }

#if !OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesPreferLowerCase()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://127.0.0.1:7777");
                Environment.SetEnvironmentVariable("HTTP_PROXY", "http://127.0.0.1:8888");
                Environment.SetEnvironmentVariable("https_proxy", "http://user:pass@127.0.0.1:8888");
                Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://user:pass@127.0.0.1:9999");
                Environment.SetEnvironmentVariable("no_proxy", "github.com,  github.com  ");
                Environment.SetEnvironmentVariable("NO_PROXY", "github.com, google.com,");
                var proxy = new RunnerWebProxy();

                Assert.Equal("http://127.0.0.1:7777", proxy.HttpProxyAddress);
                Assert.Null(proxy.HttpProxyUsername);
                Assert.Null(proxy.HttpProxyPassword);

                Assert.Equal("http://user:pass@127.0.0.1:8888", proxy.HttpsProxyAddress);
                Assert.Equal("user", proxy.HttpsProxyUsername);
                Assert.Equal("pass", proxy.HttpsProxyPassword);

                Assert.Equal(1, proxy.NoProxyList.Count);
                Assert.Equal("github.com", proxy.NoProxyList[0].Host);
            }
            finally
            {
                CleanProxyEnv();
            }
        }
#endif

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesInvalidString()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "127.0.0.1:7777");
                Environment.SetEnvironmentVariable("https_proxy", "127.0.0.1");
                var proxy = new RunnerWebProxy();

                Assert.Null(proxy.HttpProxyAddress);
                Assert.Null(proxy.HttpProxyUsername);
                Assert.Null(proxy.HttpProxyPassword);

                Assert.Null(proxy.HttpsProxyAddress);
                Assert.Null(proxy.HttpsProxyUsername);
                Assert.Null(proxy.HttpsProxyPassword);

                Assert.Equal(0, proxy.NoProxyList.Count);
            }
            finally
            {
                CleanProxyEnv();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesProxyCredentials()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://user1@127.0.0.1:8888");
                Environment.SetEnvironmentVariable("https_proxy", "http://user2:pass@127.0.0.1:9999");
                Environment.SetEnvironmentVariable("no_proxy", "github.com, google.com,");
                var proxy = new RunnerWebProxy();

                Assert.Equal("http://user1@127.0.0.1:8888", proxy.HttpProxyAddress);
                Assert.Equal("user1", proxy.HttpProxyUsername);
                Assert.Null(proxy.HttpProxyPassword);

                var cred = proxy.Credentials.GetCredential(new Uri("http://user1@127.0.0.1:8888"), "Basic");
                Assert.Equal("user1", cred.UserName);
                Assert.Equal(string.Empty, cred.Password);

                Assert.Equal("http://user2:pass@127.0.0.1:9999", proxy.HttpsProxyAddress);
                Assert.Equal("user2", proxy.HttpsProxyUsername);
                Assert.Equal("pass", proxy.HttpsProxyPassword);

                cred = proxy.Credentials.GetCredential(new Uri("http://user2:pass@127.0.0.1:9999"), "Basic");
                Assert.Equal("user2", cred.UserName);
                Assert.Equal("pass", cred.Password);

                Assert.Equal(2, proxy.NoProxyList.Count);
                Assert.Equal("github.com", proxy.NoProxyList[0].Host);
                Assert.Equal("google.com", proxy.NoProxyList[1].Host);
            }
            finally
            {
                CleanProxyEnv();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesProxyCredentialsEncoding()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://user1:pass1%40@127.0.0.1:8888");
                Environment.SetEnvironmentVariable("https_proxy", "http://user2:pass2%40@127.0.0.1:9999");
                Environment.SetEnvironmentVariable("no_proxy", "github.com, google.com,");
                var proxy = new RunnerWebProxy();

                Assert.Equal("http://user1:pass1%40@127.0.0.1:8888", proxy.HttpProxyAddress);
                Assert.Equal("user1", proxy.HttpProxyUsername);
                Assert.Equal("pass1@", proxy.HttpProxyPassword);

                var cred = proxy.Credentials.GetCredential(new Uri("http://user1:pass1%40@127.0.0.1:8888"), "Basic");
                Assert.Equal("user1", cred.UserName);
                Assert.Equal("pass1@", cred.Password);

                Assert.Equal("http://user2:pass2%40@127.0.0.1:9999", proxy.HttpsProxyAddress);
                Assert.Equal("user2", proxy.HttpsProxyUsername);
                Assert.Equal("pass2@", proxy.HttpsProxyPassword);

                cred = proxy.Credentials.GetCredential(new Uri("http://user2:pass2%40@127.0.0.1:9999"), "Basic");
                Assert.Equal("user2", cred.UserName);
                Assert.Equal("pass2@", cred.Password);

                Assert.Equal(2, proxy.NoProxyList.Count);
                Assert.Equal("github.com", proxy.NoProxyList[0].Host);
                Assert.Equal("google.com", proxy.NoProxyList[1].Host);
            }
            finally
            {
                CleanProxyEnv();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesByPassEmptyProxy()
        {
            var proxy = new RunnerWebProxy();
            Assert.True(proxy.IsBypassed(new Uri("https://github.com")));
            Assert.True(proxy.IsBypassed(new Uri("https://github.com")));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesGetProxyEmptyHttpProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("https_proxy", "http://user2:pass2%40@127.0.0.1:9999");
                var proxy = new RunnerWebProxy();

                Assert.Null(proxy.GetProxy(new Uri("http://github.com")));
                Assert.Null(proxy.GetProxy(new Uri("http://example.com:444")));

                Assert.Equal("http://user2:pass2%40@127.0.0.1:9999/", proxy.GetProxy(new Uri("https://something.com")).AbsoluteUri);
                Assert.Equal("http://user2:pass2%40@127.0.0.1:9999/", proxy.GetProxy(new Uri("https://www.something2.com")).AbsoluteUri);
            }
            finally
            {
                CleanProxyEnv();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesGetProxyEmptyHttpsProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://user1:pass1%40@127.0.0.1:8888");
                var proxy = new RunnerWebProxy();

                Assert.Null(proxy.GetProxy(new Uri("https://github.com/owner/repo")));
                Assert.Null(proxy.GetProxy(new Uri("https://mails.google.com")));

                Assert.Equal("http://user1:pass1%40@127.0.0.1:8888/", proxy.GetProxy(new Uri("http://something.com")).AbsoluteUri);
                Assert.Equal("http://user1:pass1%40@127.0.0.1:8888/", proxy.GetProxy(new Uri("http://www.something2.com")).AbsoluteUri);
            }
            finally
            {
                CleanProxyEnv();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesNoProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://user1:pass1%40@127.0.0.1:8888");
                Environment.SetEnvironmentVariable("https_proxy", "http://user2:pass2%40@127.0.0.1:9999");
                Environment.SetEnvironmentVariable("no_proxy", "github.com, .google.com, example.com:444, 192.168.0.123:123, 192.168.1.123");
                var proxy = new RunnerWebProxy();

                Assert.False(proxy.IsBypassed(new Uri("https://actions.com")));
                Assert.False(proxy.IsBypassed(new Uri("https://ggithub.com")));
                Assert.False(proxy.IsBypassed(new Uri("https://github.comm")));
                Assert.False(proxy.IsBypassed(new Uri("https://google.com")));
                Assert.False(proxy.IsBypassed(new Uri("https://example.com")));
                Assert.False(proxy.IsBypassed(new Uri("http://example.com:333")));
                Assert.False(proxy.IsBypassed(new Uri("http://192.168.0.123:123")));
                Assert.False(proxy.IsBypassed(new Uri("http://192.168.1.123/home")));

                Assert.True(proxy.IsBypassed(new Uri("https://github.com")));
                Assert.True(proxy.IsBypassed(new Uri("https://GITHUB.COM")));
                Assert.True(proxy.IsBypassed(new Uri("https://github.com/owner/repo")));
                Assert.True(proxy.IsBypassed(new Uri("https://actions.github.com")));
                Assert.True(proxy.IsBypassed(new Uri("https://mails.google.com")));
                Assert.True(proxy.IsBypassed(new Uri("https://MAILS.GOOGLE.com")));
                Assert.True(proxy.IsBypassed(new Uri("https://mails.v2.google.com")));
                Assert.True(proxy.IsBypassed(new Uri("http://mails.v2.v3.google.com/inbox")));
                Assert.True(proxy.IsBypassed(new Uri("https://example.com:444")));
                Assert.True(proxy.IsBypassed(new Uri("http://example.com:444")));
                Assert.True(proxy.IsBypassed(new Uri("http://example.COM:444")));
            }
            finally
            {
                CleanProxyEnv();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesGetProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://user1:pass1%40@127.0.0.1:8888");
                Environment.SetEnvironmentVariable("https_proxy", "http://user2:pass2%40@127.0.0.1:9999");
                Environment.SetEnvironmentVariable("no_proxy", "github.com, .google.com, example.com:444");
                var proxy = new RunnerWebProxy();

                Assert.Null(proxy.GetProxy(new Uri("http://github.com")));
                Assert.Null(proxy.GetProxy(new Uri("https://github.com/owner/repo")));
                Assert.Null(proxy.GetProxy(new Uri("https://mails.google.com")));
                Assert.Null(proxy.GetProxy(new Uri("http://example.com:444")));


                Assert.Equal("http://user1:pass1%40@127.0.0.1:8888/", proxy.GetProxy(new Uri("http://something.com")).AbsoluteUri);
                Assert.Equal("http://user1:pass1%40@127.0.0.1:8888/", proxy.GetProxy(new Uri("http://www.something2.com")).AbsoluteUri);

                Assert.Equal("http://user2:pass2%40@127.0.0.1:9999/", proxy.GetProxy(new Uri("https://something.com")).AbsoluteUri);
                Assert.Equal("http://user2:pass2%40@127.0.0.1:9999/", proxy.GetProxy(new Uri("https://www.something2.com")).AbsoluteUri);
            }
            finally
            {
                CleanProxyEnv();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WebProxyFromEnvironmentVariablesWithPort80()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://127.0.0.1:80");
                Environment.SetEnvironmentVariable("https_proxy", "http://user:pass@127.0.0.1:80");
                Environment.SetEnvironmentVariable("no_proxy", "github.com, google.com,");
                var proxy = new RunnerWebProxy();

                Assert.Equal("http://127.0.0.1:80", Environment.GetEnvironmentVariable("http_proxy"));
                Assert.Null(proxy.HttpProxyUsername);
                Assert.Null(proxy.HttpProxyPassword);

                Assert.Equal("http://user:pass@127.0.0.1:80", Environment.GetEnvironmentVariable("https_proxy"));
                Assert.Equal("user", proxy.HttpsProxyUsername);
                Assert.Equal("pass", proxy.HttpsProxyPassword);

                Assert.Equal(2, proxy.NoProxyList.Count);
                Assert.Equal("github.com", proxy.NoProxyList[0].Host);
                Assert.Equal("google.com", proxy.NoProxyList[1].Host);
            }
            finally
            {
                CleanProxyEnv();
            }
        }

        private void CleanProxyEnv()
        {
            Environment.SetEnvironmentVariable("http_proxy", null);
            Environment.SetEnvironmentVariable("https_proxy", null);
            Environment.SetEnvironmentVariable("HTTP_PROXY", null);
            Environment.SetEnvironmentVariable("HTTPS_PROXY", null);
            Environment.SetEnvironmentVariable("no_proxy", null);
            Environment.SetEnvironmentVariable("NO_PROXY", null);
        }
    }
}

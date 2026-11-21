using System;
using System.Collections.Generic;
using System.IO;
using GitHub.Runner.Sdk;
using Xunit;

namespace GitHub.Runner.Common.Tests.Util
{
    public sealed class NetRcUtilL0 : IDisposable
    {
        private readonly string _tempDirectory;

        public NetRcUtilL0()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"netrc_{Path.GetRandomFileName()}");
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        private string WriteNetRc(string content)
        {
            var filePath = Path.Combine(_tempDirectory, Path.GetRandomFileName());
            File.WriteAllText(filePath, content);
            return filePath;
        }

        private static NetRcCredential GetCredential(string filePath, string host)
        {
            return NetRcUtil.ReadCredentials(filePath).TryGetValue(host, out var credential) ? credential : null;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_MultiLineEntry()
        {
            var filePath = WriteNetRc(@"
machine internal.cache
  login builder
  password hunter2
");

            var credential = GetCredential(filePath, "internal.cache");

            Assert.NotNull(credential);
            Assert.Equal("builder", credential.Login);
            Assert.Equal("hunter2", credential.Password);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_SingleLineEntries()
        {
            var filePath = WriteNetRc(@"machine one.example.com login a password b machine two.example.com login c password d");

            var one = GetCredential(filePath, "one.example.com");
            var two = GetCredential(filePath, "two.example.com");

            Assert.Equal("a", one.Login);
            Assert.Equal("b", one.Password);
            Assert.Equal("c", two.Login);
            Assert.Equal("d", two.Password);
        }

        [Theory]
        [InlineData("MACHINE private.service", true)]
        [InlineData("MaChInE private.service", true)]
        [InlineData("DEFAULT", false)]
        [InlineData("DeFaUlT", false)]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReadCredentials_MixedCaseEntryBoundariesDoNotMixCredentials(string nextEntry, bool hasPrivateMachine)
        {
            var filePath = WriteNetRc($"machine internal.cache login builder password cache-password\n{nextEntry} login private-user password private-password\n");
            var credentials = NetRcUtil.ReadCredentials(filePath);

            Assert.Equal(hasPrivateMachine ? 2 : 1, credentials.Count);
            Assert.Equal("builder", credentials["internal.cache"].Login);
            Assert.Equal("cache-password", credentials["internal.cache"].Password);
            if (hasPrivateMachine)
            {
                Assert.Equal("private-user", credentials["private.service"].Login);
                Assert.Equal("private-password", credentials["private.service"].Password);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_MixedCaseDirectivesPreserveValueCase()
        {
            var filePath = WriteNetRc("MaChInE internal.cache LoGiN BuildUser PaSsWoRd SecretPass AcCoUnT password");

            var credential = GetCredential(filePath, "internal.cache");

            Assert.NotNull(credential);
            Assert.Equal("BuildUser", credential.Login);
            Assert.Equal("SecretPass", credential.Password);
        }

        [Theory]
        [InlineData("machine one.example.com login a password b", true)]
        [InlineData("default login fallback password everywhere", false)]
        [InlineData("default login fallback password everywhere\nmachine one.example.com login a password b", true)]
        [InlineData("machine one.example.com login a password b\ndefault login fallback password everywhere", true)]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReadCredentials_RequiresExplicitMachineEntries(string contents, bool hasMachineEntry)
        {
            var credentials = NetRcUtil.ReadCredentials(WriteNetRc(contents));

            Assert.Equal(hasMachineEntry ? 1 : 0, credentials.Count);
            Assert.False(credentials.ContainsKey("unlisted.example.com"));
            if (hasMachineEntry)
            {
                Assert.Equal("a", credentials["one.example.com"].Login);
                Assert.Equal("b", credentials["one.example.com"].Password);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_HostMatchIsCaseInsensitive()
        {
            var filePath = WriteNetRc(@"machine Internal.Cache login a password b");

            Assert.NotNull(GetCredential(filePath, "internal.cache"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_FirstEntryWins()
        {
            var filePath = WriteNetRc(@"
machine one.example.com login first password firstpw
machine one.example.com login second password secondpw
");

            var credential = GetCredential(filePath, "one.example.com");

            Assert.Equal("first", credential.Login);
            Assert.Equal("firstpw", credential.Password);
        }

        [Theory]
        [InlineData("macdef")]
        [InlineData("MaCdEf")]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_MacroBodyIsSkipped(string directive)
        {
            var filePath = WriteNetRc($@"
{directive} init
machine bogus.example.com login trap password trap

machine real.example.com login a password b
");

            Assert.Null(GetCredential(filePath, "bogus.example.com"));
            Assert.NotNull(GetCredential(filePath, "real.example.com"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_EntryWithoutPassword_ReturnsNull()
        {
            var filePath = WriteNetRc(@"machine one.example.com login a");

            Assert.Null(GetCredential(filePath, "one.example.com"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_MissingFile_ReturnsNull()
        {
            Assert.Null(GetCredential(Path.Combine(_tempDirectory, "does-not-exist"), "one.example.com"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_CommentsDoNotOverrideActiveEntries()
        {
            var filePath = WriteNetRc(@"
# machine internal.cache login obsolete password obsolete
machine internal.cache login builder password correct # password obsolete
# macdef ignored
machine other.cache login other password otherpw
");

            var credential = GetCredential(filePath, "internal.cache");
            Assert.Equal("builder", credential.Login);
            Assert.Equal("correct", credential.Password);
            Assert.Equal("otherpw", GetCredential(filePath, "other.cache").Password);
        }

        [Theory]
        [InlineData("plain#password", "plain#password")]
        [InlineData("#secret", "#secret")]
        [InlineData("\"two words\"", "two words")]
        [InlineData("\"two # words\"", "two # words")]
        [InlineData("\"quote\\\"slash\\\\line\\nreturn\\rtab\\t\"", "quote\"slash\\line\nreturn\rtab\t")]
        [InlineData("\"\"", "")]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_PreservesPasswordCharacters(string encodedPassword, string expectedPassword)
        {
            var filePath = WriteNetRc($"machine internal.cache login \"build user\" password {encodedPassword}\n");

            var credential = GetCredential(filePath, "internal.cache");

            Assert.Equal("build user", credential.Login);
            Assert.Equal(expectedPassword, credential.Password);
        }

        [Theory]
        [InlineData("correct")]
        [InlineData("#secret")]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredential_ValuesMayFollowOnTheNextLine(string password)
        {
            var filePath = WriteNetRc($"machine\ninternal.cache\nlogin\nbuilder\npassword\n{password}\n");

            var credential = GetCredential(filePath, "internal.cache");

            Assert.Equal("builder", credential.Login);
            Assert.Equal(password, credential.Password);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReadCredentials_HashPasswordDoesNotConsumeTheNextMachine()
        {
            var filePath = WriteNetRc("machine internal.cache login builder password #secret # password ignored\nmachine private.service login private-user password private-password\n");

            var credentials = NetRcUtil.ReadCredentials(filePath);

            Assert.Equal(2, credentials.Count);
            Assert.Equal("builder", credentials["internal.cache"].Login);
            Assert.Equal("#secret", credentials["internal.cache"].Password);
            Assert.Equal("private-user", credentials["private.service"].Login);
            Assert.Equal("private-password", credentials["private.service"].Password);
        }

        [Theory]
        [InlineData("\"unterminated")]
        [InlineData("\"unterminated\n")]
        [InlineData("\"trailing\\")]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReadCredentials_MalformedFileDoesNotReturnPartialCredentials(string password)
        {
            var filePath = WriteNetRc($"machine other.cache login other password otherpw\nmachine internal.cache login builder password {password}");
            var warnings = new List<string>();

            Assert.Empty(NetRcUtil.ReadCredentials(filePath, warnings.Add));
            var warning = Assert.Single(warnings);
            Assert.Contains(filePath, warning);
            Assert.Contains("invalid netrc syntax", warning);
            Assert.DoesNotContain("otherpw", warning);
            Assert.DoesNotContain(password, warning);
        }

        [Theory]
        [InlineData("missing", "file does not exist")]
        [InlineData("directory", "access was denied")]
        [InlineData("invalid", "invalid file path")]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReadCredentials_ReportsFileReadFailures(string failure, string expectedReason)
        {
            var path = failure switch
            {
                "directory" => _tempDirectory,
                "invalid" => Path.Combine(_tempDirectory, "invalid\0path"),
                _ => Path.Combine(_tempDirectory, "does-not-exist")
            };
            var warnings = new List<string>();

            Assert.Empty(NetRcUtil.ReadCredentials(path, warnings.Add));
            var warning = Assert.Single(warnings);
            Assert.Contains(path, warning);
            Assert.Contains(expectedReason, warning);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("machine other.cache login other password otherpw")]
        [InlineData("default login fallback password fallbackpw")]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReadCredentials_NoFileOrMatchingEntryDoesNotWarn(string contents)
        {
            var path = contents == null ? null : WriteNetRc(contents);
            var warnings = new List<string>();

            var credentials = NetRcUtil.ReadCredentials(path, warnings.Add);

            Assert.False(credentials.ContainsKey("internal.cache"));
            Assert.Empty(warnings);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ResolveFilePath_HonorsNetRcEnvironmentVariable()
        {
            var filePath = WriteNetRc(@"machine one.example.com login a password b");
            var originalValue = Environment.GetEnvironmentVariable("NETRC");
            try
            {
                Environment.SetEnvironmentVariable("NETRC", filePath);
                Assert.Equal(filePath, NetRcUtil.ResolveFilePath());

                var missingFile = Path.Combine(_tempDirectory, "does-not-exist");
                Environment.SetEnvironmentVariable("NETRC", missingFile);
                Assert.Equal(missingFile, NetRcUtil.ResolveFilePath());
            }
            finally
            {
                Environment.SetEnvironmentVariable("NETRC", originalValue);
            }
        }
    }
}

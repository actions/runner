using System;
using GitHub.Runner.Worker;
using GitHub.Services.WebApi;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class IssueMatcherL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_Loop_MayNotBeSetOnSinglePattern()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^error: (.+)$"",
          ""message"": 1,
          ""loop"": true
        }
      ]
    }
  ]
}
");
            Assert.Throws<ArgumentException>(() => config.Validate());

            // Sanity test
            config.Matchers[0].Patterns = new[]
            {
                new IssuePatternConfig
                {
                    Pattern = "^file: (.+)$",
                    File = 1,
                },
                config.Matchers[0].Patterns[0],
            };
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_Loop_OnlyAllowedOnLastPattern()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^(error)$"",
          ""severity"": 1
        },
        {
          ""regexp"": ""^file: (.+)$"",
          ""file"": 1,
          ""loop"": true
        },
        {
          ""regexp"": ""^error: (.+)$"",
          ""message"": 1
        }
      ]
    }
  ]
}
");
            Assert.Throws<ArgumentException>(() => config.Validate());

            // Sanity test
            config.Matchers[0].Patterns[1].Loop = false;
            config.Matchers[0].Patterns[2].Loop = true;
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_Loop_MustSetMessage()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^file: (.+)$"",
          ""message"": 1
        },
        {
          ""regexp"": ""^file: (.+)$"",
          ""file"": 1,
          ""loop"": true
        }
      ]
    }
  ]
}
");

            Assert.Throws<ArgumentException>(() => config.Validate());

            config.Matchers[0].Patterns[1].Loop = false;
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_Message_AllowedInFirstPattern()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^file: (.+)$"",
          ""message"": 1
        },
        {
          ""regexp"": ""^error: (.+)$"",
          ""file"": 1
        }
      ]
    }
  ]
}
");
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_Message_Required()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^error: (.+)$"",
          ""file"": 1
        }
      ]
    }
  ]
}
");
            Assert.Throws<ArgumentException>(() => config.Validate());

            // Sanity test
            config.Matchers[0].Patterns[0].File = null;
            config.Matchers[0].Patterns[0].Message = 1;
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_Owner_Distinct()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^error: (.+)$"",
          ""message"": 1
        }
      ]
    },
    {
      ""owner"": ""MYmatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^ERR: (.+)$"",
          ""message"": 1
        }
      ]
    }
  ]
}
");
            Assert.Throws<ArgumentException>(() => config.Validate());

            // Sanity test
            config.Matchers[0].Owner = "asdf";
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_Owner_Required()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": """",
      ""pattern"": [
        {
          ""regexp"": ""^error: (.+)$"",
          ""message"": 1
        }
      ]
    }
  ]
}
");
            Assert.Throws<ArgumentException>(() => config.Validate());

            // Sanity test
            config.Matchers[0].Owner = "asdf";
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_Pattern_Required()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
      ]
    }
  ]
}
");
            Assert.Throws<ArgumentException>(() => config.Validate());

            // Sanity test
            config.Matchers[0].Patterns = new[]
            {
                new IssuePatternConfig
                {
                    Pattern = "^error: (.+)$",
                    Message = 1,
                }
            };
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_PropertyMayNotBeSetTwice()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^severity: (.+)$"",
          ""file"": 1
        },
        {
          ""regexp"": ""^file: (.+)$"",
          ""file"": 1
        },
        {
          ""regexp"": ""^(.+)$"",
          ""message"": 1
        }
      ]
    }
  ]
}
");
            Assert.Throws<ArgumentException>(() => config.Validate());

            // Sanity test
            config.Matchers[0].Patterns[0].File = null;
            config.Matchers[0].Patterns[0].Severity = 1;
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_PropertyOutOfRange()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^(.+)$"",
          ""message"": 2
        }
      ]
    }
  ]
}
");
            Assert.Throws<ArgumentException>(() => config.Validate());

            // Sanity test
            config.Matchers[0].Patterns[0].Message = 1;
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Config_Validate_PropertyOutOfRange_LessThanZero()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^(.+)$"",
          ""message"": -1
        }
      ]
    }
  ]
}
");
            Assert.Throws<ArgumentException>(() => config.Validate());

            // Sanity test
            config.Matchers[0].Patterns[0].Message = 1;
            config.Validate();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_MultiplePatterns_DefaultSeverity()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""severity"": ""warning"",
      ""pattern"": [
        {
          ""regexp"": ""^(ERROR)?(?: )?(.+):$"",
          ""severity"": 1,
          ""code"": 2
        },
        {
          ""regexp"": ""^(.+)$"",
          ""message"": 1
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));

            var match = matcher.Match("ABC:");
            match = matcher.Match("not-working");
            Assert.Equal("warning", match.Severity);
            Assert.Equal("ABC", match.Code);
            Assert.Equal("not-working", match.Message);

            match = matcher.Match("ERROR ABC:");
            match = matcher.Match("not-working");
            Assert.Equal("ERROR", match.Severity);
            Assert.Equal("ABC", match.Code);
            Assert.Equal("not-working", match.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_MultiplePatterns_Loop_AccumulatesStatePerLine()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^(.+)$"",
          ""file"": 1
        },
        {
          ""regexp"": ""^(.+)$"",
          ""code"": 1
        },
        {
          ""regexp"": ""^message:(.+)$"",
          ""message"": 1,
          ""loop"": true
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));
            var match = matcher.Match("file1");
            Assert.Null(match);
            match = matcher.Match("code1");
            Assert.Null(match);
            match = matcher.Match("message:message1");
            Assert.Equal("file1", match.File);
            Assert.Equal("code1", match.Code);
            Assert.Equal("message1", match.Message);
            match = matcher.Match("message:message1-2"); // sanity check loop
            Assert.Equal("file1", match.File);
            Assert.Equal("code1", match.Code);
            Assert.Equal("message1-2", match.Message);
            match = matcher.Match("abc"); // discarded
            match = matcher.Match("file2");
            Assert.Null(match);
            match = matcher.Match("code2");
            Assert.Null(match);
            match = matcher.Match("message:message2");
            Assert.Equal("file2", match.File);
            Assert.Equal("code2", match.Code);
            Assert.Equal("message2", match.Message);
            match = matcher.Match("abc"); // discarded
            match = matcher.Match("abc"); // discarded
            match = matcher.Match("file3");
            Assert.Null(match);
            match = matcher.Match("code3");
            Assert.Null(match);
            match = matcher.Match("message:message3");
            Assert.Equal("file3", match.File);
            Assert.Equal("code3", match.Code);
            Assert.Equal("message3", match.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_MultiplePatterns_Loop_BrokenMatchClearsState()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^(.+)$"",
          ""file"": 1
        },
        {
          ""regexp"": ""^(.+)$"",
          ""severity"": 1
        },
        {
          ""regexp"": ""^message:(.+)$"",
          ""message"": 1,
          ""loop"": true
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));
            var match = matcher.Match("my-file.cs"); // file
            Assert.Null(match);
            match = matcher.Match("real-bad"); // severity
            Assert.Null(match);
            match = matcher.Match("message:not-working"); // message
            Assert.Equal("my-file.cs", match.File);
            Assert.Equal("real-bad", match.Severity);
            Assert.Equal("not-working", match.Message);
            match = matcher.Match("message:problem"); // message
            Assert.Equal("my-file.cs", match.File);
            Assert.Equal("real-bad", match.Severity);
            Assert.Equal("problem", match.Message);
            match = matcher.Match("other-file.cs"); // file - breaks the loop
            Assert.Null(match);
            match = matcher.Match("message:not-good"); // severity - also matches the message pattern, therefore
            Assert.Null(match);                        // guarantees sufficient previous state has been cleared
            match = matcher.Match("message:broken"); // message
            Assert.Equal("other-file.cs", match.File);
            Assert.Equal("message:not-good", match.Severity);
            Assert.Equal("broken", match.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_MultiplePatterns_Loop_ExtractsProperties()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^file:(.+) fromPath:(.+)$"",
          ""file"": 1,
          ""fromPath"": 2
        },
        {
          ""regexp"": ""^severity:(.+)$"",
          ""severity"": 1
        },
        {
          ""regexp"": ""^line:(.+) column:(.+) code:(.+) message:(.+)$"",
          ""line"": 1,
          ""column"": 2,
          ""code"": 3,
          ""message"": 4,
          ""loop"": true
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));
            var match = matcher.Match("file:my-file.cs fromPath:my-project.proj");
            Assert.Null(match);
            match = matcher.Match("severity:real-bad");
            Assert.Null(match);
            match = matcher.Match("line:123 column:45 code:uh-oh message:not-working");
            Assert.Equal("my-file.cs", match.File);
            Assert.Equal("my-project.proj", match.FromPath);
            Assert.Equal("real-bad", match.Severity);
            Assert.Equal("123", match.Line);
            Assert.Equal("45", match.Column);
            Assert.Equal("uh-oh", match.Code);
            Assert.Equal("not-working", match.Message);
            match = matcher.Match("line:234 column:56 code:yikes message:broken");
            Assert.Equal("my-file.cs", match.File);
            Assert.Equal("my-project.proj", match.FromPath);
            Assert.Equal("real-bad", match.Severity);
            Assert.Equal("234", match.Line);
            Assert.Equal("56", match.Column);
            Assert.Equal("yikes", match.Code);
            Assert.Equal("broken", match.Message);
            match = matcher.Match("line:345 column:67 code:failed message:cant-do-that");
            Assert.Equal("my-file.cs", match.File);
            Assert.Equal("my-project.proj", match.FromPath);
            Assert.Equal("real-bad", match.Severity);
            Assert.Equal("345", match.Line);
            Assert.Equal("67", match.Column);
            Assert.Equal("failed", match.Code);
            Assert.Equal("cant-do-that", match.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_MultiplePatterns_NonLoop_AccumulatesStatePerLine()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^(.+)$"",
          ""file"": 1
        },
        {
          ""regexp"": ""^(.+)$"",
          ""code"": 1
        },
        {
          ""regexp"": ""^message:(.+)$"",
          ""message"": 1
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));
            var match = matcher.Match("file1");
            Assert.Null(match);
            match = matcher.Match("code1");
            Assert.Null(match);
            match = matcher.Match("message:message1");
            Assert.Equal("file1", match.File);
            Assert.Equal("code1", match.Code);
            Assert.Equal("message1", match.Message);
            match = matcher.Match("abc"); // discarded
            match = matcher.Match("file2");
            Assert.Null(match);
            match = matcher.Match("code2");
            Assert.Null(match);
            match = matcher.Match("message:message2");
            Assert.Equal("file2", match.File);
            Assert.Equal("code2", match.Code);
            Assert.Equal("message2", match.Message);
            match = matcher.Match("abc"); // discarded
            match = matcher.Match("abc"); // discarded
            match = matcher.Match("file3");
            Assert.Null(match);
            match = matcher.Match("code3");
            Assert.Null(match);
            match = matcher.Match("message:message3");
            Assert.Equal("file3", match.File);
            Assert.Equal("code3", match.Code);
            Assert.Equal("message3", match.Message);
            match = matcher.Match("message:message3"); // sanity check not loop
            Assert.Null(match);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_MultiplePatterns_NonLoop_DoesNotLoop()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^file:(.+)$"",
          ""file"": 1
        },
        {
          ""regexp"": ""^message:(.+)$"",
          ""message"": 1
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));
            var match = matcher.Match("file:my-file.cs");
            Assert.Null(match);
            match = matcher.Match("message:not-working");
            Assert.Equal("my-file.cs", match.File);
            Assert.Equal("not-working", match.Message);
            match = matcher.Match("message:not-working");
            Assert.Null(match);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_MultiplePatterns_NonLoop_ExtractsProperties()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^file:(.+) fromPath:(.+)$"",
          ""file"": 1,
          ""fromPath"": 2
        },
        {
          ""regexp"": ""^severity:(.+)$"",
          ""severity"": 1
        },
        {
          ""regexp"": ""^line:(.+) column:(.+) code:(.+) message:(.+)$"",
          ""line"": 1,
          ""column"": 2,
          ""code"": 3,
          ""message"": 4
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));
            var match = matcher.Match("file:my-file.cs fromPath:my-project.proj");
            Assert.Null(match);
            match = matcher.Match("severity:real-bad");
            Assert.Null(match);
            match = matcher.Match("line:123 column:45 code:uh-oh message:not-working");
            Assert.Equal("my-file.cs", match.File);
            Assert.Equal("my-project.proj", match.FromPath);
            Assert.Equal("real-bad", match.Severity);
            Assert.Equal("123", match.Line);
            Assert.Equal("45", match.Column);
            Assert.Equal("uh-oh", match.Code);
            Assert.Equal("not-working", match.Message);
            match = matcher.Match("line:123 column:45 code:uh-oh message:not-working"); // sanity check not loop
            Assert.Null(match);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_MultiplePatterns_NonLoop_MatchClearsState()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^(.+)$"",
          ""file"": 1
        },
        {
          ""regexp"": ""^(.+)$"",
          ""severity"": 1
        },
        {
          ""regexp"": ""^(.+)$"",
          ""message"": 1
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));
            var match = matcher.Match("my-file.cs"); // file
            Assert.Null(match);
            match = matcher.Match("real-bad"); // severity
            Assert.Null(match);
            match = matcher.Match("not-working"); // message
            Assert.Equal("my-file.cs", match.File);
            Assert.Equal("real-bad", match.Severity);
            Assert.Equal("not-working", match.Message);
            match = matcher.Match("other-file.cs"); // file
            Assert.Null(match);
            match = matcher.Match("not-good"); // severity
            Assert.Null(match);
            match = matcher.Match("broken"); // message
            Assert.Equal("other-file.cs", match.File);
            Assert.Equal("not-good", match.Severity);
            Assert.Equal("broken", match.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_SetsOwner()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^(.+)$"",
          ""message"": 1
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));
            Assert.Equal("myMatcher", matcher.Owner);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_SinglePattern_DefaultSeverity()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""severity"": ""warning"",
      ""pattern"": [
        {
          ""regexp"": ""^(ERROR)?(?: )?(.+): (.+)$"",
          ""severity"": 1,
          ""code"": 2,
          ""message"": 3
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));

            var match = matcher.Match("ABC: not-working");
            Assert.Equal("warning", match.Severity);
            Assert.Equal("ABC", match.Code);
            Assert.Equal("not-working", match.Message);

            match = matcher.Match("ERROR ABC: not-working");
            Assert.Equal("ERROR", match.Severity);
            Assert.Equal("ABC", match.Code);
            Assert.Equal("not-working", match.Message);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matcher_SinglePattern_ExtractsProperties()
        {
            var config = JsonUtility.FromString<IssueMatchersConfig>(@"
{
  ""problemMatcher"": [
    {
      ""owner"": ""myMatcher"",
      ""pattern"": [
        {
          ""regexp"": ""^file:(.+) line:(.+) column:(.+) severity:(.+) code:(.+) message:(.+) fromPath:(.+)$"",
          ""file"": 1,
          ""line"": 2,
          ""column"": 3,
          ""severity"": 4,
          ""code"": 5,
          ""message"": 6,
          ""fromPath"": 7
        }
      ]
    }
  ]
}
");
            config.Validate();
            var matcher = new IssueMatcher(config.Matchers[0], TimeSpan.FromSeconds(1));
            var match = matcher.Match("file:my-file.cs line:123 column:45 severity:real-bad code:uh-oh message:not-working fromPath:my-project.proj");
            Assert.Equal("my-file.cs", match.File);
            Assert.Equal("123", match.Line);
            Assert.Equal("45", match.Column);
            Assert.Equal("real-bad", match.Severity);
            Assert.Equal("uh-oh", match.Code);
            Assert.Equal("not-working", match.Message);
            Assert.Equal("my-project.proj", match.FromPath);
        }
    }
}

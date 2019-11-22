using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;
using DTWebApi = GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class OutputManagerL0
    {
        private Mock<IExecutionContext> _executionContext;
        private Mock<IActionCommandManager> _commandManager;
        private Variables _variables;
        private OnMatcherChanged _onMatcherChanged;
        private List<Tuple<DTWebApi.Issue, string>> _issues;
        private List<string> _messages;
        private List<string> _commands;
        private OutputManager _outputManager;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void AddMatcher_Clobber()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "ERROR: (.+)",
                                Message = 1,
                            },
                        },
                    },
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-2",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "NOT GOOD: (.+)",
                                Message = 1,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("ERROR: message 1");
                Process("NOT GOOD: message 2");
                Add(new IssueMatcherConfig
                {
                    Owner = "my-matcher-1",
                    Patterns = new[]
                    {
                        new IssuePatternConfig
                        {
                            Pattern = "ERROR: (.+) END MESSAGE",
                            Message = 1,
                        },
                    },
                });
                Process("ERROR: message 3 END MESSAGE");
                Process("ERROR: message 4");
                Process("NOT GOOD: message 5");
                Assert.Equal(4, _issues.Count);
                Assert.Equal("message 1", _issues[0].Item1.Message);
                Assert.Equal("message 2", _issues[1].Item1.Message);
                Assert.Equal("message 3", _issues[2].Item1.Message);
                Assert.Equal("message 5", _issues[3].Item1.Message);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(1, _messages.Count);
                Assert.Equal("ERROR: message 4", _messages[0]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void AddMatcher_Prepend()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "ERROR: (.+)",
                                Message = 1,
                            },
                        },
                    },
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-2",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "NOT GOOD: (.+)",
                                Message = 1,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("ERROR: message 1");
                Process("NOT GOOD: message 2");
                Add(new IssueMatcherConfig
                {
                    Owner = "new-matcher",
                    Patterns = new[]
                    {
                        new IssuePatternConfig
                        {
                            Pattern = "ERROR: (.+) END MESSAGE",
                            Message = 1,
                        },
                    },
                });
                Process("ERROR: message 3 END MESSAGE");
                Process("ERROR: message 4");
                Process("NOT GOOD: message 5");
                Assert.Equal(5, _issues.Count);
                Assert.Equal("message 1", _issues[0].Item1.Message);
                Assert.Equal("message 2", _issues[1].Item1.Message);
                Assert.Equal("message 3", _issues[2].Item1.Message);
                Assert.Equal("message 4", _issues[3].Item1.Message);
                Assert.Equal("message 5", _issues[4].Item1.Message);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(0, _messages.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherCode()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = @"(.*): (.+)",
                                Code = 1,
                                Message = 2,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("BAD: real bad");
                Process(": not working");
                Assert.Equal(2, _issues.Count);
                Assert.Equal("real bad", _issues[0].Item1.Message);
                Assert.Equal("BAD", _issues[0].Item1.Data["code"]);
                Assert.Equal("not working", _issues[1].Item1.Message);
                Assert.False(_issues[1].Item1.Data.ContainsKey("code"));
                Assert.Equal(0, _commands.Count);
                Assert.Equal(0, _messages.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotResetMatchingMatcher()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "Start: .+",
                            },
                            new IssuePatternConfig
                            {
                                Pattern = "Error: (.+)",
                                Message = 1,
                                Loop = true,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("Start: hello");
                Process("Error: it broke");
                Process("Error: oh no");
                Process("Error: not good");
                Process("regular message 1");
                Process("Start: hello again");
                Process("Error: it broke again");
                Process("Error: real bad");
                Process("regular message 2");
                Assert.Equal(5, _issues.Count);
                Assert.Equal("it broke", _issues[0].Item1.Message);
                Assert.Equal("oh no", _issues[1].Item1.Message);
                Assert.Equal("not good", _issues[2].Item1.Message);
                Assert.Equal("it broke again", _issues[3].Item1.Message);
                Assert.Equal("real bad", _issues[4].Item1.Message);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(4, _messages.Count);
                Assert.Equal("Start: hello", _messages[0]);
                Assert.Equal("regular message 1", _messages[1]);
                Assert.Equal("Start: hello again", _messages[2]);
                Assert.Equal("regular message 2", _messages[3]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InitialMatchers()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "ERROR: (.+)",
                                Message = 1,
                            },
                        },
                    },
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-2",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "NOT GOOD: (.+)",
                                Message = 1,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("ERROR: it is broken");
                Process("NOT GOOD: that did not work");
                Assert.Equal(2, _issues.Count);
                Assert.Equal("it is broken", _issues[0].Item1.Message);
                Assert.Equal("that did not work", _issues[1].Item1.Message);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(0, _messages.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherLineColumn()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = @"\((.+),(.+)\): (.+)",
                                Line = 1,
                                Column = 2,
                                Message = 3,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("(12,34): real bad");
                Process("(12,thirty-four): it is broken");
                Process("(twelve,34): not working");
                Assert.Equal(3, _issues.Count);
                Assert.Equal("real bad", _issues[0].Item1.Message);
                Assert.Equal("12", _issues[0].Item1.Data["line"]);
                Assert.Equal("34", _issues[0].Item1.Data["col"]);
                Assert.Equal("it is broken", _issues[1].Item1.Message);
                Assert.Equal("12", _issues[1].Item1.Data["line"]);
                Assert.False(_issues[1].Item1.Data.ContainsKey("col"));
                Assert.Equal("not working", _issues[2].Item1.Message);
                Assert.False(_issues[2].Item1.Data.ContainsKey("line"));
                Assert.Equal("34", _issues[2].Item1.Data["col"]);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(2, _messages.Count);
                Assert.Equal("##[debug]Unable to parse column number 'thirty-four'", _messages[0]);
                Assert.Equal("##[debug]Unable to parse line number 'twelve'", _messages[1]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherDoesNotReceiveCommand()
        {
            using (Setup())
            using (_outputManager)
            {
                Add(new IssueMatcherConfig
                {
                    Owner = "my-matcher",
                    Patterns = new[]
                    {
                        new IssuePatternConfig
                        {
                            Pattern = "ERROR: (.+)",
                            Message = 1,
                        },
                    },
                });
                Process("this line is an ERROR: it is broken");
                Process("##[some-command]this line is a command even though it contains ERROR: not working");
                Process("this line is a command too ##[some-command]even though it contains ERROR: not working again");
                Process("##[not-command]this line is an ERROR: it is broken again");
                Assert.Equal(2, _issues.Count);
                Assert.Equal("it is broken", _issues[0].Item1.Message);
                Assert.Equal("it is broken again", _issues[1].Item1.Message);
                Assert.Equal(2, _commands.Count);
                Assert.Equal("##[some-command]this line is a command even though it contains ERROR: not working", _commands[0]);
                Assert.Equal("this line is a command too ##[some-command]even though it contains ERROR: not working again", _commands[1]);
                Assert.Equal(0, _messages.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherRemoveColorCodes()
        {
            using (Setup())
            using (_outputManager)
            {
                Add(new IssueMatcherConfig
                {
                    Owner = "my-matcher",
                    Patterns = new[]
                    {
                        new IssuePatternConfig
                        {
                            Pattern = "^the error: (.+)$",
                            Message = 1,
                        },
                    },
                });
                Process("the error: \033[31mred, \033[1;31mbright red, \033[mreset");
                Assert.Equal(1, _issues.Count);
                Assert.Equal("red, bright red, reset", _issues[0].Item1.Message);
                Assert.Equal("the error: red, bright red, reset", _issues[0].Item2);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(0, _messages.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RemoveMatcher()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "ERROR: (.+)",
                                Message = 1,
                            },
                        },
                    },
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-2",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "NOT GOOD: (.+)",
                                Message = 1,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("ERROR: message 1");
                Process("NOT GOOD: message 2");
                Remove("my-matcher-1");
                Process("ERROR: message 3");
                Process("NOT GOOD: message 4");
                Assert.Equal(3, _issues.Count);
                Assert.Equal("message 1", _issues[0].Item1.Message);
                Assert.Equal("message 2", _issues[1].Item1.Message);
                Assert.Equal("message 4", _issues[2].Item1.Message);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(1, _messages.Count);
                Assert.Equal("ERROR: message 3", _messages[0]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResetsOtherMatchers()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "Matches both line 1: .+",
                            },
                            new IssuePatternConfig
                            {
                                Pattern = "Matches 1 only line 2: (.+)",
                                Message = 1,
                            },
                        },
                    },
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-2",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "Matches both line 1: (.+)",
                            },
                            new IssuePatternConfig
                            {
                                Pattern = "(.+)",
                                Message = 1,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("Matches both line 1: hello");
                Process("Matches 1 only line 2: it broke");
                Process("regular message 1");
                Process("regular message 2");
                Process("Matches both line 1: hello again");
                Process("oh no, another error");
                Assert.Equal(2, _issues.Count);
                Assert.Equal("it broke", _issues[0].Item1.Message);
                Assert.Equal("oh no, another error", _issues[1].Item1.Message);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(4, _messages.Count);
                Assert.Equal("Matches both line 1: hello", _messages[0]);
                Assert.Equal("regular message 1", _messages[1]);
                Assert.Equal("regular message 2", _messages[2]);
                Assert.Equal("Matches both line 1: hello again", _messages[3]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherSeverity()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "(.*): (.+)",
                                Severity = 1,
                                Message = 2,
                            },
                        },
                    },
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-2",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = "ERROR! (.+)",
                                Message = 1,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("ERRor: real bad");
                Process("WARNing: not great");
                Process("info: hey");
                Process(": not working");
                Process("ERROR! uh oh");
                Assert.Equal(4, _issues.Count);
                Assert.Equal("real bad", _issues[0].Item1.Message);
                Assert.Equal(DTWebApi.IssueType.Error, _issues[0].Item1.Type);
                Assert.Equal("not great", _issues[1].Item1.Message);
                Assert.Equal(DTWebApi.IssueType.Warning, _issues[1].Item1.Type);
                Assert.Equal("not working", _issues[2].Item1.Message);
                Assert.Equal(DTWebApi.IssueType.Error, _issues[2].Item1.Type);
                Assert.Equal("uh oh", _issues[3].Item1.Message);
                Assert.Equal(DTWebApi.IssueType.Error, _issues[3].Item1.Type);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(2, _messages.Count);
                Assert.StartsWith("##[debug]Skipped", _messages[0]);
                Assert.Contains("'info'", _messages[0]);
                Assert.Equal("info: hey", _messages[1]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherTimeout()
        {
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_ISSUE_MATCHER_TIMEOUT", "0:0:0.01");
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "email",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$",
                                Message = 0,
                            },
                        },
                    },
                    new IssueMatcherConfig
                    {
                        Owner = "err",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = @"ERR: (.+)",
                                Message = 1,
                            },
                        },
                    },
                },
            };
            using (Setup(matchers: matchers))
            using (_outputManager)
            {
                Process("john.doe@contoso.com");
                Process("t@t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.c%20");
                Process("jane.doe@contoso.com");
                Process("ERR: this error");
                Assert.Equal(3, _issues.Count);
                Assert.Equal("john.doe@contoso.com", _issues[0].Item1.Message);
                Assert.Contains("Removing issue matcher 'email'", _issues[1].Item1.Message);
                Assert.Equal("this error", _issues[2].Item1.Message);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(2, _messages.Where(x => x.StartsWith("##[debug]Timeout processing issue matcher")).Count());
                Assert.Equal(1, _messages.Where(x => x.Equals("t@t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.t.c%20")).Count());
                Assert.Equal(1, _messages.Where(x => x.StartsWith("jane.doe@contoso.com")).Count());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherFile()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = @"(.+): (.+)",
                                File = 1,
                                Message = 2,
                            },
                        },
                    },
                },
            };
            using (var hostContext = Setup(matchers: matchers))
            using (_outputManager)
            {
                // Setup github.workspace
                var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
                ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
                Directory.CreateDirectory(workDirectory);
                var workspaceDirectory = Path.Combine(workDirectory, "workspace");
                Directory.CreateDirectory(workspaceDirectory);
                _executionContext.Setup(x => x.GetGitHubContext("workspace")).Returns(workspaceDirectory);

                // Create a test file
                Directory.CreateDirectory(Path.Combine(workspaceDirectory, "some-directory"));
                var filePath = Path.Combine(workspaceDirectory, "some-directory", "some-file.txt");
                File.WriteAllText(filePath, "");

                // Process
                Process("some-directory/some-file.txt: some error");
                Assert.Equal(1, _issues.Count);
                Assert.Equal("some error", _issues[0].Item1.Message);
                Assert.Equal("some-directory/some-file.txt", _issues[0].Item1.Data["file"]);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(0, _messages.Count);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherFileExists()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = @"(.+): (.+)",
                                File = 1,
                                Message = 2,
                            },
                        },
                    },
                },
            };
            using (var hostContext = Setup(matchers: matchers))
            using (_outputManager)
            {
                // Setup github.workspace
                var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
                ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
                Directory.CreateDirectory(workDirectory);
                var workspaceDirectory = Path.Combine(workDirectory, "workspace");
                Directory.CreateDirectory(workspaceDirectory);
                _executionContext.Setup(x => x.GetGitHubContext("workspace")).Returns(workspaceDirectory);

                // Create a test file
                File.WriteAllText(Path.Combine(workspaceDirectory, "some-file-1.txt"), "");

                // Process
                Process("some-file-1.txt: some error 1"); // file exists
                Process("some-file-2.txt: some error 2"); // file does not exist

                Assert.Equal(2, _issues.Count);
                Assert.Equal("some error 1", _issues[0].Item1.Message);
                Assert.Equal("some-file-1.txt", _issues[0].Item1.Data["file"]);
                Assert.Equal("some error 2", _issues[1].Item1.Message);
                Assert.False(_issues[1].Item1.Data.ContainsKey("file")); // does not contain file key
                Assert.Equal(0, _commands.Count);
                Assert.Equal(0, _messages.Where(x => !x.StartsWith("##[debug]")).Count());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherFileOutsideRepository()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = @"(.+): (.+)",
                                File = 1,
                                Message = 2,
                            },
                        },
                    },
                },
            };
            using (var hostContext = Setup(matchers: matchers))
            using (_outputManager)
            {
                // Setup github.workspace
                var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
                ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
                Directory.CreateDirectory(workDirectory);
                var workspaceDirectory = Path.Combine(workDirectory, "workspace");
                Directory.CreateDirectory(workspaceDirectory);
                _executionContext.Setup(x => x.GetGitHubContext("workspace")).Returns(workspaceDirectory);

                // Create test files
                var filePath1 = Path.Combine(workspaceDirectory, "some-file-1.txt");
                File.WriteAllText(filePath1, "");
                var workspaceSiblingDirectory = Path.Combine(Path.GetDirectoryName(workspaceDirectory), "workspace-sibling");
                Directory.CreateDirectory(workspaceSiblingDirectory);
                var filePath2 = Path.Combine(workspaceSiblingDirectory, "some-file-2.txt");
                File.WriteAllText(filePath2, "");

                // Process
                Process($"{filePath1}: some error 1"); // file exists inside workspace
                Process($"{filePath2}: some error 2"); // file exists outside workspace

                Assert.Equal(2, _issues.Count);
                Assert.Equal("some error 1", _issues[0].Item1.Message);
                Assert.Equal("some-file-1.txt", _issues[0].Item1.Data["file"]);
                Assert.Equal("some error 2", _issues[1].Item1.Message);
                Assert.False(_issues[1].Item1.Data.ContainsKey("file")); // does not contain file key
                Assert.Equal(0, _commands.Count);
                Assert.Equal(0, _messages.Where(x => !x.StartsWith("##[debug]")).Count());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void MatcherFromPath()
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    new IssueMatcherConfig
                    {
                        Owner = "my-matcher-1",
                        Patterns = new[]
                        {
                            new IssuePatternConfig
                            {
                                Pattern = @"(.+): (.+) \[(.+)\]",
                                File = 1,
                                Message = 2,
                                FromPath = 3,
                            },
                        },
                    },
                },
            };
            using (var hostContext = Setup(matchers: matchers))
            using (_outputManager)
            {
                // Setup github.workspace
                var workDirectory = hostContext.GetDirectory(WellKnownDirectory.Work);
                ArgUtil.NotNullOrEmpty(workDirectory, nameof(workDirectory));
                Directory.CreateDirectory(workDirectory);
                var workspaceDirectory = Path.Combine(workDirectory, "workspace");
                Directory.CreateDirectory(workspaceDirectory);
                _executionContext.Setup(x => x.GetGitHubContext("workspace")).Returns(workspaceDirectory);

                // Create a test file
                Directory.CreateDirectory(Path.Combine(workspaceDirectory, "some-directory"));
                Directory.CreateDirectory(Path.Combine(workspaceDirectory, "some-project", "some-directory"));
                var filePath = Path.Combine(workspaceDirectory, "some-project", "some-directory", "some-file.txt");
                File.WriteAllText(filePath, "");

                // Process
                Process("some-directory/some-file.txt: some error [some-project/some-project.proj]");
                Assert.Equal(1, _issues.Count);
                Assert.Equal("some error", _issues[0].Item1.Message);
                Assert.Equal("some-project/some-directory/some-file.txt", _issues[0].Item1.Data["file"]);
                Assert.Equal(0, _commands.Count);
                Assert.Equal(0, _messages.Count);
            }
        }

        private TestHostContext Setup(
            [CallerMemberName] string name = "",
            IssueMatchersConfig matchers = null)
        {
            matchers?.Validate();

            _onMatcherChanged = null;
            _issues = new List<Tuple<DTWebApi.Issue, string>>();
            _messages = new List<string>();
            _commands = new List<string>();

            var hostContext = new TestHostContext(this, name);

            _variables = new Variables(hostContext, new Dictionary<string, DTWebApi.VariableValue>());

            _executionContext = new Mock<IExecutionContext>();
            _executionContext.Setup(x => x.WriteDebug)
                .Returns(true);
            _executionContext.Setup(x => x.Variables)
                .Returns(_variables);
            _executionContext.Setup(x => x.GetMatchers())
                .Returns(matchers?.Matchers ?? new List<IssueMatcherConfig>());
            _executionContext.Setup(x => x.Add(It.IsAny<OnMatcherChanged>()))
                .Callback((OnMatcherChanged handler) =>
                {
                    _onMatcherChanged = handler;
                });
            _executionContext.Setup(x => x.AddIssue(It.IsAny<DTWebApi.Issue>(), It.IsAny<string>()))
                .Callback((DTWebApi.Issue issue, string logMessage) =>
                {
                    _issues.Add(new Tuple<DTWebApi.Issue, string>(issue, logMessage));
                });
            _executionContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string tag, string message) =>
                {
                    _messages.Add($"{tag}{message}");
                    hostContext.GetTrace().Info($"{tag}{message}");
                });

            _commandManager = new Mock<IActionCommandManager>();
            _commandManager.Setup(x => x.TryProcessCommand(It.IsAny<IExecutionContext>(), It.IsAny<string>()))
                .Returns((IExecutionContext executionContext, string line) =>
                {
                    if (line.IndexOf("##[some-command]") >= 0)
                    {
                        _commands.Add(line);
                        return true;
                    }

                    return false;
                });

            _outputManager = new OutputManager(_executionContext.Object, _commandManager.Object);
            return hostContext;
        }

        private void Add(IssueMatcherConfig matcher)
        {
            var matchers = new IssueMatchersConfig
            {
                Matchers =
                {
                    matcher,
                },
            };
            matchers.Validate();
            _onMatcherChanged(null, new MatcherChangedEventArgs(matcher));
        }

        private void Remove(string owner)
        {
            var matcher = new IssueMatcherConfig { Owner = owner };
            _onMatcherChanged(null, new MatcherChangedEventArgs(matcher));
        }

        private void Process(string line)
        {
            _outputManager.OnDataReceived(null, new ProcessDataReceivedEventArgs(line));
        }
    }
}

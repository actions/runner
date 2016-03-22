using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public sealed class VariablesL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CanSetAndGet()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), out warnings);

                // Act.
                variables.Set("foo", "bar");
                string actual = variables.Get("foo");

                // Assert.
                Assert.Equal("bar", actual); 
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ConstructorSetsNullAsEmpty()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var copy = new Dictionary<string, string>
                {
                    { "variable1", null },
                };

                // Act.
                var variables = new Variables(hc, copy, out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(string.Empty, variables.Get("variable1"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DetectsAdjacentCyclicalReference()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "variable1", "1_$(variable2)" },
                    { "variable2", "2_$(variable3)" },
                    { "variable3", "3_$(variable2)" },
                };

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, out warnings);

                // Assert.
                Assert.Equal(3, warnings.Count);
                Assert.True(warnings.Any(x => string.Equals(x, StringUtil.Loc("Variable0ContainsCyclicalReference", "variable1"))));
                Assert.True(warnings.Any(x => string.Equals(x, StringUtil.Loc("Variable0ContainsCyclicalReference", "variable2"))));
                Assert.True(warnings.Any(x => string.Equals(x, StringUtil.Loc("Variable0ContainsCyclicalReference", "variable3"))));
                Assert.Equal("1_$(variable2)", variables.Get("variable1"));
                Assert.Equal("2_$(variable3)", variables.Get("variable2"));
                Assert.Equal("3_$(variable2)", variables.Get("variable3"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DetectsExcessiveDepth()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                const int MaxDepth = 50;
                var copy = new Dictionary<string, string>();
                copy[$"variable{MaxDepth + 1}"] = "Final value"; // Variable 51.
                for (int i = 1 ; i <= MaxDepth ; i++)
                {
                    copy[$"variable{i}"] = $"$(variable{i + 1})"; // Variables 1-50.
                }

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, out warnings);

                // Assert.
                Assert.Equal(1, warnings.Count);
                Assert.Equal(warnings[0], StringUtil.Loc("Variable0ExceedsMaxDepth1", "variable1", MaxDepth));
                Assert.Equal("$(variable2)", variables.Get("variable1")); // Variable 1.
                for (int i = 2; i <= MaxDepth + 1; i++)
                {
                    Assert.Equal("Final value", variables.Get($"variable{i}")); // Variables 2-51.
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DetectsNonadjacentCyclicalReference()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "variable1", "1_$(variable2)" },
                    { "variable2", "2_$(variable3)" },
                    { "variable3", "3_$(variable1)" },
                };

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, out warnings);

                // Assert.
                Assert.Equal(3, warnings.Count);
                Assert.True(warnings.Any(x => string.Equals(x, StringUtil.Loc("Variable0ContainsCyclicalReference", "variable1"))));
                Assert.True(warnings.Any(x => string.Equals(x, StringUtil.Loc("Variable0ContainsCyclicalReference", "variable2"))));
                Assert.True(warnings.Any(x => string.Equals(x, StringUtil.Loc("Variable0ContainsCyclicalReference", "variable3"))));
                Assert.Equal("1_$(variable2)", variables.Get("variable1"));
                Assert.Equal("2_$(variable3)", variables.Get("variable2"));
                Assert.Equal("3_$(variable1)", variables.Get("variable3"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandHandlesNullValue()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "variable1", null },
                    { "variable2", "some variable 2 value" },
                };

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(string.Empty, variables.Get("variable1"));
                Assert.Equal("some variable 2 value", variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandHandlesNullNestedValue()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "variable1", "before $(variable2) after" },
                    { "variable2", null },
                };

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal("before  after", variables.Get("variable1"));
                Assert.Equal(string.Empty, variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandsTargetHandlesNullValue()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange: Setup the variables.
                List<string> warnings;
                var variableDictionary = new Dictionary<string, string>
                {
                    { "variable1", "some variable 1 value " },
                };
                var variables = new Variables(hc, variableDictionary, out warnings);

                // Arrange: Setup the target dictionary.
                var targetDictionary = new Dictionary<string, string>
                {
                    { "some target key", null },
                };

                // Act.
                variables.ExpandValues(target: targetDictionary);

                // Assert: The consecutive macros should both have been expanded.
                Assert.Equal(string.Empty, targetDictionary["some target key"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandsTargetValueWithConsecutiveMacros()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange: Setup the variables.
                List<string> warnings;
                var variableDictionary = new Dictionary<string, string>
                {
                    { "variable1", "some variable 1 value " },
                    { "variable2", "some variable 2 value" },
                };
                var variables = new Variables(hc, variableDictionary, out warnings);

                // Arrange: Setup the target dictionary.
                var targetDictionary = new Dictionary<string, string>();
                targetDictionary["some target key"] = "before $(variable1)$(variable2) after";

                // Act.
                variables.ExpandValues(target: targetDictionary);

                // Assert: The consecutive macros should both have been expanded.
                Assert.Equal("before some variable 1 value some variable 2 value after", targetDictionary["some target key"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandsTargetValueWithPreceedingPrefix()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange: Setup the variables.
                List<string> warnings;
                var variableDictionary = new Dictionary<string, string>
                {
                    { "variable1", "some variable 1 value" },
                };
                var variables = new Variables(hc, variableDictionary, out warnings);

                // Arrange: Setup the target dictionary.
                var targetDictionary = new Dictionary<string, string>();
                targetDictionary["some target key"] = "before $($(variable1) after";

                // Act.
                variables.ExpandValues(target: targetDictionary);

                // Assert: The consecutive macros should both have been expanded.
                Assert.Equal("before $(some variable 1 value after", targetDictionary["some target key"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandsTargetValueWithoutRecursion()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange: Setup the variables. The value of the variable1 variable
                // should not get expanded since variable2 does not exist when the
                // variables class is initialized (and therefore would never get expanded).
                List<string> warnings;
                var variableDictionary = new Dictionary<string, string>
                {
                    { "variable1", "$(variable2)" },
                };
                var variables = new Variables(hc, variableDictionary, out warnings);
                variables.Set("variable2", "some variable 2 value");

                // Arrange: Setup the target dictionary.
                var targetDictionary = new Dictionary<string, string>();
                targetDictionary["some target key"] = "before $(variable1) after";

                // Act.
                variables.ExpandValues(target: targetDictionary);

                // Assert: The variable should only have been expanded one level.
                Assert.Equal("before $(variable2) after", targetDictionary["some target key"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandsValueWithConsecutiveMacros()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "variable1", "before$(variable2)$(variable2)after" },
                    { "variable2", "some variable 2 value" },
                };

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal("beforesome variable 2 valuesome variable 2 valueafter", variables.Get("variable1"));
                Assert.Equal("some variable 2 value", variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandsValueWithDeepRecursion()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "variable1", "before$(variable2)after" },
                    { "variable2", "$(variable3)world" },
                    { "variable3", "hello" },
                };

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal("beforehelloworldafter", variables.Get("variable1"));
                Assert.Equal("helloworld", variables.Get("variable2"));
                Assert.Equal("hello", variables.Get("variable3"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandsValueWithPreceedingPrefix()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "variable1", "before$($(variable2)after" },
                    { "variable2", "hello" },
                };

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal("before$(helloafter", variables.Get("variable1"));
                Assert.Equal("hello", variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetBooleanDoesNotThrowWhenNull()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), out warnings);

                // Act.
                bool? actual = variables.GetBoolean("no such");

                // Assert.
                Assert.Null(actual); 
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetEnumDoesNotThrowWhenNull()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), out warnings);

                // Act.
                System.IO.FileShare? actual = variables.GetEnum<System.IO.FileShare>("no such");

                // Assert.
                Assert.Null(actual); 
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetsNullAsEmpty()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), out warnings);

                // Act.
                variables.Set("variable1", null);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(string.Empty, variables.Get("variable1"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SetsOrdinalIgnoreCaseComparer()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                CultureInfo currentCulture = CultureInfo.CurrentCulture;
                CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
                try
                {
                    CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                    CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                    var copy = new Dictionary<string, string>
                    {
                        { "i", "foo" },
                        { "I", "foo" },
                    };

                    // Act.
                    List<string> warnings;
                    var variables = new Variables(hc, copy, out warnings);

                    // Assert.
                    int count = 0;
                    IEnumerator enumerator = variables.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        count++;
                    }

                    Assert.Equal(1, count);
                }
                finally
                {
                    // Cleanup.
                    CultureInfo.CurrentCulture = currentCulture;
                    CultureInfo.CurrentUICulture = currentUICulture;
                }
            }
        }
    }
}

using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using System.Collections.Generic;
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
        public void Constructor_AppliesMaskHints()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "MySecretName", "My secret value" },
                    { "MyPublicVariable", "My public value" },
                };
                var maskHints = new List<MaskHint>
                {
                    new MaskHint() { Type = MaskType.Variable, Value = "MySecretName" },
                };
                List<string> warnings;
                var variables = new Variables(hc, copy, maskHints, out warnings);

                // Act.
                KeyValuePair<string, string>[] publicVariables = variables.Public.ToArray();

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(1, publicVariables.Length);
                Assert.Equal("MyPublicVariable", publicVariables[0].Key);
                Assert.Equal("My public value", publicVariables[0].Value);
                Assert.Equal("My secret value", variables.Get("MySecretName"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_DetectsAdjacentCyclicalReference()
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
                var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

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
        public void Constructor_DetectsExcessiveDepth()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                const int MaxDepth = 50;
                var copy = new Dictionary<string, string>();
                copy[$"variable{MaxDepth + 1}"] = "Final value"; // Variable 51.
                for (int i = 1; i <= MaxDepth; i++)
                {
                    copy[$"variable{i}"] = $"$(variable{i + 1})"; // Variables 1-50.
                }

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

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
        public void Constructor_DetectsNonadjacentCyclicalReference()
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
                var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

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
        public void Constructor_DoesNotApplyNonVariableMaskHintTypes()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                const string Name = "MyVar";
                const string Value = "some value";
                var copy = new Dictionary<string, string>
                {
                    { Name, Value }
                };
                var maskHints = new List<MaskHint>
                {
                    new MaskHint() { Type = MaskType.Regex, Value = Name },
                };
                List<string> warnings;
                var variables = new Variables(hc, copy, maskHints, out warnings);

                // Act.
                KeyValuePair<string, string>[] publicVariables = variables.Public.ToArray();

                // Assert.
                Assert.Equal(1, publicVariables.Length);
                Assert.Equal(Name, publicVariables[0].Key);
                Assert.Equal(Value, publicVariables[0].Value);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_InheritsSecretFlagFromDeepRecursion()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "variable1", "before $(variable2) after" },
                    { "variable2", "before2 $(variable3) after2" },
                    { "variable3", "some variable 3 value" },
                };
                var maskHints = new List<MaskHint>
                {
                    new MaskHint() { Type = MaskType.Variable, Value = "variable3" },
                };

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, maskHints, out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(0, variables.Public.Count());
                Assert.Equal("before before2 some variable 3 value after2 after", variables.Get("variable1"));
                Assert.Equal("before2 some variable 3 value after2", variables.Get("variable2"));
                Assert.Equal("some variable 3 value", variables.Get("variable3"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_InheritsSecretFlagFromRecursion()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "variable1", "before $(variable2) after" },
                    { "variable2", "some variable 2 value" },
                };
                var maskHints = new List<MaskHint>
                {
                    new MaskHint() { Type = MaskType.Variable, Value = "variable2" },
                };

                // Act.
                List<string> warnings;
                var variables = new Variables(hc, copy, maskHints, out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(0, variables.Public.Count());
                Assert.Equal("before some variable 2 value after", variables.Get("variable1"));
                Assert.Equal("some variable 2 value", variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_ExpandsValueWithConsecutiveMacros()
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
                var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal("beforesome variable 2 valuesome variable 2 valueafter", variables.Get("variable1"));
                Assert.Equal("some variable 2 value", variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_ExpandsValueWithDeepRecursion()
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
                var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

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
        public void Constructor_ExpandsValueWithPreceedingPrefix()
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
                var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal("before$(helloafter", variables.Get("variable1"));
                Assert.Equal("hello", variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_HandlesNullNestedValue()
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
                var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal("before  after", variables.Get("variable1"));
                Assert.Equal(string.Empty, variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_HandlesNullValue()
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
                var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(string.Empty, variables.Get("variable1"));
                Assert.Equal("some variable 2 value", variables.Get("variable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_SetsNullAsEmpty()
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
                var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(string.Empty, variables.Get("variable1"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_SetsOrdinalIgnoreCaseComparer()
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
                    var variables = new Variables(hc, copy, new List<MaskHint>(), out warnings);

                    // Assert.
                    Assert.Equal(1, variables.Public.Count());
                }
                finally
                {
                    // Cleanup.
                    CultureInfo.CurrentCulture = currentCulture;
                    CultureInfo.CurrentUICulture = currentUICulture;
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Constructor_SkipVariableWithEmptyName()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                var copy = new Dictionary<string, string>
                {
                    { "", "" },
                    { "   ", "" },
                    { "MyPublicVariable", "My public value" },
                };
                var maskHints = new List<MaskHint>();
                List<string> warnings;
                var variables = new Variables(hc, copy, maskHints, out warnings);

                // Act.
                KeyValuePair<string, string>[] publicVariables = variables.Public.ToArray();

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(1, publicVariables.Length);
                Assert.Equal("MyPublicVariable", publicVariables[0].Key);
                Assert.Equal("My public value", publicVariables[0].Value);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandValues_DoesNotRecurse()
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
                var variables = new Variables(hc, variableDictionary, new List<MaskHint>(), out warnings);
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
        public void ExpandValues_HandlesConsecutiveMacros()
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
                var variables = new Variables(hc, variableDictionary, new List<MaskHint>(), out warnings);

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
        public void ExpandValues_HandlesNullValue()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange: Setup the variables.
                List<string> warnings;
                var variableDictionary = new Dictionary<string, string>
                {
                    { "variable1", "some variable 1 value " },
                };
                var variables = new Variables(hc, variableDictionary, new List<MaskHint>(), out warnings);

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
        public void ExpandValues_HandlesPreceedingPrefix()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange: Setup the variables.
                List<string> warnings;
                var variableDictionary = new Dictionary<string, string>
                {
                    { "variable1", "some variable 1 value" },
                };
                var variables = new Variables(hc, variableDictionary, new List<MaskHint>(), out warnings);

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
        public void Get_ReturnsNullIfNotFound()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);

                // Act.
                string actual = variables.Get("no such");

                // Assert.
                Assert.Equal(null, actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetBoolean_DoesNotThrowWhenNull()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);

                // Act.
                bool? actual = variables.GetBoolean("no such");

                // Assert.
                Assert.Null(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetEnum_DoesNotThrowWhenNull()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);

                // Act.
                System.IO.FileShare? actual = variables.GetEnum<System.IO.FileShare>("no such");

                // Assert.
                Assert.Null(actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RecalculateExpanded_PerformsRecalculation()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var original = new Dictionary<string, string>
                {
                    { "topLevelVariable", "$(nestedVariable1) $(nestedVariable2)" },
                    { "nestedVariable1", "Some nested value 1" },
                };
                var variables = new Variables(hc, original, new List<MaskHint>(), out warnings);
                Assert.Equal(0, warnings.Count);
                Assert.Equal(2, variables.Public.Count());
                Assert.Equal("Some nested value 1 $(nestedVariable2)", variables.Get("topLevelVariable"));
                Assert.Equal("Some nested value 1", variables.Get("nestedVariable1"));

                // Act.
                variables.Set("nestedVariable2", "Some nested value 2", secret: false);
                variables.RecalculateExpanded(out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(3, variables.Public.Count());
                Assert.Equal("Some nested value 1 Some nested value 2", variables.Get("topLevelVariable"));
                Assert.Equal("Some nested value 1", variables.Get("nestedVariable1"));
                Assert.Equal("Some nested value 2", variables.Get("nestedVariable2"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RecalculateExpanded_RetainsUpdatedSecretness()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
                Assert.Equal(0, warnings.Count);
                variables.Set("foo", "bar");
                Assert.Equal(1, variables.Public.Count());

                // Act.
                variables.Set("foo", "baz", secret: true);
                variables.RecalculateExpanded(out warnings);

                // Assert.
                Assert.Equal(0, warnings.Count);
                Assert.Equal(0, variables.Public.Count());
                Assert.Equal("baz", variables.Get("foo"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Set_CanConvertAPublicValueIntoASecretValue()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
                variables.Set("foo", "bar");
                Assert.Equal(1, variables.Public.Count());

                // Act.
                variables.Set("foo", "baz", secret: true);

                // Assert.
                Assert.Equal(0, variables.Public.Count());
                Assert.Equal("baz", variables.Get("foo"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Set_CannotConvertASecretValueIntoAPublicValue()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);
                variables.Set("foo", "bar", secret: true);
                Assert.Equal(0, variables.Public.Count());
                Assert.Equal("bar", variables.Get("foo"));

                // Act.
                variables.Set("foo", "baz", secret: false);

                // Assert.
                Assert.Equal(0, variables.Public.Count());
                Assert.Equal("baz", variables.Get("foo"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Set_CanStoreANewSecret()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);

                // Act.
                variables.Set("foo", "bar", secret: true);

                // Assert.
                Assert.Equal(0, variables.Public.Count());
                Assert.Equal("bar", variables.Get("foo"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Set_CanUpdateASecret()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);

                // Act.
                variables.Set("foo", "bar", secret: true);
                variables.Set("foo", "baz", secret: true);

                // Assert.
                Assert.Equal(0, variables.Public.Count());
                Assert.Equal("baz", variables.Get("foo"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Set_StoresNullAsEmpty()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);

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
        public void Set_StoresValue()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                // Arrange.
                List<string> warnings;
                var variables = new Variables(hc, new Dictionary<string, string>(), new List<MaskHint>(), out warnings);

                // Act.
                variables.Set("foo", "bar");

                // Assert.
                Assert.Equal("bar", variables.Get("foo"));
            }
        }
    }
}

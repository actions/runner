using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Tests;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Dap;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapVariableProviderL0
    {
        private TestHostContext _hc;
        private DapVariableProvider _provider;

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            _hc = new TestHostContext(this, testName);
            _provider = new DapVariableProvider(_hc.SecretMasker);
            return _hc;
        }

        private Moq.Mock<GitHub.Runner.Worker.IExecutionContext> CreateMockContext(DictionaryContextData expressionValues)
        {
            var mock = new Moq.Mock<GitHub.Runner.Worker.IExecutionContext>();
            mock.Setup(x => x.ExpressionValues).Returns(expressionValues);
            return mock;
        }

        #region GetScopes tests

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetScopes_ReturnsEmptyWhenContextIsNull()
        {
            using (CreateTestContext())
            {
                var scopes = _provider.GetScopes(null);
                Assert.Empty(scopes);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetScopes_ReturnsOnlyPopulatedScopes()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "repository", new StringContextData("owner/repo") }
                };
                exprValues["env"] = new DictionaryContextData
                {
                    { "CI", new StringContextData("true") },
                    { "HOME", new StringContextData("/home/runner") }
                };
                // "runner" is not set — should not appear in scopes

                var ctx = CreateMockContext(exprValues);
                var scopes = _provider.GetScopes(ctx.Object);

                Assert.Equal(2, scopes.Count);
                Assert.Equal("github", scopes[0].Name);
                Assert.Equal("env", scopes[1].Name);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetScopes_ReportsNamedVariableCount()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["env"] = new DictionaryContextData
                {
                    { "A", new StringContextData("1") },
                    { "B", new StringContextData("2") },
                    { "C", new StringContextData("3") }
                };

                var ctx = CreateMockContext(exprValues);
                var scopes = _provider.GetScopes(ctx.Object);

                Assert.Single(scopes);
                Assert.Equal(3, scopes[0].NamedVariables);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetScopes_SecretsGetSpecialPresentationHint()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["secrets"] = new DictionaryContextData
                {
                    { "MY_SECRET", new StringContextData("super-secret") }
                };
                exprValues["env"] = new DictionaryContextData
                {
                    { "CI", new StringContextData("true") }
                };

                var ctx = CreateMockContext(exprValues);
                var scopes = _provider.GetScopes(ctx.Object);

                var envScope = scopes.Find(s => s.Name == "env");
                var secretsScope = scopes.Find(s => s.Name == "secrets");

                Assert.NotNull(envScope);
                Assert.Null(envScope.PresentationHint);

                Assert.NotNull(secretsScope);
                Assert.Equal("registers", secretsScope.PresentationHint);
            }
        }

        #endregion

        #region GetVariables — basic types

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_ReturnsEmptyWhenContextIsNull()
        {
            using (CreateTestContext())
            {
                var variables = _provider.GetVariables(null, 1);
                Assert.Empty(variables);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_ReturnsStringVariables()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["env"] = new DictionaryContextData
                {
                    { "CI", new StringContextData("true") },
                    { "HOME", new StringContextData("/home/runner") }
                };

                var ctx = CreateMockContext(exprValues);
                // "env" is at ScopeNames index 1 → variablesReference = 2
                var variables = _provider.GetVariables(ctx.Object, 2);

                Assert.Equal(2, variables.Count);

                var ciVar = variables.Find(v => v.Name == "CI");
                Assert.NotNull(ciVar);
                Assert.Equal("true", ciVar.Value);
                Assert.Equal("string", ciVar.Type);
                Assert.Equal(0, ciVar.VariablesReference);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_ReturnsBooleanVariables()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "event_name", new StringContextData("push") },
                };
                // Use a nested dict with boolean to test
                var jobDict = new DictionaryContextData();
                // BooleanContextData is a valid PipelineContextData type
                // but job context typically has strings. Use env scope instead.
                exprValues["env"] = new DictionaryContextData
                {
                    { "flag", new BooleanContextData(true) }
                };

                var ctx = CreateMockContext(exprValues);
                // "env" is at index 1 → ref 2
                var variables = _provider.GetVariables(ctx.Object, 2);

                var flagVar = variables.Find(v => v.Name == "flag");
                Assert.NotNull(flagVar);
                Assert.Equal("true", flagVar.Value);
                Assert.Equal("boolean", flagVar.Type);
                Assert.Equal(0, flagVar.VariablesReference);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_ReturnsNumberVariables()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["env"] = new DictionaryContextData
                {
                    { "count", new NumberContextData(42) }
                };

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 2);

                var countVar = variables.Find(v => v.Name == "count");
                Assert.NotNull(countVar);
                Assert.Equal("42", countVar.Value);
                Assert.Equal("number", countVar.Type);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_HandlesNullValues()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                var dict = new DictionaryContextData();
                dict["present"] = new StringContextData("yes");
                dict["missing"] = null;
                exprValues["env"] = dict;

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 2);

                var nullVar = variables.Find(v => v.Name == "missing");
                Assert.NotNull(nullVar);
                Assert.Equal("null", nullVar.Value);
                Assert.Equal("null", nullVar.Type);
                Assert.Equal(0, nullVar.VariablesReference);
            }
        }

        #endregion

        #region GetVariables — nested expansion

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_NestedDictionaryIsExpandable()
        {
            using (CreateTestContext())
            {
                var innerDict = new DictionaryContextData
                {
                    { "name", new StringContextData("push") },
                    { "ref", new StringContextData("refs/heads/main") }
                };
                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "event", innerDict }
                };

                var ctx = CreateMockContext(exprValues);
                // "github" is at index 0 → ref 1
                var variables = _provider.GetVariables(ctx.Object, 1);

                var eventVar = variables.Find(v => v.Name == "event");
                Assert.NotNull(eventVar);
                Assert.Equal("object", eventVar.Type);
                Assert.True(eventVar.VariablesReference > 0, "Nested dict should have a non-zero variablesReference");
                Assert.Equal(2, eventVar.NamedVariables);

                // Now expand it
                var children = _provider.GetVariables(ctx.Object, eventVar.VariablesReference);
                Assert.Equal(2, children.Count);

                var nameVar = children.Find(v => v.Name == "name");
                Assert.NotNull(nameVar);
                Assert.Equal("push", nameVar.Value);
                Assert.Equal("${{ github.event.name }}", nameVar.EvaluateName);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_NestedArrayIsExpandable()
        {
            using (CreateTestContext())
            {
                var array = new ArrayContextData();
                array.Add(new StringContextData("item0"));
                array.Add(new StringContextData("item1"));

                var exprValues = new DictionaryContextData();
                exprValues["env"] = new DictionaryContextData
                {
                    { "list", array }
                };

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 2);

                var listVar = variables.Find(v => v.Name == "list");
                Assert.NotNull(listVar);
                Assert.Equal("array", listVar.Type);
                Assert.True(listVar.VariablesReference > 0);
                Assert.Equal(2, listVar.IndexedVariables);

                // Expand the array
                var items = _provider.GetVariables(ctx.Object, listVar.VariablesReference);
                Assert.Equal(2, items.Count);
                Assert.Equal("[0]", items[0].Name);
                Assert.Equal("item0", items[0].Value);
                Assert.Equal("[1]", items[1].Name);
                Assert.Equal("item1", items[1].Value);
            }
        }

        #endregion

        #region Secret masking

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_SecretsScopeValuesAreRedacted()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["secrets"] = new DictionaryContextData
                {
                    { "MY_TOKEN", new StringContextData("ghp_abc123secret") },
                    { "DB_PASSWORD", new StringContextData("p@ssword!") }
                };

                var ctx = CreateMockContext(exprValues);
                // "secrets" is at index 5 → ref 6
                var variables = _provider.GetVariables(ctx.Object, 6);

                Assert.Equal(2, variables.Count);
                foreach (var v in variables)
                {
                    Assert.Equal("***", v.Value);
                    Assert.Equal("string", v.Type);
                }

                // Keys should still be visible
                Assert.Contains(variables, v => v.Name == "MY_TOKEN");
                Assert.Contains(variables, v => v.Name == "DB_PASSWORD");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_NonSecretScopeValuesMaskedBySecretMasker()
        {
            using (var hc = CreateTestContext())
            {
                // Register a known secret value with the masker
                hc.SecretMasker.AddValue("super-secret-token");

                var exprValues = new DictionaryContextData();
                exprValues["env"] = new DictionaryContextData
                {
                    { "SAFE", new StringContextData("hello world") },
                    { "LEAKED", new StringContextData("prefix-super-secret-token-suffix") }
                };

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 2);

                var safeVar = variables.Find(v => v.Name == "SAFE");
                Assert.NotNull(safeVar);
                Assert.Equal("hello world", safeVar.Value);

                var leakedVar = variables.Find(v => v.Name == "LEAKED");
                Assert.NotNull(leakedVar);
                Assert.DoesNotContain("super-secret-token", leakedVar.Value);
                Assert.Contains("***", leakedVar.Value);
            }
        }

        #endregion

        #region Reset

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Reset_InvalidatesNestedReferences()
        {
            using (CreateTestContext())
            {
                var innerDict = new DictionaryContextData
                {
                    { "name", new StringContextData("push") }
                };
                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "event", innerDict }
                };

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 1);
                var eventVar = variables.Find(v => v.Name == "event");
                Assert.True(eventVar.VariablesReference > 0);

                var savedRef = eventVar.VariablesReference;

                // Reset should clear all dynamic references
                _provider.Reset();

                var children = _provider.GetVariables(ctx.Object, savedRef);
                Assert.Empty(children);
            }
        }

        #endregion

        #region EvaluateName

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_SetsEvaluateNameWithDotPath()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "repository", new StringContextData("owner/repo") }
                };

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 1);

                var repoVar = variables.Find(v => v.Name == "repository");
                Assert.NotNull(repoVar);
                Assert.Equal("${{ github.repository }}", repoVar.EvaluateName);
            }
        }

        #endregion

        #region EvaluateExpression

        /// <summary>
        /// Creates a mock execution context with Global set up so that
        /// ToPipelineTemplateEvaluator() works for real expression evaluation.
        /// </summary>
        private Moq.Mock<IExecutionContext> CreateEvaluatableContext(
            TestHostContext hc,
            DictionaryContextData expressionValues)
        {
            var mock = new Moq.Mock<IExecutionContext>();
            mock.Setup(x => x.ExpressionValues).Returns(expressionValues);
            mock.Setup(x => x.ExpressionFunctions)
                .Returns(new List<GitHub.DistributedTask.Expressions2.IFunctionInfo>());
            mock.Setup(x => x.Global).Returns(new GlobalContext
            {
                FileTable = new List<string>(),
                Variables = new Variables(hc, new Dictionary<string, VariableValue>()),
            });
            // ToPipelineTemplateEvaluator uses ToTemplateTraceWriter which calls
            // context.Write — provide a no-op so it doesn't NRE.
            mock.Setup(x => x.Write(Moq.It.IsAny<string>(), Moq.It.IsAny<string>()));
            return mock;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateExpression_ReturnsValueForSimpleExpression()
        {
            using (var hc = CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "repository", new StringContextData("owner/repo") }
                };

                var ctx = CreateEvaluatableContext(hc, exprValues);
                var result = _provider.EvaluateExpression("github.repository", ctx.Object);

                Assert.Equal("owner/repo", result.Result);
                Assert.Equal("string", result.Type);
                Assert.Equal(0, result.VariablesReference);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateExpression_StripsWrapperSyntax()
        {
            using (var hc = CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "event_name", new StringContextData("push") }
                };

                var ctx = CreateEvaluatableContext(hc, exprValues);
                var result = _provider.EvaluateExpression("${{ github.event_name }}", ctx.Object);

                Assert.Equal("push", result.Result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateExpression_MasksSecretInResult()
        {
            using (var hc = CreateTestContext())
            {
                hc.SecretMasker.AddValue("super-secret");

                var exprValues = new DictionaryContextData();
                exprValues["env"] = new DictionaryContextData
                {
                    { "TOKEN", new StringContextData("super-secret") }
                };

                var ctx = CreateEvaluatableContext(hc, exprValues);
                var result = _provider.EvaluateExpression("env.TOKEN", ctx.Object);

                Assert.DoesNotContain("super-secret", result.Result);
                Assert.Contains("***", result.Result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateExpression_ReturnsErrorForInvalidExpression()
        {
            using (var hc = CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData();

                var ctx = CreateEvaluatableContext(hc, exprValues);
                // An invalid expression syntax should not throw — it should
                // return an error result.
                var result = _provider.EvaluateExpression("!!!invalid[[", ctx.Object);

                Assert.Contains("error", result.Result, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateExpression_ReturnsMessageWhenNoContext()
        {
            using (CreateTestContext())
            {
                var result = _provider.EvaluateExpression("github.repository", null);

                Assert.Contains("no execution context", result.Result, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateExpression_ReturnsEmptyForEmptyExpression()
        {
            using (var hc = CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                var ctx = CreateEvaluatableContext(hc, exprValues);
                var result = _provider.EvaluateExpression("", ctx.Object);

                Assert.Equal(string.Empty, result.Result);
            }
        }

        #endregion

        #region InferResultType

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InferResultType_ClassifiesCorrectly()
        {
            using (CreateTestContext())
            {
                Assert.Equal("null", DapVariableProvider.InferResultType(null));
                Assert.Equal("null", DapVariableProvider.InferResultType("null"));
                Assert.Equal("boolean", DapVariableProvider.InferResultType("true"));
                Assert.Equal("boolean", DapVariableProvider.InferResultType("false"));
                Assert.Equal("number", DapVariableProvider.InferResultType("42"));
                Assert.Equal("number", DapVariableProvider.InferResultType("3.14"));
                Assert.Equal("object", DapVariableProvider.InferResultType("{\"key\":\"val\"}"));
                Assert.Equal("object", DapVariableProvider.InferResultType("[1,2,3]"));
                Assert.Equal("string", DapVariableProvider.InferResultType("hello world"));
                Assert.Equal("string", DapVariableProvider.InferResultType("owner/repo"));
            }
        }

        #endregion

        #region Non-string secret type redaction

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_SecretsScopeRedactsNumberContextData()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["secrets"] = new DictionaryContextData
                {
                    { "NUMERIC_SECRET", new NumberContextData(12345) }
                };

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 6);

                Assert.Single(variables);
                Assert.Equal("NUMERIC_SECRET", variables[0].Name);
                Assert.Equal("***", variables[0].Value);
                Assert.Equal("string", variables[0].Type);
                Assert.Equal(0, variables[0].VariablesReference);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_SecretsScopeRedactsBooleanContextData()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["secrets"] = new DictionaryContextData
                {
                    { "BOOL_SECRET", new BooleanContextData(true) }
                };

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 6);

                Assert.Single(variables);
                Assert.Equal("BOOL_SECRET", variables[0].Name);
                Assert.Equal("***", variables[0].Value);
                Assert.Equal("string", variables[0].Type);
                Assert.Equal(0, variables[0].VariablesReference);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_SecretsScopeRedactsNestedDictionary()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                exprValues["secrets"] = new DictionaryContextData
                {
                    { "NESTED_SECRET", new DictionaryContextData
                        {
                            { "inner_key", new StringContextData("inner_value") }
                        }
                    }
                };

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 6);

                Assert.Single(variables);
                Assert.Equal("NESTED_SECRET", variables[0].Name);
                Assert.Equal("***", variables[0].Value);
                Assert.Equal("string", variables[0].Type);
                // Nested container should NOT be drillable under secrets
                Assert.Equal(0, variables[0].VariablesReference);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void GetVariables_SecretsScopeRedactsNullValue()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                var secrets = new DictionaryContextData();
                secrets["NULL_SECRET"] = null;
                exprValues["secrets"] = secrets;

                var ctx = CreateMockContext(exprValues);
                var variables = _provider.GetVariables(ctx.Object, 6);

                Assert.Single(variables);
                Assert.Equal("NULL_SECRET", variables[0].Name);
                Assert.Equal("***", variables[0].Value);
                Assert.Equal(0, variables[0].VariablesReference);
            }
        }

        #endregion
    }
}

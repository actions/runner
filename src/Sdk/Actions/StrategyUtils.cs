using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;

namespace Sdk.Actions {
    public static class StrategyUtils {
        public static string GetDefaultDisplaySuffix(IEnumerable<string> item) {
            var displaySuffix = new StringBuilder();
            int z = 0;
            foreach (var mk in item) {
                if(!string.IsNullOrEmpty(mk)) {
                    displaySuffix.Append(z++ == 0 ? "(" : ", ");
                    displaySuffix.Append(mk);
                }
            }
            if(z > 0) {
                displaySuffix.Append( ")");
            }
            return displaySuffix.ToString();
        }

        public static StrategyResult ExpandStrategy(MappingToken strategy, bool matrixexcludeincludelists, ITraceWriter jobTraceWriter, string jobname) {
            var flatmatrix = new List<Dictionary<string, TemplateToken>> { new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase) };
            var includematrix = new List<Dictionary<string, TemplateToken>> { };
            bool failFast = true;
            double? max_parallel = null;
            SequenceToken include = null;
            SequenceToken exclude = null;
            if (strategy != null) {
                failFast = (from r in strategy where r.Key.AssertString($"jobs.{jobname}.strategy mapping key").Value == "fail-fast" select r).FirstOrDefault().Value?.AssertBoolean($"jobs.{jobname}.strategy.fail-fast")?.Value ?? failFast;
                max_parallel = (from r in strategy where r.Key.AssertString($"jobs.{jobname}.strategy mapping key").Value == "max-parallel" select r).FirstOrDefault().Value?.AssertNumber($"jobs.{jobname}.strategy.max-parallel")?.Value;
                var matrix = (from r in strategy where r.Key.AssertString($"jobs.{jobname}.strategy mapping key").Value == "matrix" select r).FirstOrDefault().Value?.AssertMapping($"jobs.{jobname}.strategy.matrix");
                if(matrix != null) {
                    foreach (var item in matrix)
                    {
                        var key = item.Key.AssertString($"jobs.{jobname}.strategy.matrix mapping key").Value;
                        switch (key)
                        {
                            case "include":
                                include = item.Value?.AssertSequence($"jobs.{jobname}.strategy.matrix.include");
                                break;
                            case "exclude":
                                exclude = item.Value?.AssertSequence($"jobs.{jobname}.strategy.matrix.exclude");
                                break;
                            default:
                                var val = item.Value.AssertSequence($"jobs.{jobname}.strategy.matrix.{key}");
                                var next = new List<Dictionary<string, TemplateToken>>();
                                foreach (var mel in flatmatrix)
                                {
                                    foreach (var n in val)
                                    {
                                        var ndict = new Dictionary<string, TemplateToken>(mel, StringComparer.OrdinalIgnoreCase);
                                        ndict.Add(key, n);
                                        next.Add(ndict);
                                    }
                                }
                                flatmatrix = next;
                                break;
                        }
                    }
                    if (exclude != null)
                    {
                        foreach (var item in exclude)
                        {
                            var map = item.AssertMapping($"jobs.{jobname}.strategy.matrix.exclude.*").ToDictionary(k => k.Key.AssertString($"jobs.{jobname}.strategy.matrix.exclude.* mapping key").Value, k => k.Value, StringComparer.OrdinalIgnoreCase);
                            flatmatrix.RemoveAll(dict =>
                            {
                                foreach (var item in map)
                                {
                                    TemplateToken val;
                                    if (!dict.TryGetValue(item.Key, out val))
                                    {
                                        // The official github actions service reject this matrix, return false would just ignore it
                                        throw new Exception($"Tried to exclude a matrix key {item.Key} which isn't defined by the matrix");
                                    }
                                    if (!(matrixexcludeincludelists && val is SequenceToken seq ? seq.Any(t => t.DeepEquals(item.Value, true)) : val.DeepEquals(item.Value, true))) {
                                        return false;
                                    }
                                }
                                jobTraceWriter?.Info("{0}", $"Removing {string.Join(',', from m in dict select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))} from matrix, due exclude entry {string.Join(',', from m in map select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))}");
                                return true;
                            });
                        }
                    }
                }
                if(flatmatrix.Count == 0) {
                    jobTraceWriter?.Info("{0}", $"Matrix is empty, adding an empty entry");
                    // Fix empty matrix after exclude
                    flatmatrix.Add(new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase));
                }
            }
            // Enforce job matrix limit of github
            if(flatmatrix.Count > 256) {
                jobTraceWriter?.Info("{0}", $"Failure: Matrix contains more than 256 entries after exclude");
                return new StrategyResult {
                    Result = TaskResult.Failed
                };
            }
            var keys = flatmatrix.First().Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (include != null) {
                foreach(var map in include.SelectMany(item => {
                    var map = item.AssertMapping($"jobs.{jobname}.strategy.matrix.include.*").ToDictionary(k => k.Key.AssertString($"jobs.{jobname}.strategy.matrix.include.* mapping key").Value, k => k.Value, StringComparer.OrdinalIgnoreCase);
                    if(matrixexcludeincludelists) {
                        var ret = new List<Dictionary<string, TemplateToken>>{ new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase) };
                        foreach(var m in map) {
                            var next = new List<Dictionary<string, TemplateToken>>();
                            var cur = next.ToArray();
                            foreach(var n in ret) {
                                var d = new Dictionary<string, TemplateToken>(n, StringComparer.OrdinalIgnoreCase);
                                if(m.Value is SequenceToken seq) {
                                    foreach(var v in seq) {
                                        d[m.Key] = v;
                                    }
                                } else {
                                    d[m.Key] = m.Value;
                                }
                                next.Add(d);
                            }
                            ret = next;
                        }
                        return ret.AsEnumerable();
                    } else {
                        return new[] { map }.AsEnumerable();
                    }
                })) {
                    bool matched = false;
                    if(keys.Count > 0) {
                        flatmatrix.ForEach(dict => {
                            foreach (var item in keys) {
                                TemplateToken val;
                                if (map.TryGetValue(item, out val) && !dict[item].DeepEquals(val, true)) {
                                    return;
                                }
                            }
                            matched = true;
                            // Add missing keys
                            jobTraceWriter?.Info("{0}", $"Add missing keys to {string.Join(',', from m in dict select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))}, due to include entry {string.Join(',', from m in map select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))}");
                            foreach (var item in map) {
                                if(!keys.Contains(item.Key)) {
                                    dict[item.Key] = item.Value;
                                }
                            }
                        });
                    }
                    if (!matched) {
                        jobTraceWriter?.Info("{0}", $"Append include entry {string.Join(',', from m in map select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))}, due to match miss");
                        includematrix.Add(map);
                    }
                }
            }
            return new StrategyResult {
                FlatMatrix = flatmatrix,
                IncludeMatrix = includematrix,
                FailFast = failFast,
                MaxParallel = max_parallel,
                MatrixKeys = keys
            };
        }
    }
}
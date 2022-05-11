using System;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace Runner.Server {
    public class Concurrency {
        public string Group { get; set; }
        public bool CancelInProgress { get; set; }
    }

    public static class ConcurrencyExtensions {
        public static Concurrency ToConcurrency(this TemplateToken concurrency, string tag = null, int maxConcurrencyGroupNameLength = -1) {
            var prefix = string.IsNullOrEmpty(tag) ? "" : tag + ": ";
            var ret = new Concurrency();
            if(concurrency is StringToken stkn) {
                ret.Group = stkn.Value;
            } else {
                var cmapping = concurrency.AssertMapping($"{prefix}concurrency must be a string or mapping");
                ret.Group = (from r in cmapping where r.Key.AssertString($"{prefix}concurrency mapping key").Value == "group" select r).FirstOrDefault().Value?.AssertString($"{prefix}concurrency.group")?.Value;
                ret.CancelInProgress = (from r in cmapping where r.Key.AssertString($"{prefix}concurrency mapping key").Value == "cancel-in-progress" select r).FirstOrDefault().Value?.AssertBoolean($"{prefix}concurrency.cancel-in-progress")?.Value == true;
            }
            var concurrencyGroupNameLength = System.Text.Encoding.UTF8.GetByteCount(ret.Group ?? "");
            if(maxConcurrencyGroupNameLength >= 0 && concurrencyGroupNameLength > maxConcurrencyGroupNameLength) {
                throw new Exception($"{prefix}The specified concurrency group name with length {concurrencyGroupNameLength} exceeds the maximum allowed length of {maxConcurrencyGroupNameLength}");
            }
            return ret;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GitHub.DistributedTask.Pipelines;
using GitHub.Services.WebApi;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.WebApi
{
    public static class VariableUtility
    {
        public static VariableValue Clone(this VariableValue value)
        {
            return new VariableValue(value);
        }
    }
}

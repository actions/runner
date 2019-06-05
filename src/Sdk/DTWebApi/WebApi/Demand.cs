using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using GitHub.Services.Common;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    [JsonConverter(typeof(DemandJsonConverter))]
    public abstract class Demand
    {
        protected Demand(
            String name,
            String value)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(name, "name");
            this.Name = name;
            this.Value = value;
        }

        [DataMember]
        public String Name
        {
            get;
            private set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Value
        {
            get;
            private set;
        }

        public override sealed Boolean Equals(Object obj)
        {
            Demand demand = obj as Demand;
            return demand != null && demand.ToString().Equals(this.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override sealed Int32 GetHashCode()
        {
            return this.ToString().ToUpperInvariant().GetHashCode();
        }

        public override sealed String ToString()
        {
            return GetExpression();
        }

        public abstract Demand Clone();

        protected abstract String GetExpression();

        public abstract Boolean IsSatisfied(IDictionary<String, String> capabilities);

        public static Boolean TryParse(
            String input,
            out Demand demand)
        {
            demand = null;

            Match match = s_demandRegex.Match(input);
            if (!match.Success)
            {
                return false;
            }

            String name = match.Groups["name"].Value;
            String opcode = match.Groups["opcode"].Value;
            String value = match.Groups["value"].Value;

            if (String.IsNullOrEmpty(opcode))
            {
                demand = new DemandExists(name);
            }
            else
            {
                switch (opcode)
                {
                    case "equals":
                        demand = new DemandEquals(name, value);
                        break;
                    case "gtVersion":
                        demand = new DemandMinimumVersion(name, value);
                        break;
                }
            }

            return demand != null;
        }

        public void Update(String value)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(value, "value");
            this.Value = value;
        }

        private static readonly Regex s_demandRegex = new Regex(@"^(?<name>\S+)(\s+\-(?<opcode>\S+)\s+(?<value>.*))?$", RegexOptions.Compiled);
    }
}

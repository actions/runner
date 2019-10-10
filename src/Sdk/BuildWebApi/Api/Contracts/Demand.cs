using System;
using System.Text.RegularExpressions;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a demand used by a definition or build.
    /// </summary>
    [JsonConverter(typeof(DemandJsonConverter))]
    public abstract class Demand : BaseSecuredObject
    {
        protected Demand(
            String name,
            String value)
            : this(name, value, null)
        {
        }

        protected Demand(
            String name,
            String value,
            ISecuredObject securedObject)
            : base(securedObject)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(name, "name");
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// The name of the capability referenced by the demand.
        /// </summary>
        public String Name
        {
            get;
            private set;
        }

        /// <summary>
        /// The demanded value.
        /// </summary>
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

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns></returns>
        public abstract Demand Clone();

        protected abstract String GetExpression();

        /// <summary>
        /// Parses a string into a Demand instance.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="demand"></param>
        /// <returns></returns>
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
                }
            }

            return demand != null;
        }

        private static readonly Regex s_demandRegex = new Regex(@"^(?<name>[^ ]+)([ ]+\-(?<opcode>[^ ]+)[ ]+(?<value>.*))?$",
            RegexOptions.Compiled);
    }
}

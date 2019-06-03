using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.Common
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RSAParametersExtensions
    {
        public static RSAParameters FromJsonString(string parameterString)
        {
            ArgumentUtility.CheckForNull(parameterString, nameof(parameterString));
            JObject rsaJson = JObject.Parse(parameterString);
            RSAParameters rsaParameters = new RSAParameters();
            rsaParameters.D = rsaJson["D"].ToObject<byte[]>();
            rsaParameters.DP = rsaJson["DP"].ToObject<byte[]>();
            rsaParameters.DQ = rsaJson["DQ"].ToObject<byte[]>();
            rsaParameters.Exponent = rsaJson["Exponent"].ToObject<byte[]>();
            rsaParameters.InverseQ = rsaJson["InverseQ"].ToObject<byte[]>();
            rsaParameters.Modulus = rsaJson["Modulus"].ToObject<byte[]>();
            rsaParameters.P = rsaJson["P"].ToObject<byte[]>();
            rsaParameters.Q = rsaJson["Q"].ToObject<byte[]>();
            return rsaParameters;
        }

        public static string ToJsonString(this RSAParameters rsaParameters)
        {
            JObject rsaJson = new JObject();
            rsaJson["D"] = rsaParameters.D;
            rsaJson["DP"] = rsaParameters.DP;
            rsaJson["DQ"] = rsaParameters.DQ;
            rsaJson["Exponent"] = rsaParameters.Exponent;
            rsaJson["InverseQ"] = rsaParameters.InverseQ;
            rsaJson["Modulus"] = rsaParameters.Modulus;
            rsaJson["P"] = rsaParameters.P;
            rsaJson["Q"] = rsaParameters.Q;
            return rsaJson.ToString();
        }
    }
}

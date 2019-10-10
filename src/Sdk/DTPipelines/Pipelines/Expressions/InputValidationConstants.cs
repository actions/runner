using System;
using GitHub.DistributedTask.Expressions;

namespace GitHub.DistributedTask.Pipelines.Expressions
{
    internal static class InputValidationConstants
    {
        public static readonly String IsEmail = "isEmail";
        public static readonly String IsInRange = "isInRange";
        public static readonly String IsIPv4Address = "isIPv4Address";
        public static readonly String IsSha1 = "isSha1";
        public static readonly String IsUrl = "isUrl";
        public static readonly String IsMatch = "isMatch";
        public static readonly String Length = "length";

        public static readonly IFunctionInfo[] Functions = new IFunctionInfo[]
        {
            new FunctionInfo<IsEmailNode>(InputValidationConstants.IsEmail, IsEmailNode.minParameters, IsEmailNode.maxParameters),
            new FunctionInfo<IsInRangeNode>(InputValidationConstants.IsInRange, IsInRangeNode.minParameters, IsInRangeNode.maxParameters),
            new FunctionInfo<IsIPv4AddressNode>(InputValidationConstants.IsIPv4Address, IsIPv4AddressNode.minParameters, IsIPv4AddressNode.maxParameters),
            new FunctionInfo<IsMatchNode>(InputValidationConstants.IsMatch, IsMatchNode.minParameters, IsMatchNode.maxParameters),
            new FunctionInfo<IsSHA1Node>(InputValidationConstants.IsSha1, IsSHA1Node.minParameters, IsSHA1Node.maxParameters),
            new FunctionInfo<IsUrlNode>(InputValidationConstants.IsUrl, IsUrlNode.minParameters, IsUrlNode.maxParameters),
            new FunctionInfo<LengthNode>(InputValidationConstants.Length, LengthNode.minParameters, LengthNode.maxParameters),
        };

        public static readonly INamedValueInfo[] NamedValues = new INamedValueInfo[]
        {
            new NamedValueInfo<InputValueNode>("value"),
        };
    }
}

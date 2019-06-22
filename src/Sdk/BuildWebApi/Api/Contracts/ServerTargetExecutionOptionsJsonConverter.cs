using System;

namespace GitHub.Build.WebApi
{
    internal sealed class ServerTargetExecutionOptionsJsonConverter : TypePropertyJsonConverter<ServerTargetExecutionOptions>
    {
        protected override ServerTargetExecutionOptions GetInstance(Type objectType)
        {
            if (objectType == typeof(ServerTargetExecutionType))
            {
                return new ServerTargetExecutionOptions();
            }
            else if (objectType == typeof(VariableMultipliersServerExecutionOptions))
            {
                return new VariableMultipliersServerExecutionOptions();
            }
            else
            {
                return base.GetInstance(objectType);
            }
        }

        protected override ServerTargetExecutionOptions GetInstance(Int32 targetType)
        {
            switch (targetType)
            {
                case ServerTargetExecutionType.Normal:
                    return new ServerTargetExecutionOptions();
                case ServerTargetExecutionType.VariableMultipliers:
                    return new VariableMultipliersServerExecutionOptions();
                default:
                    return null;
            }
        }
    }
}

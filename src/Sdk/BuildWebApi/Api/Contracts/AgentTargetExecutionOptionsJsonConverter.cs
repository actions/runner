using System;

namespace GitHub.Build.WebApi
{
    internal sealed class AgentTargetExecutionOptionsJsonConverter : TypePropertyJsonConverter<AgentTargetExecutionOptions>
    {
        protected override AgentTargetExecutionOptions GetInstance(Type objectType)
        {
            if (objectType == typeof(AgentTargetExecutionType))
            {
                return new AgentTargetExecutionOptions();
            }
            else if (objectType == typeof(VariableMultipliersAgentExecutionOptions))
            {
                return new VariableMultipliersAgentExecutionOptions();
            }
            else if (objectType == typeof(MultipleAgentExecutionOptions))
            {
                return new MultipleAgentExecutionOptions();
            }
            else
            {
                return base.GetInstance(objectType);
            }
        }

        protected override AgentTargetExecutionOptions GetInstance(Int32 targetType)
        {
            switch (targetType)
            {
                case AgentTargetExecutionType.Normal:
                    return new AgentTargetExecutionOptions();
                case AgentTargetExecutionType.VariableMultipliers:
                    return new VariableMultipliersAgentExecutionOptions();
                case AgentTargetExecutionType.MultipleAgents:
                    return new MultipleAgentExecutionOptions();
                default:
                    return null;
            }
        }
    }
}

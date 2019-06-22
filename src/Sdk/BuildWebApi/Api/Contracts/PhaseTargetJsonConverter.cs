using System;

namespace GitHub.Build.WebApi
{
    internal sealed class PhaseTargetJsonConverter : TypePropertyJsonConverter<PhaseTarget>
    {
        protected override PhaseTarget GetInstance(Type objectType)
        {
            if (objectType == typeof(AgentPoolQueueTarget))
            {
                return new AgentPoolQueueTarget();
            }
            else if (objectType == typeof(ServerTarget))
            {
                return new ServerTarget();
            }
            else
            {
                return base.GetInstance(objectType);
            }
        }

        protected override PhaseTarget GetInstance(Int32 targetType)
        {
            switch (targetType)
            {
                case PhaseTargetType.Agent:
                    return new AgentPoolQueueTarget();
                case PhaseTargetType.Server:
                    return new ServerTarget();
                default:
                    return null;
            }
        }
    }
}

using System;

namespace GitHub.Actions.WorkflowParser
{
    public class ReferencedWorkflowNotFoundException : Exception
    {
        public ReferencedWorkflowNotFoundException(String message)
            : base(message)
        {
        }
    }
}

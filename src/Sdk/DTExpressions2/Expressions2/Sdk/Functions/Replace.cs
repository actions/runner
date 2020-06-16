using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class Replace : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var value = Parameters[0].Evaluate(context);
            var characterToReplace = Parameters[1].Evaluate(context);
            var characterToReplaceWith = Parameters[2].Evaluate(context);

            if (value.IsPrimitive)
            {
                value = value.ConvertToString();
            }
            if (characterToReplace.IsPrimitive)
            {
                characterToReplace = characterToReplace.ConvertToString();
            }
            if (characterToReplaceWith.IsPrimitive)
            {
                characterToReplaceWith = characterToReplaceWith.ConvertToString();
            }

            return String.IsNullOrEmpty(value) ? String.Empty : value.Replace(characterToReplace, characterToReplaceWith);
        }
    }
}

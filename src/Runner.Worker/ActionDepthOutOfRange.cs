using System;

namespace GitHub.Runner.Worker
{
    public class ActionDepthOutOfRange : Exception
    {
        public ActionDepthOutOfRange(int depth)
            : base(FormatMessage(depth))
        {
        }

        public ActionDepthOutOfRange(string message)
            : base(message)
        {
        }

        private static string FormatMessage(int depth)
        {
            return $"Composite action depth exceeded max depth {depth-1}, please simplify your actions.'";
        }
    }
}
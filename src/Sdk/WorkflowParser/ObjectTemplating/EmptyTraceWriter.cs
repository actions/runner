using System;

﻿namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    internal class EmptyTraceWriter : ITraceWriter
    {
        public void Error(
            String format,
            params Object[] args)
        {
        }

        public void Info(
            String format,
            params Object[] args)
        {
        }

        public void Verbose(
            String format,
            params Object[] args)
        {
        }
    }
}
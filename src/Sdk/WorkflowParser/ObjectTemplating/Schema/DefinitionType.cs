using System;

﻿namespace GitHub.Actions.WorkflowParser.ObjectTemplating.Schema
{
    internal enum DefinitionType
    {
        Null,
        Boolean,
        Number,
        String,
        Sequence,
        Mapping,
        OneOf,
        AllowedValues,
    }
}
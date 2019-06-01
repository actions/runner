﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Azure.DevOps.Licensing.WebApi
{
    [DataContract]
    [Flags]
    public enum LicensingSettingsSelectProperty
    {
        DefaultAccessLevel = 1,
        AccessLevelOptions = 2,

        All = DefaultAccessLevel | AccessLevelOptions
    }
}

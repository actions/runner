using System;
using System.Collections.Generic;

public interface IEnvironmentContextData
{
    IEnumerable<KeyValuePair<string, string>> GetRuntimeEnvironmentVariables();
}

using System;
using System.Collections.Generic;

namespace Clc.Rest
{
    public interface ISerializer
    {
        string MediaType { get; }
        string Serialize(object input, bool ignoreNullValues = true);
    }
}

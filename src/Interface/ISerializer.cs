using System;
using System.Collections.Generic;
using System.Text;

namespace Clc.Rest
{
    public interface ISerializer
    {
        string MediaType { get; }
        string Serialize(object input, bool ignoreNullValues = true);
    }
}

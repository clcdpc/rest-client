using System;
using System.Collections.Generic;
using System.Text;

namespace Clc.Rest
{
    public interface IDeserializer
    {
        T Deserialize<T>(string input);
    }
}
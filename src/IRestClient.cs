using Clc.Rest.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Clc.Rest
{
    public interface IRestClient
    {
        IRestResponse<T> Execute<T>(RestRequest request);
    }
}

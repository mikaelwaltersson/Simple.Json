using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    public interface IObjectBuilder
    {
        void Add(string name, object value);
        object End();
    }
}
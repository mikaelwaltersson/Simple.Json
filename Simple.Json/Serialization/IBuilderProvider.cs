using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    public interface IBuilderProvider
    {
        IObjectBuilder GetObjectBuilder();
        IBuilderProvider GetObjectValueBuilderProvider(string key);

        IArrayBuilder GetArrayBuilder();
        IBuilderProvider GetArrayElementBuilderProvider();
    }
}
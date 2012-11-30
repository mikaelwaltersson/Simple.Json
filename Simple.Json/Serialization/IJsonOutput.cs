using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    public interface IJsonOutput
    {
        void Null();
        void Boolean(bool value);
        void Number(double value);
        void String(string value);

        void BeginObject();
        void NamedProperty(string name);        
        void EndObject();

        void BeginArray();
        void EndArray();
    }
}

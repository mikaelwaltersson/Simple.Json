using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Simple.Json.Serialization;

namespace Simple.Json
{
    public class JsonArray : List<object>, IArrayBuilder
    {
        object IArrayBuilder.End()
        {
            return this;
        }

        public override string ToString()
        {
            return JsonSerializer.Default.ToJson(this);
        }
    }
}
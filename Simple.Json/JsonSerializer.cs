using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Simple.Json.Formatters;
using Simple.Json.Parsers;
using Simple.Json.Serialization;

namespace Simple.Json
{
    public class JsonSerializer : IJsonSerializer
    {
        public const int DefaultMaxSerializeGraphDepth = 64;

        ITypeSerializer typeSerializer;
        int maxSerializeGraphDepth = DefaultMaxSerializeGraphDepth;        

        public static readonly JsonSerializer Default = new JsonSerializer(TypeSerializer.Default);

        
        public JsonSerializer(ITypeSerializer typeSerializer)
        {
            this.typeSerializer = Argument.NotNull(typeSerializer, "typeSerializer");            
        }

        public int MaxSerializeGraphDepth
        {
            get { return maxSerializeGraphDepth; }
            set { maxSerializeGraphDepth = value; }
        }


        public object ParseJson(string input, Type type)
        {           
            var builderProvider = typeSerializer.GetBuilderProvider(type);

            return new JsonParser(input).Parse(() => builderProvider);
        }

        public string ToJson(object value, Type type, bool formatted)
        {
            var deconstructor = typeSerializer.GetDeconstructor(type);
            var output = new StringBuilder();

            deconstructor.Deconstruct(value, new JsonOutput(output, formatted, maxSerializeGraphDepth));

            return output.ToString();
        }





    }
}

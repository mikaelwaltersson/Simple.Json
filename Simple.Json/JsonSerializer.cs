using System;
using System.Collections.Generic;
using System.IO;
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


        public object ParseJson(string s, Type type)
        {
            return ParseJson(new JsonParser(s), type);
        }
        
        public object ParseJson(TextReader reader, Type type)
        {
            return ParseJson(new JsonParser(reader), type);
        }

        object ParseJson(JsonParser parser, Type type)
        {
            var builderProvider = typeSerializer.GetBuilderProvider(type);

            return parser.Parse(() => builderProvider);
        }

        public string ToJson(object value, Type type, bool formatted)
        {
            using (var writer = new StringWriter())
            {
                ToJson(writer, value, type, formatted);
                return writer.ToString();
           }            
        }

        public void ToJson(TextWriter writer, object value, Type type, bool formatted)
        {
            var deconstructor = typeSerializer.GetDeconstructor(type);

            deconstructor.Deconstruct(value, new JsonOutput(writer, formatted, maxSerializeGraphDepth));
        }





    }
}

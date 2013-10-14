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



        public object ParseJson(string s)
        {
            return ParseJson(s, typeof(object));
        }

        public object ParseJson(string s, Type type)
        {
            return ParseJson(new JsonParser(s), type);
        }

        public T ParseJson<T>(string s)
        {
            return (T)ParseJson(s, typeof(T));
        }

        public object ParseJson(TextReader reader)
        {
            return ParseJson(new JsonParser(reader), typeof(object));
        }

        public object ParseJson(TextReader reader, Type type)
        {
            return ParseJson(new JsonParser(reader), type);
        }

        public T ParseJson<T>(TextReader reader)
        {
            return (T)ParseJson(reader, typeof(T));
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

        public string ToJson<T>(T value, bool formatted = false)
        {
            return ToJson(value, typeof(T), formatted);
        }

        public void ToJson(TextWriter writer, object value, Type type, bool formatted)
        {
            var deconstructor = typeSerializer.GetDeconstructor(type);

            deconstructor.Deconstruct(value, new JsonOutput(writer, formatted, maxSerializeGraphDepth));
        }

        public void ToJson<T>(TextWriter writer, T value, bool formatted = false)
        {
            ToJson(writer, value, typeof(T), formatted);
        }
    }
}

using System;
using System.IO;

namespace Simple.Json
{
    public interface IJsonSerializer
    {
        object ParseJson(string s, Type type);
        object ParseJson(TextReader reader, Type type);

        string ToJson(object value, Type type, bool formatted);
        void ToJson(TextWriter writer, object value, Type type, bool formatted);
    }
}
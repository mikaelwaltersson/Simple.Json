using System;
using System.IO;

namespace Simple.Json
{
    public interface IJsonSerializer
    {
        object ParseJson(string s);
        object ParseJson(string s, Type type);
        T ParseJson<T>(string s);

        object ParseJson(TextReader reader);
        object ParseJson(TextReader reader, Type type);
        T ParseJson<T>(TextReader reader);
        
        string ToJson(object value, Type type, bool formatted = false);                
        string ToJson<T>(T value, bool formatted = false);

        void ToJson(TextWriter writer, object value, Type type, bool formatted = false);
        void ToJson<T>(TextWriter writer, T value, bool formatted = false);        
    }
}
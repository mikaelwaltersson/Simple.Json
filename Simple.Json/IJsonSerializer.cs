using System;

namespace Simple.Json
{
    public interface IJsonSerializer
    {
        object ParseJson(string s, Type type);
        string ToJson(object value, Type type, bool formatted);
    }
}
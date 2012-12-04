using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json
{
    public static class JsonSerializerOverloads
    {
        public static T ParseJson<T>(this IJsonSerializer serializer, string s)
        {
            return (T)serializer.ParseJson(s, typeof(T));
        }

        public static object ParseJson(this IJsonSerializer serializer, string s)
        {
            return serializer.ParseJson(s, typeof(object));
        }

        public static string ToJson<T>(this IJsonSerializer serializer, T value)
        {
            return serializer.ToJson(value, false);
        }

        public static string ToJson<T>(this IJsonSerializer serializer, T value, bool formatted)
        {
            return serializer.ToJson(value, typeof(T), formatted);
        }
    }


}
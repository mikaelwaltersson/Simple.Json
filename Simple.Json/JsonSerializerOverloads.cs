using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json
{
    public static class JsonSerializerOverloads
    {
        public static T ParseJson<T>(this IJsonSerializer serializer, string input)
        {
            return (T)serializer.ParseJson(input, typeof(T));
        }

        public static object ParseJson(this IJsonSerializer serializer, string input)
        {
            return serializer.ParseJson(input, typeof(object));
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
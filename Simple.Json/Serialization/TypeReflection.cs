using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    static class TypeReflection
    {
        public static bool CanStoreNullValue(this Type type)
        {
            return type.IsClass || type.IsGenericType(typeof(Nullable<>));
        }

        public static bool IsGenericType(this Type type, params Type[] genericTypeDefinitions)
        {
            return type.IsGenericType && genericTypeDefinitions.Contains(type.GetGenericTypeDefinition());
        }

        public static Type GetArrayOrGenericCollectionElementType(this Type type)
        {
            return type.IsArray ? type.GetElementType() : type.GetGenericArguments().FirstOrDefault();
        }

        public static ConstructorInfo GetPublicDefaultConstructor(this Type type)
        {
            return type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        }

        public static ConstructorInfo GetPublicConstructorWithMostNonByRefParameters(this Type type)
        {
            return 
                type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).
                    Where(constructor => !constructor.GetParameters().Any(parameter => parameter.ParameterType.IsByRef)).
                    OrderByDescending(constructor => constructor.GetParameters().Length).
                    FirstOrDefault();
        }

        public static FieldInfo GetPublicStaticField(this Type type, string name)
        {
            return type.GetField(name, BindingFlags.Static | BindingFlags.Public);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    public class TypeSerializer : InstanceCountConstrained<TypeSerializer>, ITypeSerializer
    {
        public const string DynamicAssemblyName = "Simple.Json.Serialization.TypeSerializer.GeneratedTypes";

        ClassFactory classFactory;
        ConcurrentDictionary<Type, IBuilderProvider> builderProviders;
        ConcurrentDictionary<Type, IDeconstructor> deconstructors;
        

        public static readonly TypeSerializer Default = new TypeSerializer();

        public TypeSerializer()
        {                        
            classFactory = new ClassFactory(this);
            builderProviders = new ConcurrentDictionary<Type, IBuilderProvider>();
            deconstructors = new ConcurrentDictionary<Type, IDeconstructor>();
        }


        public IBuilderProvider GetBuilderProvider(Type type)
        {
            return GetSingletonInstance(type, builderProviders, classFactory.GetOrCreateBuilderProviderInstanceField);        
        }

        public IDeconstructor GetDeconstructor(Type type)
        {
            return GetSingletonInstance(type, deconstructors, classFactory.GetOrCreateDesctructorInstanceField);         
        }

        public ITypeSerializerConfiguration Configuration
        {
            get { return classFactory; }
        }



        static T GetSingletonInstance<T>(Type type, ConcurrentDictionary<Type, T> cache, Func<Type, FieldInfo> lookupMethod)
        {
            Argument.NotNull(type, "type");

            return
                cache.GetOrAdd(
                    type,
                    key =>
                    {
                        var instanceField = lookupMethod(key);

                        if (instanceField == null)
                            throw new InvalidOperationException("Type is neither a class, a generic collection type or an array type. (" + key + ")");                        

                        return (T)instanceField.GetValue(null);
                    });
        }


        internal interface IMakeImmutableInstance
        {
            object MakeImmutableInstance();
        }

        

        class ClassFactory : ITypeSerializerConfiguration
        {
            const string SingletonInstanceFieldName = "Default";

            static long uniqueNameCounter;


            Type listGenericType = typeof(List<>);
            MethodInfo toArrayGenericMethod = typeof(Enumerable).GetMethod("ToArray");
            MethodInfo stringCompareMethod = typeof(string).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string) }, null);
            Type fsharpListType = TryLoadFsharpListType();

            Dictionary<Type, MethodInfo> convertFromJsonValueMethods = new Dictionary<Type, MethodInfo>();
            Dictionary<Type, MethodInfo> convertFromJsonValueUsingStaticParseMethods = new Dictionary<Type, MethodInfo>();
            Dictionary<Type, MethodInfo> convertToJsonValueMethods = new Dictionary<Type, MethodInfo>();            
            Func<string, string> getObjectPropertyName = TypeSerializerConfigurationDefaults.NameToCamelCase;
            Func<string, string> getIsSpecifiedMember = TypeSerializerConfigurationDefaults.GetIsSpecifiedMember;
            bool skipOutputOfDefaultValues;
            bool enableMandatoryFieldsValidation;

            Dictionary<Type, FieldInfo> builderProviderInstanceFields = new Dictionary<Type, FieldInfo>();
            Dictionary<Type, FieldInfo> deconstructorInstanceFields = new Dictionary<Type, FieldInfo>();
            Dictionary<Type, Type> objectBuilderTypes = new Dictionary<Type, Type>();
            Dictionary<Type, Type> arrayBuilderTypes = new Dictionary<Type, Type>();

            TypeSerializer typeSerializer;
            ModuleBuilder moduleBuilder;            
            object syncRoot = new object();



            public ClassFactory(TypeSerializer typeSerializer)
            {
                this.typeSerializer = typeSerializer;

                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToSByte);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToByte);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToInt16);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToUInt16);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToInt32);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToUInt32);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToInt64);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToUInt64);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToSingle);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.CastNumberToDecimal);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.ConvertStringToDateTime);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.ConvertStringToTimeSpan);
                RegisterConvertFromJsonValueMethod(TypeSerializerConfigurationDefaults.ConvertStringToByteArray);

                RegisterConvertToJsonValueMethod<sbyte, double>(TypeSerializerConfigurationDefaults.CastSByteToNumber);
                RegisterConvertToJsonValueMethod<byte, double>(TypeSerializerConfigurationDefaults.CastByteToNumber);
                RegisterConvertToJsonValueMethod<short, double>(TypeSerializerConfigurationDefaults.CastInt16ToNumber);
                RegisterConvertToJsonValueMethod<ushort, double>(TypeSerializerConfigurationDefaults.CastUInt16ToNumber);
                RegisterConvertToJsonValueMethod<int, double>(TypeSerializerConfigurationDefaults.CastInt32ToNumber);
                RegisterConvertToJsonValueMethod<uint, double>(TypeSerializerConfigurationDefaults.CastUInt32ToNumber);
                RegisterConvertToJsonValueMethod<long, double>(TypeSerializerConfigurationDefaults.CastInt64ToNumber);
                RegisterConvertToJsonValueMethod<ulong, double>(TypeSerializerConfigurationDefaults.CastUInt64ToNumber);
                RegisterConvertToJsonValueMethod<float, double>(TypeSerializerConfigurationDefaults.CastSingleToNumber);
                RegisterConvertToJsonValueMethod<decimal, double>(TypeSerializerConfigurationDefaults.CastDecimalToNumber);
                RegisterConvertToJsonValueMethod<DateTime, string>(TypeSerializerConfigurationDefaults.ConvertDateTimeToString);
                RegisterConvertToJsonValueMethod<TimeSpan, string>(TypeSerializerConfigurationDefaults.ConvertTimeSpanToString);
                RegisterConvertToJsonValueMethod<byte[], string>(TypeSerializerConfigurationDefaults.ConvertByteArrayToString);
            }



            static ModuleBuilder CreateModuleBuilder()
            {
                var name = DynamicAssemblyName;

                var appDomain = AppDomain.CurrentDomain;
                var assemblyName = new AssemblyName(name);

                var assemblyBuilder = appDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

                return assemblyBuilder.DefineDynamicModule(name);
            }

            static Type TryLoadFsharpListType()
            {
                return Type.GetType("Microsoft.FSharp.Collections.FSharpList`1, FSharp.Core", false);
            }

            static MethodInfo GetToFSharpListGenericMethod()
            {
                return Type.GetType("Microsoft.FSharp.Collections.SeqModule, FSharp.Core", true).GetMethod("ToList");
            }

            void AssertHasNotBeenUsed()
            {
                lock (syncRoot)
                {
                    if (moduleBuilder != null)
                        throw new InvalidOperationException("The builder provider can not be configured after use");
                }
            }

            MethodInfo GetMethodInfoForPublicStaticDelegate(Delegate method)
            {
                var methodInfo = method.GetMethodInfo();

                if (!(methodInfo.IsStatic && methodInfo.IsPublic && methodInfo.DeclaringType.IsVisible))
                    throw new ArgumentException("Convert value method must be public and static");

                return methodInfo;
            }

            public void RegisterConvertFromJsonValueMethod<TTo>(Func<object, TTo> method)
            {
                Argument.NotNull(method, "method");


                var toType = typeof(TTo);
                
                if (toType.IsValueType)
                {
                    if (!toType.IsGenericType(typeof(Nullable<>)))
                        throw new ArgumentException("Return type must be nullable", "method");

                    toType = toType.GetGenericArguments()[0];
                }


                AssertHasNotBeenUsed();

                convertFromJsonValueMethods[toType] = GetMethodInfoForPublicStaticDelegate(method);
            }

            public void RegisterConvertToJsonValueMethod<TFrom, TTo>(Func<TFrom, TTo> method)
            {
                Argument.NotNull(method, "method");

                var fromType = typeof(TFrom);
                var toType = typeof(TTo);

                if (fromType.IsGenericType(typeof(Nullable<>)))
                    throw new ArgumentException("Argument type can not be Nullable<T>", "method");
                
                if (!new[]{ typeof(bool), typeof(double), typeof(string) }.Contains(toType))
                    throw new ArgumentException("Return type must be bool, double or string", "method");

                AssertHasNotBeenUsed();

                convertToJsonValueMethods[fromType] = GetMethodInfoForPublicStaticDelegate(method);
            }

            public Func<string, string> GetObjectPropertyNameDelegate
            {
                set
                {
                    Argument.NotNull(value, "value");

                    AssertHasNotBeenUsed();

                    getObjectPropertyName = value;
                }
            }

            public Func<string, string> GetIsSpecifiedMemberDelegate
            {
                set
                {
                    Argument.NotNull(value, "value");

                    AssertHasNotBeenUsed();

                    getIsSpecifiedMember = value;
                }
            }     

            public bool SkipOutputOfDefaultValues
            {
                set
                {
                    AssertHasNotBeenUsed();

                    skipOutputOfDefaultValues = value;
                }
            }

            public bool EnableMandatoryFieldsValidation
            {
                set
                {
                    AssertHasNotBeenUsed();

                    enableMandatoryFieldsValidation = value;
                }
            }

            public FieldInfo GetOrCreateBuilderProviderInstanceField(Type type)
            {
                return
                    GetOrCreateInstanceField(
                        type, builderProviderInstanceFields,
                        () => 
                        builderProviderInstanceFields[type] = 
                            typeof(UntypedBuilderProvider).GetPublicStaticField(SingletonInstanceFieldName),
                        () =>
                        CreateBuilderProviderInstanceField(
                            type,
                            ImplementMethodBodyWithThrowException<InvalidCastException>,
                            ImplementMethodBodyWithThrowException<InvalidOperationException>,
                            ImplementGetArrayBuilder,
                            ImplementGetArrayElementBuilderProvider),
                        () =>
                        CreateBuilderProviderInstanceField(
                            type,
                            ImplementGetObjectBuilder,
                            ImplementGetObjectValueBuilderProvider,
                            ImplementMethodBodyWithThrowException<InvalidCastException>,
                            ImplementMethodBodyWithThrowException<InvalidOperationException>));
            }

            public FieldInfo GetOrCreateDesctructorInstanceField(Type type)
            {
                return
                    GetOrCreateInstanceField(
                        type, deconstructorInstanceFields,
                        () => GetOrCreateUntypedDestructorInstanceField(type),
                        () => CreateDeconstructorInstanceField(type, ImplementDeconstructArray),
                        () => CreateDeconstructorInstanceField(type, ImplementDeconstructObject));
            }

            FieldInfo GetOrCreateInstanceField(Type type, IDictionary<Type, FieldInfo> typeCache, Func<FieldInfo> getForUntyped, Func<FieldInfo> getForArray, Func<FieldInfo> getForObject)
            {
                lock (syncRoot)
                {
                    if (moduleBuilder == null)
                        moduleBuilder = CreateModuleBuilder();

                    FieldInfo instanceField;
                    if (typeCache.TryGetValue(type, out instanceField))
                        return instanceField;

                    if (IsUntypedJsonObjectOrArrayCompatible(type))
                        return getForUntyped();

                    if (IsArrayOrCollection(type))
                        return getForArray();

                    if (IsObject(type))
                        return getForObject();
                    
                    return null;
                }
            }

            bool IsUntypedJsonObjectOrArrayCompatible(Type type)
            {
                return
                    type == typeof(object) ||
                    type == typeof(JsonObject) ||
                    type == typeof(IDictionary<string, object>) ||                    
                    type == typeof(ICollection<KeyValuePair<string, object>>) ||
                    type == typeof(IEnumerable<KeyValuePair<string, object>>) ||
                    type == typeof(JsonArray) ||
                    type == typeof(IList<object>) ||
                    type == typeof(ICollection<object>) ||                    
                    type == typeof(IEnumerable<object>) ||                    
                    (type == typeof(IEnumerable) && type != typeof(string) && !IsArrayOrCollection(type));
            }

            bool IsObject(Type type)
            {
                return (type.IsClass || type.IsInterface) && type != typeof(string);
            }

            bool IsArrayOrCollection(Type type)
            {
                return type.IsArray || type.IsGenericType(typeof(IEnumerable<>), typeof(ICollection<>), typeof(IList<>), listGenericType, fsharpListType);
            }



            FieldInfo CreateBuilderProviderInstanceField(
                Type type,
                Action<Type, ILGenerator> implementGetObjectBuilder,
                Action<Type, ILGenerator> implementGetObjectValueBuilderProvider,
                Action<Type, ILGenerator> implementGetArrayBuilder,
                Action<Type, ILGenerator> implementGetArrayElementBuilderProvider)
            {
                return
                    CreateSingletonType(
                        type, 
                        "BuilderProvider", 
                        typeof(IBuilderProvider), 
                        builderProviderInstanceFields,
                        Tuple.Create("GetObjectBuilder", implementGetObjectBuilder),
                        Tuple.Create("GetObjectValueBuilderProvider", implementGetObjectValueBuilderProvider),
                        Tuple.Create("GetArrayBuilder", implementGetArrayBuilder),
                        Tuple.Create("GetArrayElementBuilderProvider", implementGetArrayElementBuilderProvider));
            }

            FieldInfo CreateDeconstructorInstanceField(Type type, Action<Type, ILGenerator> implementDeconstruct)
            {
                return
                    CreateSingletonType(
                        type, 
                        "Deconstructor", 
                        typeof(IDeconstructor), 
                        deconstructorInstanceFields,
                        Tuple.Create("Deconstruct", implementDeconstruct));
            }

            FieldInfo GetOrCreateUntypedDestructorInstanceField(Type type)
            {
                FieldInfo defaultInstanceField;
                if (!deconstructorInstanceFields.TryGetValue(typeof(object), out defaultInstanceField))
                {
                    var typeBuilder = DefineGeneratedType<UntypedDeconstructor>(typeof(object), "Deconstructor");                    

                    DefineSingletonInstanceField(typeBuilder);
                    ImplementOutputObjectOrArray(typeBuilder, OverrideMethod(typeBuilder, typeof(UntypedDeconstructor), "OutputObjectOrArray"));

                    defaultInstanceField = typeBuilder.CreateType().GetPublicStaticField(SingletonInstanceFieldName);

                    var typeSerializerField = defaultInstanceField.DeclaringType.GetField(typeof(TypeSerializer).Name);

                    typeSerializerField.SetValue(defaultInstanceField.GetValue(0), typeSerializer);
                }

                return deconstructorInstanceFields[type] = defaultInstanceField;
            }

            void ImplementOutputObjectOrArray(TypeBuilder typeBuilder, ILGenerator ilGenerator)
            {
                var typeSerializerFieldBuilder = typeBuilder.DefineField(typeof(TypeSerializer).Name, typeof(TypeSerializer), FieldAttributes.Public);

                var possibleObjectOrArrayTypes =
                    Enumerable.Union(
                        convertToJsonValueMethods.Keys.Where(typeof(IEnumerable<KeyValuePair<string, object>>).IsAssignableFrom),
                        convertToJsonValueMethods.Keys.Where(typeof(IEnumerable).IsAssignableFrom)).
                        ToArray();

                var otherTypes =
                    convertToJsonValueMethods.Keys.Except(possibleObjectOrArrayTypes).ToArray();

                ImplementTryConvertAndOutput(possibleObjectOrArrayTypes, ilGenerator);
                ImplementCallUntypedDeconstructorBase(ilGenerator);
                ImplementTryConvertAndOutput(otherTypes, ilGenerator);
                ImplementCallTypeSerializerFromUntypedDeconstructor(typeSerializerFieldBuilder, ilGenerator);

                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Ret);
            }


            FieldInfo CreateSingletonType(Type type, string namePrefix, Type interfaceType, IDictionary<Type, FieldInfo> typeCache, params Tuple<string, Action<Type, ILGenerator>>[] implementMethods)
            {
                var typeBuilder = DefineGeneratedType(type, namePrefix, interfaceType);

                var defaultInstanceFieldBuilder = DefineSingletonInstanceField(typeBuilder);

                typeCache[type] = defaultInstanceFieldBuilder;

                foreach (var implementMethod in implementMethods)
                    implementMethod.Item2(type, OverrideMethod(typeBuilder, interfaceType, implementMethod.Item1));

                var defaultInstanceField = typeBuilder.CreateType().GetPublicStaticField(SingletonInstanceFieldName);

                typeCache[type] = defaultInstanceField;

                return defaultInstanceField;
            }

            TypeBuilder DefineGeneratedType(Type type, string namePrefix, params Type[] interfaceTypes)
            {
                return DefineGeneratedType<object>(type, namePrefix, interfaceTypes);
            }

            TypeBuilder DefineGeneratedType<TBase>(Type type, string namePrefix, params Type[] interfaceTypes)
            {
                return
                    moduleBuilder.DefineType(
                        GetGeneratedTypeName(namePrefix, type),
                        TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.BeforeFieldInit,
                        typeof(TBase),
                        interfaceTypes);
            }



            FieldBuilder DefineSingletonInstanceField(TypeBuilder typeBuilder)
            {
                var instanceFieldBuilder = typeBuilder.DefineField(SingletonInstanceFieldName, typeBuilder, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);

                var instanceConstructorBuilder = ImplementSingletonDefaultConstructor(typeBuilder);

                ImplementSingletonClassConstructor(typeBuilder, instanceFieldBuilder, instanceConstructorBuilder);

                return instanceFieldBuilder;
            }

            ConstructorBuilder ImplementSingletonDefaultConstructor(TypeBuilder typeBuilder)
            {
                var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Private, CallingConventions.HasThis, Type.EmptyTypes);

                var ilGenerator = constructorBuilder.GetILGenerator();

                ilGenerator.Emit(OpCodes.Ret);

                return constructorBuilder;
            }

            void ImplementSingletonClassConstructor(TypeBuilder typeBuilder, FieldBuilder instanceFieldBuilder, ConstructorInfo instanceConstructor)
            {                
                var classConstructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig, CallingConventions.Standard, null);

                var ilGenerator = classConstructorBuilder.GetILGenerator();

                ilGenerator.Emit(OpCodes.Newobj, instanceConstructor);
                ilGenerator.Emit(OpCodes.Stsfld, instanceFieldBuilder);
                ilGenerator.Emit(OpCodes.Ret);
            }

            void ImplementGetObjectValueBuilderProvider(Type type, ILGenerator ilGenerator)
            {
                ImplementForEachWritablePropertyIfNameArgumentEquals(type, ilGenerator, property => ImplementGetBuilderProvider(ilGenerator, property.Type));
                ImplementGetBuilderProvider(ilGenerator, typeof(object));
            }

            static void ImplementCallMethod(ILGenerator ilGenerator, MethodInfo method)
            {
                ilGenerator.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
            }

            IEnumerable<Property> GetObjectProperties(Type type, bool writable = false)
            {
                var properties =
                    from property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    let accessorMethod = writable ? property.GetSetMethod() : property.GetGetMethod()
                    where accessorMethod != null && accessorMethod.GetParameters().Length == (writable ? 1 : 0)
                    select
                        new Property
                        {
                            Member = (MemberInfo)property,
                            Type = property.PropertyType,
                            IsSpecifiedMember = GetIsSpecifiedMember(type, property.Name)
                        };

                var fields =
                    from field in type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                    where !(writable && field.IsInitOnly)
                    select
                        new Property
                        {
                            Member = (MemberInfo)field,
                            Type = field.FieldType,
                            IsSpecifiedMember = GetIsSpecifiedMember(type, field.Name)
                        };

                var propertiesAndFields = properties.Concat(fields).ToArray();
                var isSpecifiedMembers = new HashSet<MemberInfo>(propertiesAndFields.Select(property => property.IsSpecifiedMember));

                return propertiesAndFields.Where(property => !isSpecifiedMembers.Contains(property.Member));
            }

            MemberInfo GetIsSpecifiedMember(Type type, string name)
            {
                var isSpecifiedMemberName = getIsSpecifiedMember(name);
                
                if (isSpecifiedMemberName != null)
                {
                    var property = type.GetProperty(isSpecifiedMemberName, BindingFlags.Public | BindingFlags.Instance);

                    if (property != null)
                    {
                        AssertIsSpecifiedTypeHasNoIndexParameters(type, isSpecifiedMemberName, property.GetIndexParameters());
                        AssertIsSpecifiedTypeIsBool(type, isSpecifiedMemberName, property.PropertyType);
                        return property;
                    }

                    var field = type.GetField(isSpecifiedMemberName, BindingFlags.Public | BindingFlags.Instance);

                    if (field != null)
                    {
                        AssertIsSpecifiedTypeIsBool(type, isSpecifiedMemberName, field.FieldType);
                        return field;
                    }
                }

                return null;
            }

            static void AssertIsSpecifiedTypeIsBool(Type declaringType, string name, Type memberType)
            {
                if (memberType == typeof(bool))
                    return;

                throw new InvalidOperationException(string.Format("Property or field '{0}' in type '{1}' must be of type bool", name, declaringType));
            }

            static void AssertIsSpecifiedTypeHasNoIndexParameters(Type declaringType, string name, ParameterInfo[] indexParameters)
            {
                if (indexParameters.Length == 0)
                    return;

                throw new InvalidOperationException(string.Format("Property '{0}' in type '{1}' can not have index parameters", name, declaringType));
            }


            void ImplementForEachWritablePropertyIfNameArgumentEquals(Type type, ILGenerator ilGenerator, Action<Property> implementForProperty)
            {
                foreach (var property in GetObjectProperties(type, writable: true))
                {
                    var label = ilGenerator.DefineLabel();

                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Ldstr, getObjectPropertyName(property.Member.Name));
                    ImplementCallMethod(ilGenerator, stringCompareMethod);

                    ilGenerator.Emit(OpCodes.Brfalse_S, label);

                    implementForProperty(property);

                    ilGenerator.MarkLabel(label);
                }
            }

            void ImplementGetArrayElementBuilderProvider(Type type, ILGenerator ilGenerator)
            {
                ImplementGetBuilderProvider(ilGenerator, type.GetArrayOrGenericCollectionElementType());
            }

            void ImplementGetObjectBuilder(Type type, ILGenerator ilGenerator)
            {
                ImplementGetBuilder(type, ilGenerator, "ObjectBuilder", typeof(IObjectBuilder), objectBuilderTypes, type, ImplementObjectBuilderAdd);
            }

            void ImplementGetArrayBuilder(Type type, ILGenerator ilGenerator)
            {
                var listType = typeof(List<>).MakeGenericType(type.GetArrayOrGenericCollectionElementType());

                ImplementGetBuilder(type, ilGenerator, "ArrayBuilder", typeof(IArrayBuilder), arrayBuilderTypes, listType, ImplementArrayBuilderAdd);
            }

            void ImplementGetBuilder(
                Type type, ILGenerator ilGenerator,
                string namePrefix, Type interfaceType, Dictionary<Type, Type> typeCache,
                Type resultFieldType, Action<Type, ILGenerator, FieldInfo, FieldInfo> implementAdd)
            {
                Type builderType;
                if (!typeCache.TryGetValue(type, out builderType))
                {
                    var typeBuilder = DefineGeneratedType(type, namePrefix, interfaceType);
                    var resultField = ImplementBuilderConstructor(typeBuilder, resultFieldType);
                    var mandatoryFieldsCounterField = (FieldInfo)null;

                    if (enableMandatoryFieldsValidation &&
                        interfaceType == typeof(IObjectBuilder) &&
                        GetObjectProperties(type, writable: true).Any(IsMandatoryProperty))
                    {
                        mandatoryFieldsCounterField = typeBuilder.DefineField("mandatoryFieldsCounter", typeof(int), FieldAttributes.Private);
                    }

                    implementAdd(type, OverrideMethod(typeBuilder, interfaceType, "Add"), resultField, mandatoryFieldsCounterField);
                    ImplementBuilderEnd(type, OverrideMethod(typeBuilder, interfaceType, "End"), resultField, mandatoryFieldsCounterField);

                    builderType = typeBuilder.CreateType();
                    typeCache.Add(type, builderType);
                }

                ilGenerator.Emit(OpCodes.Newobj, builderType.GetPublicDefaultConstructor());
                ilGenerator.Emit(OpCodes.Ret);
            }


            FieldInfo ImplementBuilderConstructor(TypeBuilder typeBuilder, Type resultFieldType)
            {
                var resultFieldTypeConstructor = 
                    resultFieldType.GetPublicDefaultConstructor() ??
                    resultFieldType.GetPublicConstructorWithMostNonByRefParameters();

                
                if (resultFieldTypeConstructor != null &&
                    resultFieldTypeConstructor.GetParameters().Length > 0)
                {
                    resultFieldType = CreateImmutableTypeBuilderHelperType(resultFieldType, resultFieldTypeConstructor);
                    resultFieldTypeConstructor = resultFieldType.GetPublicDefaultConstructor();
                }


                var resultFieldBuilder = typeBuilder.DefineField("result", resultFieldType, FieldAttributes.InitOnly | FieldAttributes.Private);
                var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);

                var ilGenerator = constructorBuilder.GetILGenerator();

                if (resultFieldTypeConstructor != null && !resultFieldType.IsAbstract)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Newobj, resultFieldTypeConstructor);
                    ilGenerator.Emit(OpCodes.Stfld, resultFieldBuilder);
                    ilGenerator.Emit(OpCodes.Ret);
                }
                else
                {
                    ImplementThrowException(
                        ilGenerator,
                        typeof(InvalidOperationException),
                        string.Format("Can not deserialize abstract type or type without a public constructor (excluding ones with byref parameters), '{0}'", resultFieldType));
                }

                return resultFieldBuilder;
            }

            Type CreateImmutableTypeBuilderHelperType(Type resultFieldType, ConstructorInfo resultFieldTypeConstructor)
            {
                var typeBuilder = DefineGeneratedType(resultFieldType, "ImmutableTypeBuilderHelper", typeof(IMakeImmutableInstance));
                
                typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

                var fields =
                    resultFieldTypeConstructor.GetParameters().
                        ToDictionary(
                            parameter => parameter.Name, 
                            parameter => typeBuilder.DefineField(parameter.Name, parameter.ParameterType, FieldAttributes.Public));

                var ilGenerator = OverrideMethod(typeBuilder, typeof(IMakeImmutableInstance), "MakeImmutableInstance");

                foreach (var parameter in resultFieldTypeConstructor.GetParameters())
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, fields[parameter.Name]);
                }

                ilGenerator.Emit(OpCodes.Newobj, resultFieldTypeConstructor);
                ilGenerator.Emit(OpCodes.Ret);

                return typeBuilder.CreateType();
            }

            void ImplementArrayBuilderAdd(Type type, ILGenerator ilGenerator, FieldInfo resultField, FieldInfo mandatoryFieldsCounterField)
            {
                var elementType = type.GetArrayOrGenericCollectionElementType();
                var addMethod = resultField.FieldType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new[] { elementType }, null);

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, resultField);
                ilGenerator.Emit(OpCodes.Ldarg_1);

                ImplementConvertFromJsonValue(ilGenerator, elementType);               
                ImplementCallMethod(ilGenerator, addMethod);                

                ilGenerator.Emit(OpCodes.Ret);
            }

            void ImplementObjectBuilderAdd(Type type, ILGenerator ilGenerator, FieldInfo resultField, FieldInfo mandatoryFieldsCounterField)
            {
                ImplementForEachWritablePropertyIfNameArgumentEquals(
                    resultField.FieldType, ilGenerator,
                    property =>
                    {
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldfld, resultField);                        
                        ilGenerator.Emit(OpCodes.Ldarg_2);

                        ImplementConvertFromJsonValue(ilGenerator, property.Type);     
                        ImplementSetMember(ilGenerator, property.Member);

                        if (property.IsSpecifiedMember != null)
                        {
                            ilGenerator.Emit(OpCodes.Ldarg_0);
                            ilGenerator.Emit(OpCodes.Ldfld, resultField);  
                            ilGenerator.Emit(OpCodes.Ldc_I4_1);

                            ImplementSetMember(ilGenerator, property.IsSpecifiedMember);    
                        }

                        if (mandatoryFieldsCounterField != null && IsMandatoryProperty(property))
                        {
                            ilGenerator.Emit(OpCodes.Ldarg_0);
                            ilGenerator.Emit(OpCodes.Ldarg_0);
                            ilGenerator.Emit(OpCodes.Ldfld, mandatoryFieldsCounterField);
                            ilGenerator.Emit(OpCodes.Ldc_I4_1);
                            ilGenerator.Emit(OpCodes.Add);
                            ilGenerator.Emit(OpCodes.Stfld, mandatoryFieldsCounterField);                            
                        }

                        ilGenerator.Emit(OpCodes.Ret);
                    });

                ilGenerator.Emit(OpCodes.Ret);
            }

            void ImplementBuilderEnd(Type type, ILGenerator ilGenerator, FieldInfo resultField, FieldInfo mandatoryFieldsCounterField)
            {
                if (mandatoryFieldsCounterField != null)
                    ImplementMandatoryFieldsValidation(type, ilGenerator, mandatoryFieldsCounterField);                

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, resultField);

                var convertMethod =
                    type.IsArray
                        ? toArrayGenericMethod.MakeGenericMethod(type.GetArrayOrGenericCollectionElementType())
                        : type.IsGenericType(fsharpListType)
                              ? GetToFSharpListGenericMethod().MakeGenericMethod(type.GetArrayOrGenericCollectionElementType())
                              : null;
                
                if (convertMethod != null)
                    ilGenerator.Emit(OpCodes.Call, convertMethod);  
                else if (typeof(IMakeImmutableInstance).IsAssignableFrom(resultField.FieldType))
                    ilGenerator.Emit(OpCodes.Callvirt, typeof(IMakeImmutableInstance).GetMethod("MakeImmutableInstance"));                

                ilGenerator.Emit(OpCodes.Ret);
            }



            void ImplementMandatoryFieldsValidation(Type type, ILGenerator ilGenerator, FieldInfo mandatoryFieldsCounterField)
            {
                var isSuccessLabel = ilGenerator.DefineLabel();

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, mandatoryFieldsCounterField);
                ilGenerator.Emit(OpCodes.Ldc_I4, GetObjectProperties(type, writable: true).Count(IsMandatoryProperty));
                ilGenerator.Emit(OpCodes.Ceq);
                ilGenerator.Emit(OpCodes.Brtrue_S, isSuccessLabel);

                ImplementThrowException(ilGenerator, typeof(FormatException), string.Format("One or more mandatory fields in json input is missing for object type '{0}'", type.FullName));

                ilGenerator.MarkLabel(isSuccessLabel);
            }

            void ImplementConvertFromJsonValue(ILGenerator ilGenerator, Type toType)
            {               
                if (IsOptionalWrapperType(toType))
                {
                    var nonOptionalType = toType.GetGenericArguments()[0];                    
                    var optionalTypeConstructor = toType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new[] { nonOptionalType }, null);

                    if (optionalTypeConstructor != null)
                    {
                        ImplementConvertFromJsonValue(ilGenerator, nonOptionalType);

                        ilGenerator.Emit(OpCodes.Newobj, optionalTypeConstructor);
                        return;
                    }
                }

                var nonNullableType = toType.IsGenericType(typeof(Nullable<>)) ? toType.GetGenericArguments()[0] : toType;

                MethodInfo convertMethod;
                if (convertFromJsonValueMethods.TryGetValue(nonNullableType, out convertMethod) ||
                    (IsNonJsonNativeValueType(nonNullableType) && TryGetOrImplementConvertMethodFromStaticParseMethod(nonNullableType, out convertMethod)))
                {
                    ImplementCallMethod(ilGenerator, convertMethod);

                    if (toType.IsValueType && !toType.IsGenericType(typeof(Nullable<>)))
                        ImplementGetValueFromNullable(ilGenerator, typeof(Nullable<>).MakeGenericType(toType));                    

                    return;
                } 

                ilGenerator.Emit(OpCodes.Unbox_Any, toType);
            }

            static bool TryGetStaticParseMethod(Type type, out MethodInfo parseMethod)
            {
                return
                    TryGetStaticParseMethod(type, out parseMethod, typeof(bool), "TryParse", typeof(string), typeof(IFormatProvider), type.MakeByRefType()) ||
                    TryGetStaticParseMethod(type, out parseMethod, type, "Parse", typeof(string), typeof(IFormatProvider)) ||
                    TryGetStaticParseMethod(type, out parseMethod, typeof(bool), "TryParse", typeof(string), type.MakeByRefType()) ||
                    TryGetStaticParseMethod(type, out parseMethod, type, "Parse", typeof(string));
            }

            static bool TryGetStaticParseMethod(Type type, out MethodInfo parseMethod, Type returnType, string name, params Type[] parameterTypes)
            {
                parseMethod =
                    type.GetMethods(BindingFlags.Public | BindingFlags.Static).
                        SingleOrDefault(method => 
                            method.ReturnType == returnType && 
                            method.Name == name && 
                            method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));
                
                return parseMethod != null;
            }

            bool TryGetOrImplementConvertMethodFromStaticParseMethod(Type type, out MethodInfo convertMethod)
            {
                if (!convertFromJsonValueUsingStaticParseMethods.TryGetValue(type, out convertMethod))
                {
                    MethodInfo parseMethod;
                    if (!TryGetStaticParseMethod(type, out parseMethod))
                        return false;

                    var parserTypeBuilder = DefineGeneratedType(type, "Parser");
                    var methodBuilder = parserTypeBuilder.DefineMethod("ConvertFromJsonValue", MethodAttributes.Public | MethodAttributes.Static, typeof(Nullable<>).MakeGenericType(type), new[] { typeof(object) });
                    var ilGenerator = methodBuilder.GetILGenerator();

                    var parseMethodParameters = parseMethod.GetParameters();
                    var isTryParseMethod = parseMethodParameters.Last().ParameterType.IsByRef;
                    var parseMethodTakesFormatProviderArgument = parseMethodParameters.Any(parameter => parameter.ParameterType == typeof(IFormatProvider));


                    var ifNotNullLabel = ilGenerator.DefineLabel();

                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Brtrue_S, ifNotNullLabel);
                    ilGenerator.Emit(OpCodes.Ldnull);
                    ilGenerator.Emit(OpCodes.Unbox_Any, methodBuilder.ReturnType);
                    ilGenerator.Emit(OpCodes.Ret);
                    ilGenerator.MarkLabel(ifNotNullLabel);

                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Castclass, typeof(string));

                    if (parseMethodTakesFormatProviderArgument)
                        ImplementCallMethod(ilGenerator, typeof(CultureInfo).GetProperty("InvariantCulture", BindingFlags.Public | BindingFlags.Static).GetGetMethod());

                    if (isTryParseMethod)
                    {
                        var resultLocal = ilGenerator.DeclareLocal(type);
                        var ifSuccessLabel = ilGenerator.DefineLabel();

                        ilGenerator.Emit(OpCodes.Ldloca_S, resultLocal);
                        ImplementCallMethod(ilGenerator, parseMethod);

                        ilGenerator.Emit(OpCodes.Brtrue_S, ifSuccessLabel);
                        ImplementThrowException(ilGenerator, typeof(FormatException));

                        ilGenerator.MarkLabel(ifSuccessLabel);
                        ilGenerator.Emit(OpCodes.Ldloc_S, resultLocal);
                    }
                    else
                        ImplementCallMethod(ilGenerator, parseMethod);

                    ilGenerator.Emit(OpCodes.Box, type);
                    ilGenerator.Emit(OpCodes.Unbox_Any, methodBuilder.ReturnType);
                    ilGenerator.Emit(OpCodes.Ret);

                    convertMethod = parserTypeBuilder.CreateType().GetMethod(methodBuilder.Name, BindingFlags.Public | BindingFlags.Static);
                    convertFromJsonValueUsingStaticParseMethods[type] = convertMethod;
                }

                return true;
            }

            static bool IsNonJsonNativeValueType(Type type)
            {
                return 
                    type.IsValueType && 
                    type != typeof(double) && 
                    type != typeof(bool);
            }


            void ImplementGetValueFromNullable(ILGenerator ilGenerator, Type nullableType)
            {
                var localResult = ilGenerator.DeclareLocal(nullableType);

                ilGenerator.Emit(OpCodes.Stloc, localResult);
                ilGenerator.Emit(OpCodes.Ldloca, localResult);

                ImplementCallMethod(ilGenerator, nullableType.GetProperty("Value").GetGetMethod());
            }

            static void ImplementGetMember(ILGenerator ilGenerator, MemberInfo member)
            {
                if (member is PropertyInfo)
                    ImplementCallMethod(ilGenerator, ((PropertyInfo)member).GetGetMethod());
                else
                    ilGenerator.Emit(OpCodes.Ldfld, (FieldInfo)member);
            }

            static void ImplementSetMember(ILGenerator ilGenerator, MemberInfo member)
            {
                if (member is PropertyInfo)                
                    ImplementCallMethod(ilGenerator, ((PropertyInfo)member).GetSetMethod());
                else
                    ilGenerator.Emit(OpCodes.Stfld, (FieldInfo)member);
            }

            void ImplementGetBuilderProvider(ILGenerator ilGenerator, Type childType)
            {
                var instanceField = GetOrCreateBuilderProviderInstanceField(IsOptionalWrapperType(childType) ? childType.GetGenericArguments()[0] : childType);

                if (instanceField != null)
                {
                    ilGenerator.Emit(OpCodes.Ldsfld, instanceField);
                    ilGenerator.Emit(OpCodes.Ret);
                    return;
                }

                ImplementThrowException(ilGenerator, typeof(InvalidOperationException));
            }

            void ImplementTryConvertAndOutput(IEnumerable<Type> fromTypes, ILGenerator ilGenerator)
            {
                foreach (var type in fromTypes)
                {
                    var continueLabel = ilGenerator.DefineLabel();
                    var convertMethod = convertToJsonValueMethods[type];

                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Isinst, type);
                    ilGenerator.Emit(OpCodes.Brfalse_S, continueLabel);

                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Unbox_Any, type);
                    ilGenerator.Emit(OpCodes.Call, convertMethod);

                    ImplementOutputValue(ilGenerator, convertMethod.ReturnType);

                    ilGenerator.Emit(OpCodes.Ldc_I4_1);
                    ilGenerator.Emit(OpCodes.Ret);

                    ilGenerator.MarkLabel(continueLabel);
                }
            }

            void ImplementCallUntypedDeconstructorBase(ILGenerator ilGenerator)
            {
                var continueLabel = ilGenerator.DefineLabel();

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Call, typeof(UntypedDeconstructor).GetMethod("OutputObjectOrArray", BindingFlags.NonPublic | BindingFlags.Instance));
                ilGenerator.Emit(OpCodes.Brfalse_S, continueLabel);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Ret);
                
                ilGenerator.MarkLabel(continueLabel);
            }

            void ImplementCallTypeSerializerFromUntypedDeconstructor(FieldInfo typeSerializerField, ILGenerator ilGenerator)
            {
                var typeLocal = ilGenerator.DeclareLocal(typeof(Type));
                var isClassOrInterfaceLabel = ilGenerator.DefineLabel();
                var outputEmptyObjectLabel = ilGenerator.DefineLabel();
                var continueLabel = ilGenerator.DefineLabel();

                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Call, typeof(object).GetMethod("GetType"));
                ilGenerator.Emit(OpCodes.Stloc, typeLocal);

                ilGenerator.Emit(OpCodes.Ldloc, typeLocal);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(Type).GetProperty("IsClass").GetGetMethod());
                ilGenerator.Emit(OpCodes.Brtrue_S, isClassOrInterfaceLabel);
                
                ilGenerator.Emit(OpCodes.Ldloc, typeLocal);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(Type).GetProperty("IsInterface").GetGetMethod());
                ilGenerator.Emit(OpCodes.Brtrue_S, isClassOrInterfaceLabel);

                ilGenerator.Emit(OpCodes.Br_S, continueLabel);
                
                ilGenerator.MarkLabel(isClassOrInterfaceLabel);                
                ilGenerator.Emit(OpCodes.Ldloc, typeLocal);
                ilGenerator.Emit(OpCodes.Ldtoken, typeof(object));
                ilGenerator.Emit(OpCodes.Call, typeof(object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static));
                ilGenerator.Emit(OpCodes.Brtrue_S, outputEmptyObjectLabel);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, typeSerializerField);
                ilGenerator.Emit(OpCodes.Ldloc, typeLocal);
                ilGenerator.Emit(OpCodes.Call, typeof(TypeSerializer).GetMethod("GetDeconstructor"));
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(IDeconstructor).GetMethod("Deconstruct"));
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Ret);

                ilGenerator.MarkLabel(outputEmptyObjectLabel);
                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(IJsonOutput).GetMethod("BeginObject"));
                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(IJsonOutput).GetMethod("EndObject"));
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Ret);

                ilGenerator.MarkLabel(continueLabel);

                
            }
            
            void ImplementDeconstructObject(Type type, ILGenerator ilGenerator)
            {
                var typedValueLocal = ImplementCastFirstArgToType(type, ilGenerator);

                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(IJsonOutput).GetMethod("BeginObject"));

                foreach (var property in GetObjectProperties(typedValueLocal.LocalType))
                {
                    var propertyName = getObjectPropertyName(property.Member.Name);
                    var propertyLocal = ilGenerator.DeclareLocal(property.Type);
                    var endPropertyLabel = ilGenerator.DefineLabel();

                    ilGenerator.Emit(OpCodes.Ldloc, typedValueLocal);
                    ImplementGetMember(ilGenerator, property.Member);
                    ilGenerator.Emit(OpCodes.Stloc, propertyLocal);

                    ImplementOutputValueOrCallSubDeconstructor(ilGenerator, property.Type, propertyLocal, endPropertyLabel, propertyName, property.IsSpecifiedMember);

                    ilGenerator.MarkLabel(endPropertyLabel);
                }

                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(IJsonOutput).GetMethod("EndObject"));
                ilGenerator.Emit(OpCodes.Ret);
            }

            void ImplementDeconstructArray(Type type, ILGenerator ilGenerator)
            {                
                var arrayLocal = ImplementCastFirstArgToType(type, ilGenerator);

                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(IJsonOutput).GetMethod("BeginArray"));
             
                var elementType = type.GetArrayOrGenericCollectionElementType();
                var elementLocal = ilGenerator.DeclareLocal(elementType);

                var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
                var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);                
                var enumeratorLocal = ilGenerator.DeclareLocal(enumeratorType);
                                
                var loopBodyLabel = ilGenerator.DefineLabel();
                var loopConditionLabel = ilGenerator.DefineLabel();
                var endFinallyLabel = ilGenerator.DefineLabel();
                var endMethodLabel = ilGenerator.DefineLabel();
                
                ilGenerator.Emit(OpCodes.Ldloc_S, arrayLocal);
                ilGenerator.Emit(OpCodes.Callvirt, enumerableType.GetMethod("GetEnumerator"));
                ilGenerator.Emit(OpCodes.Stloc_S, enumeratorLocal);
                
                ilGenerator.BeginExceptionBlock();
                
                ilGenerator.Emit(OpCodes.Br_S, loopConditionLabel);
                
                ilGenerator.MarkLabel(loopBodyLabel);
                ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
                ilGenerator.Emit(OpCodes.Callvirt, enumeratorType.GetProperty("Current").GetGetMethod());
                ilGenerator.Emit(OpCodes.Stloc, elementLocal);

                ImplementOutputValueOrCallSubDeconstructor(ilGenerator, elementType, elementLocal, loopConditionLabel, null, null);
                
                ilGenerator.MarkLabel(loopConditionLabel);
                ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext"));
                ilGenerator.Emit(OpCodes.Brtrue_S, loopBodyLabel);
                ilGenerator.Emit(OpCodes.Leave_S, endMethodLabel);
                
                ilGenerator.BeginFinallyBlock();
                ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
                ilGenerator.Emit(OpCodes.Brfalse_S, endFinallyLabel);
                ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(IDisposable).GetMethod("Dispose"));
                ilGenerator.MarkLabel(endFinallyLabel);
                ilGenerator.EndExceptionBlock();
                
                ilGenerator.MarkLabel(endMethodLabel);

                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Callvirt, typeof(IJsonOutput).GetMethod("EndArray"));

                ilGenerator.Emit(OpCodes.Ret);
            }

            void ImplementOutputValueOrCallSubDeconstructor(ILGenerator ilGenerator, Type type, LocalBuilder valueLocal, Label continueLabel, string propertyName, MemberInfo isSpecifiedMember)
            {
                var skipNullCheck = false;

                if (!ImplementGetOptionalValue(ilGenerator, ref type, ref valueLocal, continueLabel))
                {                   
                    if (isSpecifiedMember != null)
                    {
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ImplementGetMember(ilGenerator, isSpecifiedMember);
                        ilGenerator.Emit(OpCodes.Brfalse, continueLabel);
                    }
                    else if (skipOutputOfDefaultValues)
                    {
                        var defaultValueLocal = ilGenerator.DeclareLocal(type);
                        
                        ilGenerator.Emit(OpCodes.Call, typeof(EqualityComparer<>).MakeGenericType(type).GetProperty("Default").GetGetMethod());
                        ilGenerator.Emit(OpCodes.Ldloca, defaultValueLocal);
                        ilGenerator.Emit(OpCodes.Initobj, type);
                        ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
                        ilGenerator.Emit(OpCodes.Ldloc, defaultValueLocal);
                        ilGenerator.Emit(OpCodes.Callvirt, typeof(IEqualityComparer<>).MakeGenericType(type).GetMethod("Equals", new[] { type, type }));
                        ilGenerator.Emit(OpCodes.Brtrue, continueLabel);

                        skipNullCheck = true;
                    }

                    if (type.IsAssignableFrom(typeof(Undefined)))
                    {
                        ilGenerator.Emit(OpCodes.Ldsfld, typeof(Undefined).GetField("Value", BindingFlags.Public | BindingFlags.Static));
                        ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
                        ilGenerator.Emit(OpCodes.Call, typeof(Undefined).GetMethod("Equals"));
                        ilGenerator.Emit(OpCodes.Brtrue, continueLabel);
                    }
                }

                if (propertyName != null)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    ilGenerator.Emit(OpCodes.Ldstr, propertyName);
                    ilGenerator.Emit(OpCodes.Callvirt, typeof(IJsonOutput).GetMethod("NamedProperty"));
                }

                if (!skipNullCheck && type.CanStoreNullValue())
                {
                    var isNotNullLabel = ilGenerator.DefineLabel();

                    ilGenerator.Emit(OpCodes.Ldloc, valueLocal);                    
                    ilGenerator.Emit(OpCodes.Box, type);
                    ilGenerator.Emit(OpCodes.Brtrue_S, isNotNullLabel);
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    ilGenerator.Emit(OpCodes.Callvirt, typeof(IJsonOutput).GetMethod("Null"));
                    ilGenerator.Emit(OpCodes.Br, continueLabel);
                    ilGenerator.MarkLabel(isNotNullLabel);
                }

                if (!convertToJsonValueMethods.ContainsKey(type))
                {
                    var subDeconstructorInstanceField = GetOrCreateDesctructorInstanceField(type);
                    if (subDeconstructorInstanceField != null)
                    {                        
                        ilGenerator.Emit(OpCodes.Ldsfld, subDeconstructorInstanceField);
                        ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
                        ilGenerator.Emit(OpCodes.Ldarg_2);
                        ilGenerator.Emit(OpCodes.Callvirt, typeof(IDeconstructor).GetMethod("Deconstruct"));
                        return;
                    }
                }

                ilGenerator.Emit(OpCodes.Ldarg_2);            
                ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
                
                var nonNullableType = type.IsGenericType(typeof(Nullable<>)) ? type.GetGenericArguments()[0] : type;

                if (nonNullableType != type)
                {
                    ilGenerator.Emit(OpCodes.Box, type);
                    ilGenerator.Emit(OpCodes.Unbox_Any, nonNullableType);
                }

                var convertedToType = nonNullableType;

                MethodInfo convertMethod;
                if (convertToJsonValueMethods.TryGetValue(nonNullableType, out convertMethod))
                {
                    ImplementCallMethod(ilGenerator, convertMethod);
                    convertedToType = convertMethod.ReturnType;
                }

                ImplementOutputValue(ilGenerator, convertedToType);
            }

            static void ImplementOutputValue(ILGenerator ilGenerator, Type type)
            {
                var outputMethodName = "String";

                if (type == typeof(bool))
                {
                    outputMethodName = "Boolean";
                }
                else if (type == typeof(double))
                {
                    outputMethodName = "Number";
                }
                else if (type != typeof(string))
                {
                    var local = ilGenerator.DeclareLocal(type);

                    ilGenerator.Emit(OpCodes.Stloc, local);
                    ilGenerator.Emit(OpCodes.Ldloca, local);
                    ilGenerator.Emit(OpCodes.Constrained, type);
                    ilGenerator.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString"));
                }

                ilGenerator.Emit(OpCodes.Callvirt, typeof(IJsonOutput).GetMethod(outputMethodName));
            }

            LocalBuilder ImplementCastFirstArgToType(Type type, ILGenerator ilGenerator)
            {
                var typedValueLocal = ilGenerator.DeclareLocal(type);

                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Castclass, type);                
                ilGenerator.Emit(OpCodes.Stloc_S, typedValueLocal);

                return typedValueLocal;
            }

            bool ImplementGetOptionalValue(ILGenerator ilGenerator, ref Type type, ref LocalBuilder valueLocal, Label continueLabel)
            {
                if (IsOptionalWrapperType(type))
                {
                    var nonOptionalType = type.GetGenericArguments()[0];
                    var valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance, null, nonOptionalType, Type.EmptyTypes, null);
                    var isSpecifiedProperty = type.GetProperty("IsSpecified", BindingFlags.Public | BindingFlags.Instance, null, typeof(bool), Type.EmptyTypes, null);

                    if (valueProperty != null &&
                        valueProperty.GetGetMethod() != null &&
                        isSpecifiedProperty != null &&
                        isSpecifiedProperty.GetGetMethod() != null)
                    {
                        ilGenerator.Emit(OpCodes.Ldloca, valueLocal);
                        ImplementCallMethod(ilGenerator, isSpecifiedProperty.GetGetMethod());
                        ilGenerator.Emit(OpCodes.Brfalse, continueLabel);
                        ilGenerator.Emit(OpCodes.Ldloca, valueLocal);
                        ImplementCallMethod(ilGenerator, valueProperty.GetGetMethod());

                        type = nonOptionalType;
                        valueLocal = ilGenerator.DeclareLocal(nonOptionalType);
                        ilGenerator.Emit(OpCodes.Stloc, valueLocal);
                        return true;
                    }
                }

                return false;
            }

            
            static void ImplementMethodBodyWithThrowException<TException>(Type type, ILGenerator ilGenerator) where TException : Exception
            {
                ImplementThrowException(ilGenerator, typeof(TException));
            }

 
            static void ImplementThrowException(ILGenerator ilGenerator, Type exceptionType, string message = null)
            {
                var constructorArgumentTypes = Type.EmptyTypes;

                if (message != null)
                {
                    ilGenerator.Emit(OpCodes.Ldstr, message);
                    constructorArgumentTypes = new[] { typeof(string) };
                }

                ilGenerator.Emit(OpCodes.Newobj, exceptionType.GetConstructor(constructorArgumentTypes));
                ilGenerator.Emit(OpCodes.Throw);
            }



            static ILGenerator OverrideMethod(TypeBuilder typeBuilder, Type declaringType, string methodName)
            {
                return OverrideMethod(typeBuilder, declaringType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            }

            static ILGenerator OverrideMethod(TypeBuilder typeBuilder, MethodInfo declaringMethod)
            {
                return
                    typeBuilder.DefineMethod(
                        declaringMethod.Name,
                        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | (declaringMethod.DeclaringType.IsInterface ? MethodAttributes.Final | MethodAttributes.NewSlot : 0),
                        declaringMethod.ReturnType,
                        declaringMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray()).
                        GetILGenerator();
            }

            static string GetGeneratedTypeName(string name, Type type)
            {
                var prefix = "Generated" + Type.Delimiter;

                if (type.IsArray)
                {
                    type = type.GetElementType();
                    prefix += Type.Delimiter + "ArrayOf_";
                }
                
                var typeNamespace = 
                    type.IsGenericType 
                    ? string.Join("_", new[] { type.GetGenericTypeDefinition().Name }.Concat(type.GetGenericArguments().Select(typeArgument => typeArgument.Name)))
                    : type.Name;

                return prefix + typeNamespace + Type.Delimiter + name + Interlocked.Increment(ref uniqueNameCounter); 
            }


            static bool IsOptionalWrapperType(Type toType)
            {
                return
                    toType.IsGenericType &&
                    toType.GetGenericTypeDefinition().Name == "Optional`1" &&
                    toType.GetGenericArguments().Length == 1 &&
                    toType.IsValueType;
            }

            static bool IsMandatoryProperty(Property property)
            {
                return !IsOptionalWrapperType(property.Type) && property.IsSpecifiedMember == null;
            }

            class Property
            {
                public MemberInfo Member;
                public Type Type;
                public MemberInfo IsSpecifiedMember;
            }



        }


    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Simple.Json.Serialization;
using Simple.Json.Tests.TypedJsonObjects;

using Xunit;

namespace Simple.Json.Tests
{
    public class JsonSerializerTests
    {
        static readonly string sample1Content = GetEmbeddedResourceContent("sample1.json");
        static readonly string sample1ContentCompact = GetEmbeddedResourceContent("sample1-compact.json");

        
        JsonSerializer serializer = JsonSerializer.Default;

        [Fact]
        public void CanOutputSample1AsJson()
        {
            var rootObject =
                new JsonObject
                {
                    {
                        "images", new JsonArray
                        {
                            new JsonObject
                            {
                                { "width", 1800 },
                                { "height", 1600 },
                                { "title", "View from 15th Floor" },
                                {
                                    "thumbnail", new JsonObject
                                    {
                                        { "url", "http://www.example.com/image/481989943" },
                                        { "height", 125 },
                                        { "width", 100 }
                                    }
                                },
                                {
                                    "ids", new JsonArray
                                    {
                                        116,
                                        943,
                                        234,
                                        38793
                                    }
                                },
                                { "visible", true },
                                { "archived", false },
                                { "creator", null },
                                { "scale", 750E-3 },
                                { "rotation", -90 },
                                { "createdAt", new DateTime(2012, 1, 1, 8, 0, 0) },
                                { "modifiedAt", new DateTime(2012, 11, 25, 10, 48, 25, 112) }
                            }
                        }
                    }
                };

             Assert.Equal(sample1Content, serializer.ToJson(rootObject, formatted: true));
             Assert.Equal(sample1ContentCompact, serializer.ToJson(rootObject, formatted: false));
        }
 
        [Fact]
        public void CanOutputSample1AsJsonTyped()
        {
            var rootObject =
                new ImageRequest
                {
                    Images = new[] 
                    {
                        new Image
                        {
                            Width = 1800,
                            Height = 1600,
                            Title = "View from 15th Floor",
                            Thumbnail =
                                new Thumbnail
                                {
                                    Url = "http://www.example.com/image/481989943",
                                    Height = 125,
                                    Width = 100
                                },
                            Ids =
                                new long[]
                                {
                                    116,
                                    943,
                                    234,
                                    38793
                                },
                            Visible = true,
                            Archived = false,
                            Creator = null,
                            Scale = 750E-3,
                            Rotation = -90,
                            CreatedAt = new DateTime(2012, 1, 1, 8, 0, 0),
                            ModifiedAt = new DateTime(2012, 11, 25, 10, 48, 25, 112)
                        }
                    }
                };

            Assert.Equal(sample1Content, serializer.ToJson(rootObject, formatted: true));
            Assert.Equal(sample1ContentCompact, serializer.ToJson(rootObject, formatted: false));
        }

        [Fact]
        public void CanParseSample1()
        {
            var rootObject = serializer.ParseJson(sample1Content) as JsonObject;
            Assert.NotNull(rootObject);
            Assert.Equal(new[] { "images" }, rootObject.GetPropertyNames());

            var imagesArray = rootObject["images"] as JsonArray;
            Assert.NotNull(imagesArray);
            Assert.Equal(1, imagesArray.Count);

            var imageObject = imagesArray[0] as JsonObject;
            Assert.NotNull(imageObject);
            Assert.Equal(new[] { "width", "height", "title", "thumbnail", "ids", "visible", "archived", "creator", "scale", "rotation", "createdAt", "modifiedAt" }, imageObject.GetPropertyNames());

            Assert.Equal(1800.0, imageObject["width"]);
            Assert.Equal(1600.0, imageObject["height"]);
            Assert.Equal("View from 15th Floor", imageObject["title"]);

            var thumbnailObject = imageObject["thumbnail"] as JsonObject;
            Assert.NotNull(thumbnailObject);
            Assert.Equal(new[] { "url", "height", "width" }, thumbnailObject.GetPropertyNames());
            Assert.Equal("http://www.example.com/image/481989943", thumbnailObject["url"]);
            Assert.Equal(125.0, thumbnailObject["height"]);
            Assert.Equal(100.0, thumbnailObject["width"]);

            var idsArray = imageObject["ids"] as JsonArray;
            Assert.Equal(new object[] { 116.0, 943.0, 234.0, 38793.0 }, idsArray);

            Assert.Equal(true, imageObject["visible"]);
            Assert.Equal(false, imageObject["archived"]);
            Assert.Equal(null, imageObject["creator"]);
            Assert.Equal(0.75, imageObject["scale"]);
            Assert.Equal(-90.0, imageObject["rotation"]);
        
            Assert.Equal("2012-01-01T08:00", imageObject["createdAt"]);
            Assert.Equal("2012-11-25T10:48:25.112", imageObject["modifiedAt"]);
        }

        [Fact]
        public void CanParseSample1Dynamic()
        {
            dynamic rootObject = serializer.ParseJson(sample1Content);
            Assert.NotNull(rootObject);            

            dynamic imagesArray = rootObject.images;
            Assert.NotNull(imagesArray);
            Assert.Equal(1, imagesArray.Count);

            dynamic imageObject = imagesArray[0];
            Assert.NotNull(imageObject);

            Assert.Equal(1800.0, imageObject.width);
            Assert.Equal(1600.0, imageObject.height);
            Assert.Equal("View from 15th Floor", imageObject.title);

            dynamic thumbnailObject = imageObject.thumbnail;
            Assert.NotNull(thumbnailObject);
            Assert.Equal("http://www.example.com/image/481989943", thumbnailObject.url);
            Assert.Equal(125.0, thumbnailObject.height);
            Assert.Equal(100.0, thumbnailObject.width);

            Assert.Equal(new object[] { 116.0, 943.0, 234.0, 38793.0 }, imageObject.ids);

            Assert.Equal(943.0, imageObject.ids[1]);

            Assert.Equal(true, imageObject.visible);
            Assert.Equal(false, imageObject.archived);
            Assert.Equal((string)null, imageObject.creator);
            Assert.Equal(0.75, imageObject.scale);
            Assert.Equal(-90.0, imageObject.rotation);

            Assert.Equal("2012-01-01T08:00", imageObject.createdAt);
            Assert.Equal("2012-11-25T10:48:25.112", imageObject.modifiedAt);

        }

        [Fact]
        public void CanParseSample1Typed()
        {
            var rootObject = serializer.ParseJson<ImageRequest>(sample1Content);
            Assert.NotNull(rootObject);

            var imagesArray = rootObject.Images;
            Assert.NotNull(imagesArray);
            Assert.Equal(1, imagesArray.Length);

            var imageObject = imagesArray[0];
            Assert.NotNull(imageObject);            

            Assert.Equal(1800.0, imageObject.Width);
            Assert.Equal(1600.0, imageObject.Height);
            Assert.Equal("View from 15th Floor", imageObject.Title);

            var thumbnailObject = imageObject.Thumbnail;
            Assert.NotNull(thumbnailObject);
            Assert.Equal("http://www.example.com/image/481989943", thumbnailObject.Url);
            Assert.Equal(125, thumbnailObject.Height);
            Assert.Equal(100, thumbnailObject.Width);

            Assert.Equal(new long[] { 116, 943, 234, 38793 }, imageObject.Ids);

            Assert.Equal(true, imageObject.Visible);
            Assert.Equal(false, imageObject.Archived);
            Assert.Equal(null, imageObject.Creator);
            Assert.Equal(0.75, imageObject.Scale);
            Assert.Equal(-90.0, imageObject.Rotation);

            Assert.Equal(new DateTime(2012, 1, 1, 8, 0, 0), imageObject.CreatedAt);
            Assert.Equal(new DateTime(2012, 11, 25, 10, 48, 25, 112), imageObject.ModifiedAt);
        }

        [Fact]
        public void MaxSerializeGraphDepthControlAllowedNestingDepth()
        {
            serializer = 
                new JsonSerializer(TypeSerializer.Default)
                {
                    MaxSerializeGraphDepth = 1
                };

            var obj =
                new JsonObject
                {
                    { "nestedObject", 
                        new JsonObject
                        {                            
                            { "secondNestedObject",
                                new JsonObject
                                {
                                    { "p1", 1 }
                                }
                            }
                        } 
                    }
                };

            Assert.Throws<InvalidOperationException>(() => serializer.ToJson(obj));

            serializer.MaxSerializeGraphDepth += 1;

            serializer.ToJson(obj);
        }

        [Fact]
        public void CanNotSerializePrimitiveValuesAtRootLevel()
        {
            Assert.Throws<InvalidOperationException>(() => serializer.ToJson<object>("hello json"));
            Assert.Throws<InvalidOperationException>(() => serializer.ToJson<object>(54.5));
            Assert.Throws<InvalidOperationException>(() => serializer.ToJson<object>(true));
            Assert.Throws<InvalidOperationException>(() => serializer.ToJson<object>(null));

            serializer.ToJson(new[]{ "hello json" });
            serializer.ToJson(new { p1 = 54.5 });
        }

        [Fact]
        public void CanDeserializeEmptyArraysAndObjects()
        {
            Assert.Empty((IEnumerable)serializer.ParseJson("{}"));
            Assert.Empty((IEnumerable)serializer.ParseJson("[]"));             
        }

        [Fact]
        public void CanSerializeEmptyArraysAndObjects()
        {
            Assert.Equal("{}", serializer.ToJson(new object()));
            Assert.Equal("[]", serializer.ToJson(EmptyEnumerable));
        }


        [Fact]
        public void CanParseNumberWithENotation()
        {
            Assert.Equal(100.0, serializer.ParseJson<double[]>("[1E2]").Single());
            Assert.Equal(0.001, serializer.ParseJson<double[]>("[1E-3]").Single());
            Assert.Equal(1.0, serializer.ParseJson<double[]>("[1e0]").Single());
        }

        [Fact]
        public void SerializeNanOrInfinityOutputsZero()
        {
            Assert.Equal("[0]", serializer.ToJson(new[] { double.NaN }));
            Assert.Equal("[0]", serializer.ToJson(new[] { double.PositiveInfinity }));
            Assert.Equal("[0]", serializer.ToJson(new[] { double.NegativeInfinity }));
        }

        [Fact]
        public void CanSerializeAndDeserializeAllNumberTypes()
        {
            AssertCanSerializeAndDeserializeValue<byte>(45);
            AssertCanSerializeAndDeserializeValue<sbyte>(45);
            AssertCanSerializeAndDeserializeValue<ushort>(45);
            AssertCanSerializeAndDeserializeValue<short>(45);
            AssertCanSerializeAndDeserializeValue<uint>(45);
            AssertCanSerializeAndDeserializeValue(45);
            AssertCanSerializeAndDeserializeValue<ulong>(45);
            AssertCanSerializeAndDeserializeValue<long>(45);
            AssertCanSerializeAndDeserializeValue<float>(45);
            AssertCanSerializeAndDeserializeValue<decimal>(45);
        }

        [Fact]
        public void CanSerializeAndDeserializeByteArrayAsBase64String()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var json = serializer.ToJson(new[] { bytes });

            Assert.True(json.StartsWith("[\"") && json.EndsWith("\"]"));
            Assert.Equal(bytes, serializer.ParseJson<byte[][]>(json).Single());
        }

        [Fact]
        public void CanSerializeAndDeserializeDateTime()
        {
            AssertCanSerializeAndDeserializeValue(new DateTime(634897091626152344, DateTimeKind.Unspecified));
            AssertCanSerializeAndDeserializeValue(new DateTime(634897091626152344, DateTimeKind.Local));
            AssertCanSerializeAndDeserializeValue(new DateTime(634897091626152344, DateTimeKind.Utc));
        }

        [Fact]
        public void CanSerializeAndDeserializeTimeSpan()
        {
            AssertCanSerializeAndDeserializeValue(TimeSpan.FromTicks(-12355667555334));
            AssertCanSerializeAndDeserializeValue(TimeSpan.FromTicks(12355667555334));
        }

        [Fact]
        public void EscapesControlAndSpecialCharactersInJsonStrings()
        {
            Assert.Equal("[\"Hello world!,\\r\\n\\r\\nFoo bar...\"]", serializer.ToJson(new[] { "Hello world!,\r\n\r\nFoo bar..." }));

            AssertCharacterSerializesIntoString(0x00, @"\u0000");
            AssertCharacterSerializesIntoString(0x1F, @"\u001F");

            AssertCharacterSerializesIntoString('"', "\\\"");
            AssertCharacterSerializesIntoString('\\', @"\\");

            AssertCharacterSerializesIntoString('\b', @"\b");
            AssertCharacterSerializesIntoString('\f', @"\f");
            AssertCharacterSerializesIntoString('\n', @"\n");
            AssertCharacterSerializesIntoString('\r', @"\r");
            AssertCharacterSerializesIntoString('\t', @"\t");
        }

        [Fact]
        public void CanParseEscapedCharactersInStrings()
        {
            Assert.Equal("Hello world!,\r\n\r\nFoo bar...", serializer.ParseJson<string[]>("[\"Hello \u0077orld!,\\r\\n\\r\\nFoo bar...\"]").Single());

            AssertParsesEscapedCharacterInString(@"\u0000", 0x00);
            AssertParsesEscapedCharacterInString(@"\u001F", 0x1F);

            AssertParsesEscapedCharacterInString(@"\u005A", 'Z');
            AssertParsesEscapedCharacterInString(@"\u010e", 'Ď');

            AssertParsesEscapedCharacterInString("\\\"", '"');
            AssertParsesEscapedCharacterInString(@"\\", '\\');
            AssertParsesEscapedCharacterInString(@"\/", '/');

            AssertParsesEscapedCharacterInString(@"\b", '\b');
            AssertParsesEscapedCharacterInString(@"\f", '\f');
            AssertParsesEscapedCharacterInString(@"\n", '\n');
            AssertParsesEscapedCharacterInString(@"\r", '\r');
            AssertParsesEscapedCharacterInString(@"\t", '\t');
        }

        [Fact]
        public void FailedParseReportsLineAndCharacterNumber()
        {
            AssertParsingResultsInError("[\n  1.0,\n\"\\u00ne\",12]", "Line 3, Char 6: expected hexadecimal digit ('0' - '9' | 'A' - 'F' | 'a' - 'f')");
            AssertParsingResultsInError("[\n  1.0,\n\"one\",\n12.]", "Line 4, Char 4: expected digit ('0' - '9')");

            AssertParsingResultsInError("1.0", "Line 1, Char 1: expected valid json array or json object");
            AssertParsingResultsInError("[1.0", "Line 1, Char 5: unexpected end of input");
            AssertParsingResultsInError("[1.0,", "Line 1, Char 6: expected valid json value");
            AssertParsingResultsInError("[1.0],", "Line 1, Char 6: expected end of input");

            AssertParsingResultsInError("{\"one\":1.0,", "Line 1, Char 12: expected valid json key value pair");
            AssertParsingResultsInError("{\"one\":1.0,\"two\":", "Line 1, Char 18: expected valid json value");

            AssertParsingResultsInError("[fals],", "Line 1, Char 7: expected 'e'");
        }

        [Fact]
        public void FallbacksToStringRepresentationOfValueTypesWithoutConversionMethod()
        {
            Assert.Equal(
                "[\"Simple.Json.Tests.JsonSerializerTests+SomeStruct\",\"Value2\"]", 
                serializer.ToJson<object>(new object[] { new SomeStruct(), SomeEnum.Value2 }));

            Assert.Equal(
                "{\"value\":\"Simple.Json.Tests.JsonSerializerTests+SomeStruct\"}",
                serializer.ToJson(new SomeClassWithValue<SomeStruct>()));
        }




        [Fact]
        public void SkipsUndefinedValuesInutput()
        {
            Assert.Equal("[1,2,3]", serializer.ToJson(new object[] { 1, 2, Undefined.Value, 3 }));
            Assert.Equal("{\"one\":1}", serializer.ToJson(new { one = 1, two = (object)Undefined.Value }));
        }

        [Fact]
        public void SkipsUndefinedValuesInUntypedOutput()
        {
            Assert.Equal("[1,2,3]", serializer.ToJson<object>(new object[] { 1, 2, Undefined.Value, 3 }));
            Assert.Equal("{\"one\":1}", serializer.ToJson<object>(new[] { new KeyValuePair<string, object>("one", 1), new KeyValuePair<string, object>("two", Undefined.Value) }));
        }

        [Fact]
        public void CanDeserializeImmutableTypes()
        {
            var obj = serializer.ParseJson<SomeImmutableClass>("{\"x\":1,\"y\":2}");

            Assert.Equal(1, obj.X);
            Assert.Equal(2, obj.Y);
        }

        [Fact]
        public void CanDifferBetweenNullAndNotSpecifiedWithOptionalPropertiesAndFields()
        {
            var obj1 = serializer.ParseJson<SomeClassWithOptionalPropertiesAndFields>("{\"x\":null}");
            var obj2 = serializer.ParseJson<SomeClassWithOptionalPropertiesAndFields>("{\"x\":null,\"y\":null,\"z\":null,\"w\":null}");

            Assert.Null(obj1.X);
            Assert.False(obj1.YIsSpecified);
            Assert.False(obj1.ZIsSpecified);
            Assert.False(obj1.W.IsSpecified);

            Assert.Null(obj2.X);
            Assert.Null(obj2.Y);
            Assert.True(obj2.YIsSpecified);
            Assert.Null(obj2.Z);
            Assert.True(obj2.ZIsSpecified);
            Assert.True(obj2.W.IsSpecified);
            Assert.Null(obj2.W.Value);
        }

        [Fact]
        public void IsSpecifiedValueControlsOutputOfObjectProperties()
        {
            Assert.Equal("{\"x\":null}", serializer.ToJson(new SomeClassWithOptionalPropertiesAndFields()));
            Assert.Equal("{\"x\":null,\"y\":null,\"z\":null,\"w\":null}", serializer.ToJson(new SomeClassWithOptionalPropertiesAndFields { YIsSpecified = true, ZIsSpecified = true, W = (double?)null }));
        }

        [Fact]
        public void IsSpecifiedFieldMustBeOfTypeBoolAndWithoutIndexerParameters()
        {
            Assert.Throws<InvalidOperationException>(() => serializer.ParseJson<SomeClassWithInvalidIsSpecifiedMemberType>("{\"x\":1}"));

            serializer = NewSerializerWithConfiguration(configuration => configuration.GetIsSpecifiedMemberDelegate = _ => "Item");

            Assert.Throws<InvalidOperationException>(() => serializer.ParseJson<SomeClassWithIndexer>("{\"x\":1}"));
        }


        [Fact]
        public void CanNotDeserializeObjectsToValueTypes()
        {
            Assert.Throws<InvalidOperationException>(() => serializer.ParseJson<SomeStruct>("{}"));
        }

        [Fact]
        public void CanNotDeserializeAbstractTypes()
        {
            Assert.Throws<InvalidOperationException>(() => serializer.ParseJson<SomeAbstractClass>("{}"));
        }

        [Fact]
        public void CanNotDeserializeTypesWithRefParametersInConstructor()
        {
            Assert.Throws<InvalidOperationException>(() => serializer.ParseJson<SomeClassWithRefParametersInConstructor>("{}"));
        }

        [Fact]
        public void CanParseValueTypesWithStaticTryParseOrParseMethods()
        {
            var json = "{\"value\":\"123\"}";

            Assert.Equal(123, serializer.ParseJson<SomeClassWithValue<SomeStructWithTryParse1>>(json).Value.IntValue);
            Assert.Equal(123, serializer.ParseJson<SomeClassWithValue<SomeStructWithParse1>>(json).Value.Value);
            Assert.Equal(123, serializer.ParseJson<SomeClassWithValue<SomeStructWithTryParse2>>(json).Value.Value);
            Assert.Equal(123, serializer.ParseJson<SomeClassWithValue<SomeStructWithParse2>>(json).Value.Value);
        }

        [Fact(Skip = "Enum support not implemented yet")]
        public void CanParseEnumValueTypes()
        {
            var json = "{\"value\":\"Value2\"}";

            Assert.Equal(SomeEnum.Value2, serializer.ParseJson<SomeClassWithValue<SomeEnum>>(json).Value);
            Assert.Equal(SomeEnum.Value2, serializer.ParseJson<SomeClassWithValue<SomeEnum?>>(json).Value);
        }

        [Fact]
        public void CanParseValueTypesWithCustomConvertMethods()
        {
            Assert.Throws<InvalidCastException>(() => serializer.ParseJson<SomeClassWithValue<SomeStruct>>("{\"value\":\"Simple.Json.Tests.JsonSerializerTests+SomeStruct\"}"));

            serializer = NewSerializerWithConfiguration(configuration => configuration.RegisterConvertFromJsonValueMethod(Methods.GetSomeStructFromStringValue));

            serializer.ParseJson<SomeClassWithValue<SomeStruct>>("{\"value\":\"Simple.Json.Tests.JsonSerializerTests+SomeStruct\"}");
            Assert.Throws<FormatException>(() => serializer.ParseJson<SomeClassWithValue<SomeStruct>>("{\"value\":\"BAD DATA\"}"));
        }

        [Fact]
        public void MustConfigureSerializerBeforeFirstUse()
        {
            JsonSerializer.Default.ParseJson("[]");

            Assert.Throws<InvalidOperationException>(() => TypeSerializer.Default.Configuration.EnableMandatoryFieldsValidation = true);
        }

        [Fact]
        public void CanConfigureSerializerToSkipDefaultValuesInOutput()
        {
            serializer = NewSerializerWithConfiguration(configuration => configuration.SkipOutputOfDefaultValues = true);

            Assert.Equal("{}", serializer.ToJson(new SomeClassWithOptionalPropertiesAndFields()));
            Assert.Equal("{\"y\":null,\"z\":null,\"w\":null}", serializer.ToJson(new SomeClassWithOptionalPropertiesAndFields { YIsSpecified = true, ZIsSpecified = true, W = (double?)null }));
        }

        [Fact]
        public void CanConfigureSerializerToTreatNonOptionalValuesAsMandatory()
        {
            serializer = NewSerializerWithConfiguration(configuration => configuration.EnableMandatoryFieldsValidation = true);

            Assert.Throws<FormatException>(() => serializer.ParseJson<SomeClassWithOptionalPropertiesAndFields>("{\"y\":null,\"z\":null,\"w\":null}"));
            serializer.ParseJson<SomeClassWithOptionalPropertiesAndFields>("{\"x\":null}");
        }


        [Fact]
        public void CanConfigureCustomPropertyNaming()
        {
            serializer = NewSerializerWithConfiguration(configuration => configuration.GetObjectPropertyNameDelegate = name => name.ToUpperInvariant());

            Assert.Equal("{\"VALUE\":42}", serializer.ToJson(new SomeClassWithValue<int> { Value = 42 }));
        }

        [Fact]
        public void CanConfigureCustomIsSpecifiedMemberName()
        {
            serializer = NewSerializerWithConfiguration(configuration => configuration.GetIsSpecifiedMemberDelegate = name => "IncludeInOutput" + name);

            Assert.Equal("{}", serializer.ToJson(new SomeClassWithCustomOptionalValue()));
            Assert.Equal("{\"x\":null}", serializer.ToJson(new SomeClassWithCustomOptionalValue { IncludeInOutputX = true }));
        }

        [Fact]
        public void ConfiguredConvertMethodsMustBePublicStaticMethodInVisibleTypes()
        {
            Assert.Throws<ArgumentException>(() => NewSerializerWithConfiguration(configuration => configuration.RegisterConvertFromJsonValueMethod<SomeStruct?>(_ => new SomeStruct())));
            Assert.Throws<ArgumentException>(() => NewSerializerWithConfiguration(configuration => configuration.RegisterConvertToJsonValueMethod<SomeStruct, string>(_ => "SomeStruct")));
        }

        [Fact]
        public void ConfiguredConvertFromMethodsMustReturnNullables()
        {
            Assert.Throws<ArgumentException>(() => NewSerializerWithConfiguration(configuration => configuration.RegisterConvertFromJsonValueMethod(Methods.ConvertFromUntyped<SomeStruct>)));            
        }

        [Fact]
        public void ConfiguredConvertToMethodsMustHaveNonNullableArgument()
        {
            Assert.Throws<ArgumentException>(() => NewSerializerWithConfiguration(configuration => configuration.RegisterConvertToJsonValueMethod<SomeStruct?, string>(Methods.ConvertTo<SomeStruct?, string>)));

        }

        [Fact]
        public void ConfiguredConvertToMethodsMustReturnNativeJsonPrimitiveTypes()
        {
            Assert.Throws<ArgumentException>(() => NewSerializerWithConfiguration(configuration => configuration.RegisterConvertToJsonValueMethod<SomeStruct, int>(Methods.ConvertTo<SomeStruct, int>)));
            
            NewSerializerWithConfiguration(
                configuration =>
                {
                    configuration.RegisterConvertToJsonValueMethod<SomeStruct, double>(Methods.ConvertTo<SomeStruct, double>);
                    configuration.RegisterConvertToJsonValueMethod<SomeStruct, bool>(Methods.ConvertTo<SomeStruct, bool>);
                    configuration.RegisterConvertToJsonValueMethod<SomeStruct, string>(Methods.ConvertTo<SomeStruct, string>);
                });
        }

        [Fact]
        public void CanParseAndOutputObjectAsDictionaryOfStringObjectPair()
        {
            var dictionary = serializer.ParseJson<IDictionary<string, object>>("{\"a\":1,\"b\":2}");

            Assert.Equal(2, dictionary.Count);
            Assert.Equal(1.0, dictionary["a"]);
            Assert.Equal(2.0, dictionary["b"]);

            Assert.Equal("{\"a\":1,\"b\":2}", serializer.ToJson(dictionary));
        }

        [Fact]
        public void CanParseLargeJsonDocumentFromTextReader()
        {
            var contentChars  = new[] { '[', '0' }.Concat(Enumerable.Range(1, 10000).SelectMany(i => ", " + i)).Concat(new[] { ']' });

            var intArray = (int[])serializer.ParseJson(new CharEnumerableAsTextReader(contentChars), typeof(int[]));

            Assert.True(intArray.SequenceEqual(Enumerable.Range(0, 10001)));
        }


        [Fact]
        public void NumberOfInstanceAllocationsOfTypeSerializerIsLimited()
        {
            InstanceCountConstrained<TypeSerializer>.MaxInstanceCount = 0;
            try
            {
                Assert.Throws<InvalidOperationException>(() => new JsonSerializer(new TypeSerializer()));
            }
            finally 
            {
                InstanceCountConstrained<TypeSerializer>.MaxInstanceCount = InstanceCountConstrained<TypeSerializer>.DefaultMaxInstanceCount;    
            }            
        }



        void AssertCanSerializeAndDeserializeValue<T>(T value)
        {
            Assert.Equal(value, serializer.ParseJson<T[]>(serializer.ToJson(new[] { value })).Single());
        }

        void AssertCharacterSerializesIntoString(int character, string expectedJsonStringWithoutQuotes)
        {
            Assert.Equal("[\"" + expectedJsonStringWithoutQuotes + "\"]", serializer.ToJson(new[] { char.ToString((char)character) }));
        }

        void AssertParsesEscapedCharacterInString(string jsonStringWithoutQuotes, int expectedCharacter)
        {
            Assert.Equal(expectedCharacter, serializer.ParseJson<string[]>("[\"" + jsonStringWithoutQuotes + "\"]").Single().Single());
        }

        void AssertParsingResultsInError(string json, string expectedErrorMessage)
        {
            var error = Assert.Throws<FormatException>(() => serializer.ParseJson(json));

            Assert.Equal(expectedErrorMessage, error.Message);
        }

        static string GetEmbeddedResourceContent(string resource)
        {
            var type = typeof(JsonSerializerTests);

            using (var stream = type.Assembly.GetManifestResourceStream(type, resource))
            {
                if (stream == null)
                    throw new FileNotFoundException("Embedded resource not found", resource);

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        static JsonSerializer NewSerializerWithConfiguration(Action<ITypeSerializerConfiguration> configureTypeSerializer)
        {
            var typeSerializer = new TypeSerializer();

            configureTypeSerializer(typeSerializer.Configuration);

            return new JsonSerializer(typeSerializer);
        }

        static IEnumerable EmptyEnumerable
        {
            get { yield break; }
        }

        class CharEnumerableAsTextReader : TextReader
        {
            IEnumerator<char> charsEnumerator;
            int current;

            public CharEnumerableAsTextReader(IEnumerable<char> chars)
            {
                charsEnumerator = chars.GetEnumerator();
                MoveNext();
            }

            void MoveNext()
            {
                current = charsEnumerator.MoveNext() ? charsEnumerator.Current : -1;
            }

            public override int Peek()
            {
                return current;
            }

            public override int Read()
            {
                var previous = current;
                
                MoveNext();
                return previous;
            }
        }


        public class SomeClassWithValue<T>
        {
            public T Value { get; set; }
        }

        public class SomeClassWithOptionalPropertiesAndFields
        {
            public double? X { get; set; }

            public double? Y { get; set; }
            public bool YIsSpecified { get; set; }

            public double? Z;
            public bool ZIsSpecified;            

            public Optional<double?> W;            
        }

        public class SomeClassWithCustomOptionalValue
        {
            public double? X;
            public bool IncludeInOutputX;
        }

        public class SomeClassWithInvalidIsSpecifiedMemberType
        {
            public double? X;
            public int XIsSpecified;
        }

        public class SomeClassWithIndexer
        {
            int x;
            bool xIsSpecified;

            public int X
            {
                get { return x; }
                set { x = value; }
            }

            public bool this[int i]
            {
                get { return xIsSpecified; }
                set { xIsSpecified = value; }
            }
        }

        public enum SomeEnum
        {
            Value1, Value2, Value3
        }

        public struct SomeStruct
        {
        }        

        public struct SomeStructWithTryParse1
        {
            public int IntValue;

            public static bool TryParse(string s, IFormatProvider formatProvider, out SomeStructWithTryParse1 value)
            {
                Assert.Equal(CultureInfo.InvariantCulture, formatProvider);

                return int.TryParse(s, out value.IntValue);
            }
        }

        public struct SomeStructWithTryParse2
        {
            public int Value;

            public static bool TryParse(string s, out SomeStructWithTryParse2 value)
            {
                return int.TryParse(s, out value.Value);
            }
        }

        public struct SomeStructWithParse1
        {
            public int Value;

            public static SomeStructWithParse1 Parse(string s, IFormatProvider formatProvider)
            {
                Assert.Equal(CultureInfo.InvariantCulture, formatProvider);

                return new SomeStructWithParse1 { Value = int.Parse(s) };
            }
        }

        public struct SomeStructWithParse2
        {
            public int Value;

            public static SomeStructWithParse2 Parse(string s)
            {
                return new SomeStructWithParse2 { Value = int.Parse(s) };
            }
        }
        
        public abstract class SomeAbstractClass
        {             
        }

        public class SomeClassWithoutPublicConstructor
        {
            protected SomeClassWithoutPublicConstructor()
            {                
            }
        }

        public class SomeImmutableClass
        {
            public SomeImmutableClass(int x, int y)
            {
                X = x;
                Y = y;
            }

            public readonly int X;
            public readonly int Y;
        }

        public class SomeClassWithRefParametersInConstructor : SomeImmutableClass
        {
            public SomeClassWithRefParametersInConstructor(int x, ref int y)
                : base(x, y)
            {
            }     
        }

        public static class Methods
        {
            public static TTo ConvertTo<TFrom, TTo>(TFrom value)
            {
                return default(TTo);
            }

            public static T ConvertFromUntyped<T>(object value)
            {
                return default(T);
            }

            public static SomeStruct? GetSomeStructFromStringValue(object value)
            {
                var parsedValue = new SomeStruct();

                if ((string)value == parsedValue.ToString())
                    return parsedValue;

                throw new FormatException();
            }
        }
    }
}

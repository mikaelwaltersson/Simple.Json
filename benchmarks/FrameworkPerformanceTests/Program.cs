using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FrameworkPerformanceTests.TypedJsonObjects;

namespace FrameworkPerformanceTests
{
    class Program
    {
        const int N = 1000000;
        const string InputFile = "sample1.json";

        static void Main()
        {
            Simple.Json.Serialization.TypeSerializer.Default.Configuration.GetObjectPropertyNameDelegate = name => name;

            Benchmark("ServiceStack.Text", ServiceStackTextSerialize, ServiceStackTextDeserialize);
            Benchmark("Json.NET", JsonNetSerialize, JsonNetDeserialize);
            Benchmark("Simple.Json", SimpleJsonSerialize, SimpleJsonDeserialize);            
        }


        static void Benchmark(string name, Func<ImageRequest, string> serialize, Func<string, ImageRequest> deserialize)
        {
            var content = File.ReadAllText(InputFile);

            if (!SimpleJsonDeserialize(content).Equals(deserialize(content)))
                Console.WriteLine("{0} - Warning: deserialize seems to give a different result than Simple.Json", name);

            if (!SimpleJsonDeserialize(content).Equals(SimpleJsonDeserialize(serialize(deserialize(content)))))
                Console.WriteLine("{0} - Warning: roundstrip serialization give a different result", name);


            var obj = default(ImageRequest);

            var t0 = DateTime.UtcNow;
      
            for (var i = 0; i < N; i++)
                obj = deserialize(content);

            var t1 = DateTime.UtcNow - t0;

            for (var i = 0; i < N; i++)
                serialize(obj);

            var t2 = DateTime.UtcNow - t0 - t1;            

            Console.WriteLine("{0} - deserialize: {1:0.000s}", name, t1.TotalSeconds);
            Console.WriteLine("{0} - serialize: {1:0.000s}", name, t2.TotalSeconds);
        }



        static string ServiceStackTextSerialize(ImageRequest obj)
        {
            return ServiceStack.Text.JsonSerializer.SerializeToString(obj);
        }

        static ImageRequest ServiceStackTextDeserialize(string s)
        {
            return ServiceStack.Text.JsonSerializer.DeserializeFromString<ImageRequest>(s);
        }

        static string JsonNetSerialize(ImageRequest obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        static ImageRequest JsonNetDeserialize(string s)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ImageRequest>(s);
        }

        static string SimpleJsonSerialize(ImageRequest obj)
        {
            return Simple.Json.JsonSerializer.Default.ToJson(obj, typeof(ImageRequest), false);
        }

        static ImageRequest SimpleJsonDeserialize(string s)
        {
            return (ImageRequest)Simple.Json.JsonSerializer.Default.ParseJson(s, typeof(ImageRequest));
        }


    }
}

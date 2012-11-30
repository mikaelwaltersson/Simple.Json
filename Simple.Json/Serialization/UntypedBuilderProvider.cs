using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    public class UntypedBuilderProvider : IBuilderProvider
    {
        public static readonly UntypedBuilderProvider Default = new UntypedBuilderProvider();

        public IObjectBuilder GetObjectBuilder()
        {
            return new JsonObject();
        }

        public IBuilderProvider GetObjectValueBuilderProvider(string key)
        {
            return this;
        }

        public IArrayBuilder GetArrayBuilder()
        {
            return new JsonArray();
        }

        public IBuilderProvider GetArrayElementBuilderProvider()
        {
            return this;
        }
    }
}
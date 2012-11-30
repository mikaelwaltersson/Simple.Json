using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Simple.Json.Tests
{
    public class JsonArrayTests
    {
        [Fact]
        public void ToStringSerializesToJson()
        {
            Assert.Equal("[1,\"One\",\"Ett\",true,null]", new JsonArray { 1, "One", "Ett", true, null }.ToString());
        }
    }
}

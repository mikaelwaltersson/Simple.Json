using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Simple.Json.Tests
{    
    public class OptionalTests
    {
        [Fact]
        public void CanGetValueFromSpecified()
        {
            Optional<int> specified = 23;

            Assert.Equal(23, specified.Value);
            Assert.Equal(23, (int)specified);
        }

        [Fact]
        public void CanNotGetValueFromUnspecified()
        {
            Optional<int> unspecified = Undefined.Value;

            Assert.Throws<InvalidOperationException>(() => unspecified.Value);
            Assert.Throws<InvalidOperationException>(() => (int)unspecified);
        }

        [Fact]
        public void SpecifiedValuesEqualsOtherSpecifiedValues()
        {
            var specified = new Optional<string>("hello world");

            Assert.True(specified != Undefined.Value);
            Assert.False(specified.Equals(Undefined.Value));
            Assert.False(Equals(specified, Undefined.Value));
            Assert.False(Undefined.Value.Equals(specified));
            Assert.False(Equals(specified, new Optional<string>()));

            Assert.True(specified == "hello world");
            Assert.False(specified != "hello world");
        }

        [Fact]
        public void UnspecifiedValuesEqualsUndefined()
        {
            var unspecified = new Optional<string>();
           
            Assert.True(unspecified == Undefined.Value);
            Assert.True(unspecified.Equals(Undefined.Value));
            Assert.True(Equals(unspecified, Undefined.Value));
            Assert.True(Undefined.Value.Equals(unspecified));
            Assert.True(Equals(unspecified, new Optional<string>()));

            Assert.False(unspecified == "hello world");
            Assert.True(unspecified != "hello world");
        }

        [Fact]
        public void GetHashCodeReturnsZeroOrValuesHashCode()
        {
            Assert.Equal(0, new Optional<HashCode>().GetHashCode());
            Assert.Equal(3, new Optional<HashCode>(new HashCode(2)).GetHashCode());
        }


        

        struct HashCode
        {
            readonly int value;

            public HashCode(int value)
            {
                this.value = value;
            }

            public override int GetHashCode()
            {
                return 1 + value;
            }
        }
    }
}

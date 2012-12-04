using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Simple.Json.Tests
{
    public class JsonObjectTests
    {
        [Fact]
        public void SetFieldsAddsKeyValuePairs()
        {
            dynamic person = new JsonObject();

            person.FirstName = "Mikael";
            person.LastName = "Waltersson";
            person.Age = 30;
            
            
            Assert.Equal(new[] { "FirstName", "LastName", "Age" }, person.GetDynamicMemberNames());
            Assert.Equal(
                new Dictionary<string, object>
                {
                    { "FirstName", "Mikael" },
                    { "LastName", "Waltersson" },
                    { "Age", 30 }
                },
                (IEnumerable<KeyValuePair<string, object>>)person);

            

            Assert.Equal("Mikael", person.FirstName);
            Assert.Equal("Waltersson", person.LastName);
            Assert.Equal(30, person.Age);            
        }

        [Fact]
        public void NonExistingFieldsHasUndefinedValue()
        {
            dynamic person = new JsonObject();

            person.FirstName = "Mikael";

            Assert.Equal(Undefined.Value, person.LastName);
        }

        [Fact]
        public void SettingUndefinedAsFieldValueRemovesField()
        {
            dynamic person = new JsonObject();

            person.FirstName = "Mikael";
            person.LastName = "Waltersson";

            person.FirstName = Undefined.Value;

            Assert.Equal(new[] { "LastName" }, person.GetPropertyNames());
        }

        [Fact]
        public void PropertyOrderIsSetByFirstSetFieldInvocation()
        {
            dynamic person = new JsonObject();

            person.LastName = "";
            person.FirstName = "Mikael";
            person.LastName = "Waltersson";

            Assert.Equal(
                new Dictionary<string, object>
                {
                    { "LastName", "Waltersson" },
                    { "FirstName", "Mikael" }                 
                },
                (IEnumerable<KeyValuePair<string, object>>)person);
        }

        [Fact]
        public void CantInitializePropertyWithUndefinedValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonObject { { "FirstName", Undefined.Value } });
        }

        [Fact]
        public void ToStringSerializesToJson()
        {
            Assert.Equal("{\"One\":1,\"Two\":2}", new JsonObject { { "One", 1 }, { "Two", 2 } }.ToString());
        }

        [Fact]
        public void JsonObjectIsDictionaryOfStringObjectPair()
        {
            var dictionary = (IDictionary<string, object>)new JsonObject();

            dictionary.Add(new KeyValuePair<string, object>("a", 1.0));
            dictionary.Add(new KeyValuePair<string, object>("b", 2.0));

            Assert.Equal(2, dictionary.Count);
            Assert.False(dictionary.IsReadOnly);

            object a;
            Assert.True(dictionary.TryGetValue("a", out a));
            Assert.Equal(1.0, a);


            Assert.Equal(new[] { "a", "b" }, dictionary.Keys.OrderBy(key => key));
            Assert.Equal(new object[] { 1.0, 2.0 }, dictionary.Values.OrderBy(value => value));


            var entries = new KeyValuePair<string, object>[2];
            dictionary.CopyTo(entries, 0);
            Array.Sort(entries, (x, y) => string.CompareOrdinal(x.Key, y.Key));

            Assert.Equal(new[] { new KeyValuePair<string, object>("a", 1.0), new KeyValuePair<string, object>("b", 2.0) }, entries);


            Assert.True(dictionary.ContainsKey("a"));
            Assert.True(dictionary.Contains(new KeyValuePair<string, object>("a", 1.0)));
            Assert.False(dictionary.Contains(new KeyValuePair<string, object>("a", 2.0)));

            Assert.False(dictionary.Remove(new KeyValuePair<string, object>("a", 2.0)));
            Assert.True(dictionary.Remove(new KeyValuePair<string, object>("a", 1.0)));

            Assert.Equal(1, dictionary.Count);

            dictionary.Clear();

            Assert.Equal(0, dictionary.Count);
        }
    }
}
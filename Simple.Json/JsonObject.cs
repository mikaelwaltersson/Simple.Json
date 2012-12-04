using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Simple.Json.Serialization;

namespace Simple.Json
{
    public class JsonObject : DynamicObject, IDictionary<string, object>, IObjectBuilder
    {
        List<string> names = new List<string>();
        Dictionary<string, object> values = new Dictionary<string, object>();

        public object this[string name]
        {
            get { return values[name]; }
            set
            {
                if (value is Undefined)
                    Remove(name);
                else if (IsDefined(name))
                    values[name] = value;
                else
                    Add(name, value);
            }
        }

        public bool IsDefined(string name)
        {
            return values.ContainsKey(name);
        }


        public void Add(string name, object value)
        {
            if (value is Undefined)
                throw new ArgumentOutOfRangeException("value", value, "Invalid value");

            values.Add(name, value);
            names.Add(name);
        }


        public bool Remove(string name)
        {
            values.Remove(name);            
            return names.Remove(name);
        }


        public void Clear()
        {
            names.Clear();
            values.Clear();            
        }



        public IEnumerable<string> GetPropertyNames()
        {
            return names.AsEnumerable();
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return GetPropertyNames();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!values.TryGetValue(binder.Name, out result))
                result = Undefined.Value;

            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {                            
            this[binder.Name] = value;
            return true;
        }

        public override string ToString()
        {
            return JsonSerializer.Default.ToJson(this);
        }

        object IObjectBuilder.End()
        {
            foreach (var name in names)
            {
                object value;
                values.TryGetValue(name, out value);
            }

            return this;
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return names.Select(name => new KeyValuePair<string, object>(name, values[name])).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
        }

        ICollection<string> IDictionary<string, object>.Keys
        {
            get { return values.Keys; }
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get { return values.Values; }
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            return values.ContainsKey(key);
        }


        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            return values.TryGetValue(key, out value);
        }


        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return values.Contains(item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)values).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            var itemRemoved = ((ICollection<KeyValuePair<string, object>>)values).Remove(item);

            if (itemRemoved)            
                names.Remove(item.Key);

            return itemRemoved;
        }

        int ICollection<KeyValuePair<string, object>>.Count
        {
            get { return values.Count; }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }
    }
}
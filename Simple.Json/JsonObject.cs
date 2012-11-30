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
    public class JsonObject : DynamicObject, IEnumerable<KeyValuePair<string, object>>, IObjectBuilder
    {
        List<string> names = new List<string>();
        Dictionary<string, object> values = new Dictionary<string, object>();

        public object this[string name]
        {
            get { return values[name]; }
            set
            {
                if (value is Undefined)
                {
                    values.Remove(name);
                    names.Remove(name);
                    
                }
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

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return names.Select(name => new KeyValuePair<string, object>(name, values[name])).GetEnumerator();
        }

        public IEnumerable<string> GetPropertyNames()
        {
            return names.AsEnumerable();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
    }
}
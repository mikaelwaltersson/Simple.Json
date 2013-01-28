using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    public class UntypedDeconstructor : IDeconstructor
    {
        public void Deconstruct(object value, IJsonOutput output)
        {
            if (value == null)            
                output.Null();
            else if (value is bool)
                output.Boolean((bool)value);
            else if (value is double)
                output.Number((double)value);
            else if (value is string)
                output.String((string)value);
            else if (!OutputObjectOrArray(value, output))
                output.String(value.ToString()); 
        }

        protected virtual bool OutputObjectOrArray(object value, IJsonOutput output)
        {
            if (value is IEnumerable<KeyValuePair<string, object>>)
            {
                output.BeginObject();

                foreach (var keyValuePair in (IEnumerable<KeyValuePair<string, object>>)value)
                {
                    if (keyValuePair.Value is Undefined)
                        continue;

                    output.NamedProperty(keyValuePair.Key);

                    Deconstruct(keyValuePair.Value, output);
                }

                output.EndObject();
                return true;
            }

            if (value is IEnumerable)
            {
                output.BeginArray();

                foreach (var item in (IEnumerable)value)
                {
                    if (item is Undefined)
                        continue;

                    Deconstruct(item, output);
                }

                output.EndArray();
                return true;
            }

            if (value.GetType() == typeof(object))
            {
                output.BeginObject();
                output.EndObject();
                return true;
            }
            
            return false;
        }



  
    }
}
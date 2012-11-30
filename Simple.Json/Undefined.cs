using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json
{
    public class Undefined : IOptional
    {
        public static readonly Undefined Value = new Undefined();

        Undefined()
        {            
        }


        public override bool Equals(object obj)
        {
            return obj is IOptional && !((IOptional)obj).IsSpecified;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        bool IOptional.IsSpecified
        {
            get { return false; }
        }
    }
}

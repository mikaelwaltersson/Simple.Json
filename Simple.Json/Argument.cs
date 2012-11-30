using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json
{
    static class Argument
    {
        public static T NotNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(paramName);

            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    public abstract class InstanceCountConstrained<T> where T : class
    {
        public const int DefaultMaxInstanceCount = 16;

        static int maxInstanceCount = DefaultMaxInstanceCount;
        static int currentInstanceCount;

        protected InstanceCountConstrained()
        {
            if (Interlocked.Increment(ref currentInstanceCount) > maxInstanceCount)
            {
                Interlocked.Decrement(ref currentInstanceCount);

                throw new InvalidOperationException(
                    string.Format(
                        "The type {0} is supposed to be used in a singleton like manner and has a constraint on max number of instance allocations which has now been exceeded." +
                        "Reuse instances instead of allocating new ones or set InstanceCountConstrained<{1}>.MaxInstanceCount to a higher value if you need more distinct instances.", 
                        typeof(T).FullName, typeof(T).Name));
            }
        }

        public static int MaxInstanceCount
        {
            set { Interlocked.Exchange(ref maxInstanceCount, value); }
        }
    }
}

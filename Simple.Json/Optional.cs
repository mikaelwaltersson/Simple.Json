using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json
{
    public struct Optional<T> : IEquatable<T>, IEquatable<Optional<T>>, IOptional
    {
        readonly T value;
        readonly bool isSpecified;

        public Optional(T value)
        {
            this.value = value;
            isSpecified = true;
        }

        public T Value
        {
            get
            {
                if (!isSpecified)
                    throw new InvalidOperationException("Optional object must have a value");

                return value;
            }
        }

        public bool IsSpecified
        {
            get { return isSpecified; }
        }

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }

        public static implicit operator Optional<T>(Undefined value)
        {
            return default(Optional<T>);
        }

        public static explicit operator T(Optional<T> value)
        {
            return value.Value;
        }

        public static bool operator ==(Optional<T> x, T y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Optional<T> x, T y)
        {
            return !(x == y);
        }

        public static bool operator ==(Optional<T> x, Undefined y)
        {
            return !x.IsSpecified;
        }

        public static bool operator !=(Optional<T> x, Undefined y)
        {
            return !(x == y);
        }

        public bool Equals(T other)
        {
            return isSpecified && EqualityComparer<T>.Default.Equals(value, other);
        }

        public bool Equals(Optional<T> other)
        {
            return 
                other.IsSpecified 
                ? Equals(other.value) 
                : !isSpecified;
        }

        public override bool Equals(object obj)
        {
            return 
                isSpecified
                ? Equals(obj, value)
                : Undefined.Value.Equals(obj);
        }
      
        public override int GetHashCode()
        {
            return 
                isSpecified 
                ? value.GetHashCode() 
                : Undefined.Value.GetHashCode();
        }
    }
}

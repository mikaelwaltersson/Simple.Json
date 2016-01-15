using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Annotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DiscriminatorAttribute : Attribute
    {
    }
}

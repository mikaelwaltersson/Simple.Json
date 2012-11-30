using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace Simple.Json.Serialization
{
    public interface ITypeSerializer
    {
        IBuilderProvider GetBuilderProvider(Type type);
        IDeconstructor GetDeconstructor(Type type);

        ITypeSerializerConfiguration Configuration { get; }

        
    }
}
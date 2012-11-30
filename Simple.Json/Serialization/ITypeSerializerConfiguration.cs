using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    public interface ITypeSerializerConfiguration
    {
        void RegisterConvertFromJsonValueMethod<TTo>(Func<object, TTo> method);
        void RegisterConvertToJsonValueMethod<TFrom, TTo>(Func<TFrom, TTo> method);

        Func<string, string> GetObjectPropertyNameDelegate { set; }
        Func<string, string> GetIsSpecifiedMemberDelegate { set; }
        
        bool SkipOutputOfDefaultValues { set; }
        bool EnableMandatoryFieldsValidation { set; }
    }
}
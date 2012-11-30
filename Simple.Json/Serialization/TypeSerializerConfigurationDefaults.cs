using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Serialization
{
    public static class TypeSerializerConfigurationDefaults
    {
        public static string NameToCamelCase(string name)
        {
            Argument.NotNull(name, "name");

            var firstWord = GetFirstWord(name);

            return firstWord.ToLower() + name.Substring(firstWord.Length);
        }

        public static string GetIsSpecifiedMember(string name)
        {
            return name + "IsSpecified";
        }

        static string GetFirstWord(string name)
        {
            var i = 1;

            for (; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                    break;
            }

            for (; i < name.Length; i++)
            {
                if (char.IsLower(name[i]))
                    return name.Substring(0, Math.Max(0, i - 1));
            }

            return name;
        }


        public static sbyte? CastNumberToSByte(object value)
        {
            return (sbyte?)(double?)value;
        }

        public static byte? CastNumberToByte(object value)
        {
            return (byte?)(double?)value;
        }

        public static short? CastNumberToInt16(object value)
        {
            return (short?)(double?)value;
        }

        public static ushort? CastNumberToUInt16(object value)
        {
            return (ushort?)(double?)value;
        }

        public static int? CastNumberToInt32(object value)
        {
            return (int?)(double?)value;
        }

        public static uint? CastNumberToUInt32(object value)
        {
            return (uint?)(double?)value;
        }

        public static long? CastNumberToInt64(object value)
        {
            return (long?)(double?)value;
        }

        public static ulong? CastNumberToUInt64(object value)
        {
            return (ulong?)(double?)value;
        }

        public static float? CastNumberToSingle(object value)
        {
            return (float?)(double?)value;
        }

        public static decimal? CastNumberToDecimal(object value)
        {
            return (decimal?)(double?)value;
        }

        public static DateTime? ConvertStringToDateTime(object value)
        {
            return value != null ? (DateTime?)Iso8601.ToDateTime((string)value) : null;
        }

        public static TimeSpan? ConvertStringToTimeSpan(object value)
        {
            return value != null ? (TimeSpan?)Iso8601.ToTimeSpan((string)value) : null;
        }

        public static byte[] ConvertStringToByteArray(object value)
        {
            return value != null ? Convert.FromBase64String((string)value) : null;
        }



        public static double CastSByteToNumber(sbyte value)
        {
            return value;
        }

        public static double CastByteToNumber(byte value)
        {
            return value;
        }

        public static double CastInt16ToNumber(short value)
        {
            return value;
        }

        public static double CastUInt16ToNumber(ushort value)
        {
            return value;
        }

        public static double CastInt32ToNumber(int value)
        {
            return value;
        }

        public static double CastUInt32ToNumber(uint value)
        {
            return value;
        }

        public static double CastInt64ToNumber(long value)
        {
            return value;
        }

        public static double CastUInt64ToNumber(ulong value)
        {
            return value;
        }

        public static double CastSingleToNumber(float value)
        {
            return value;
        }

        public static double CastDecimalToNumber(decimal value)
        {
            return (double)value;
        }

        public static string ConvertDateTimeToString(DateTime value)
        {
            return Iso8601.ToString(value);
        }

        public static string ConvertTimeSpanToString(TimeSpan value)
        {
            return Iso8601.ToString(value);
        }

        public static string ConvertByteArrayToString(byte[] value)
        {
            return Convert.ToBase64String(value);
        }

    }
}
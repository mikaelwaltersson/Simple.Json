using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

using Simple.Json.Serialization;

namespace Simple.Json.Parsers
{
    class JsonParser : BasicParser
    {
        public JsonParser(string input)
            : base(input)
        {
        }

        public object Parse(Func<IBuilderProvider> getBuilderProvider)
        {
            Whitespace();

            var result = default(object);
            var success = ParseJsonArray(getBuilderProvider, ref result) || ParseJsonObject(getBuilderProvider, ref result);

            if (!success)
                Fail("expected valid json array or json object");

            End();

            return result;
        }

        bool ParseJsonValue(Func<IBuilderProvider> getBuilderProvider, ref object result)
        {
            return
                ParseJsonArray(getBuilderProvider, ref result) ||
                ParseJsonObject(getBuilderProvider, ref result) ||
                ParseJsonString(ref result) ||
                ParseJsonNumber(ref result) ||
                ParseJsonLiteral(ref result);
        }

        bool ParseJsonArray(Func<IBuilderProvider> getBuilderProvider, ref object result)
        {
            if (!TryToken('['))
                return false;

            var builderProvider = getBuilderProvider();
            var arrayBuilder = builderProvider.GetArrayBuilder();
            var getElementBuilderProvider = LazyResolvedBuilderProvider(builderProvider.GetArrayElementBuilderProvider);

            Whitespace();

            var firstElement = default(object);
            if (ParseJsonValue(getElementBuilderProvider, ref firstElement))
            {
                arrayBuilder.Add(firstElement);

                Whitespace();
                while (TryToken(','))
                {
                    Whitespace();

                    var nextElement = default(object);
                    if (!ParseJsonValue(getElementBuilderProvider, ref nextElement))
                        Fail("expected valid json value");

                    arrayBuilder.Add(nextElement);

                    Whitespace();
                }

            }

            Token(']');
            Whitespace();

            result = arrayBuilder.End();
            return true;
        }

        Func<IBuilderProvider> LazyResolvedBuilderProvider(Func<IBuilderProvider> getBuilderProvider)
        {
            var value = default(IBuilderProvider);

            return () => value ?? (value = getBuilderProvider());
        }

        bool ParseJsonObject(Func<IBuilderProvider> getBuilderProvider, ref object result)
        {
            if (!TryToken('{'))
                return false;

            var builderProvider = getBuilderProvider();
            var objectBuilder = builderProvider.GetObjectBuilder();

            Whitespace();

            KeyValuePair<string, object> firstPair;
            if (ParseKeyValuePair(builderProvider, out firstPair))
            {
                objectBuilder.Add(firstPair.Key, firstPair.Value);

                Whitespace();
                while (TryToken(','))
                {
                    Whitespace();

                    KeyValuePair<string, object> nextPair;
                    if (!ParseKeyValuePair(builderProvider, out nextPair))
                        Fail("expected valid json key value pair");

                    objectBuilder.Add(nextPair.Key, nextPair.Value);

                    Whitespace();
                }

            }

            Token('}');
            Whitespace();

            result = objectBuilder.End();
            return true;
        }

        bool ParseJsonString(ref object result)
        {
            string textString;
            if (!ParseTextString(out textString))
                return false;

            result = textString;
            return true;
        }

        bool ParseJsonNumber(ref object result)
        {
            var isNegative = TryToken('-');

            int digit;
            if (isNegative)
                digit = Digit();
            else if (!TryDigit(out digit))
                return false;

            var number = 0.0;

            if (digit != 0)
            {
                do
                {
                    number = number * 10 + digit;
                }
                while (TryDigit(out digit));
            }

            if (TryToken('.'))
            {
                var decimalPart = 0.0;
                var scale = 1.0;

                digit = Digit();
                do
                {
                    decimalPart = decimalPart * 10 + digit;
                    scale *= 10;
                }
                while (TryDigit(out digit));

                number += decimalPart / scale;
            }

            if (isNegative)
                number = -number;

            if (TryToken('e') || TryToken('E'))
            {
                var exponent = 0.0;
                var isNegativeExponent = TryToken('-');

                if (!isNegativeExponent)
                    TryToken('+');

                digit = Digit();
                do
                {
                    exponent = exponent * 10 + digit;
                }
                while (TryDigit(out digit));

                if (isNegativeExponent)
                    exponent = -exponent;

                number *= Math.Pow(10.0, exponent);
            }


            Whitespace();

            result = number;
            return true;
        }

        bool ParseJsonLiteral(ref object result)
        {
            Whitespace();

            return
                ParseLiteral("null", null, ref result) ||
                ParseLiteral("false", false, ref result) ||
                ParseLiteral("true", true, ref result);
        }

        bool ParseKeyValuePair(IBuilderProvider builderProvider, out KeyValuePair<string, object> result)
        {
            string key;
            if (!ParseTextString(out key))
            {
                result = default(KeyValuePair<string, object>);
                return false;
            }

            Token(':');
            Whitespace();

            var value = default(object);
            if (!ParseJsonValue(() => builderProvider.GetObjectValueBuilderProvider(key), ref value))
                Fail("expected valid json value");

            result = new KeyValuePair<string, object>(key, value);
            return true;
        }


        bool ParseLiteral(string literalText, object literalValue, ref object result)
        {
            if (!TryToken(literalText[0]))
                return false;

            for (var i = 1; i < literalText.Length; i++)
                Token(literalText[i]);

            Whitespace();

            result = literalValue;
            return true;
        }

        bool ParseTextString(out string result)
        {
            if (!TryToken('"'))
            {
                result = null;
                return false;
            }

            var builder = new StringBuilder();

            char token;
            while ((token = Token()) != '"')
            {
                if (token == '\\')
                    token = ParseEscapedCharacter();

                builder.Append(token);
            }

            Whitespace();

            result = builder.ToString();
            return true;
        }

        char ParseEscapedCharacter()
        {
            if (TryToken('"'))
                return '"';

            if (TryToken('\\'))
                return '\\';

            if (TryToken('/'))
                return '/';

            if (TryToken('b'))
                return '\b';

            if (TryToken('f'))
                return '\f';

            if (TryToken('n'))
                return '\n';

            if (TryToken('r'))
                return '\r';

            if (TryToken('t'))
                return '\t';

            Token('u');

            return (char)((HexDigit() << 12) | (HexDigit() << 8) | (HexDigit() << 4) | HexDigit());
        }
    }
}
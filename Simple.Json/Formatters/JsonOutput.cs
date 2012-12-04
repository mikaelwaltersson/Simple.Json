using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Simple.Json.Serialization;

namespace Simple.Json.Formatters
{
    class JsonOutput : IJsonOutput
    {
        static readonly IFormatProvider formatProvider = CultureInfo.InvariantCulture;
        static readonly string newLine = "\r\n";
        static readonly string indentFill = "\t";

        TextWriter writer;
        bool formatted;
        int maxDepth;

        int indentLevel;
        bool nextValueIsSubsequentItem;
        bool nextValueStartsOnNewLine;
        bool isAtNewLine;


        public JsonOutput(TextWriter writer, bool formatted, int maxDepth)
        {
            this.writer = writer;
            this.formatted = formatted;
            this.maxDepth = maxDepth;
        }

        public void Null()
        {
            AssertIsNotAtRootLevel();
            NextItem();
            Write("null");
        }

        public void Boolean(bool value)
        {
            AssertIsNotAtRootLevel();
            NextItem();
            Write(value ? "true" : "false");
        }

        public void Number(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                value = 0;
            
            AssertIsNotAtRootLevel();
            NextItem();
            Write(value == (long)value ? ((long)value).ToString(formatProvider) : value.ToString(formatProvider));
        }

        public void String(string value)
        {
            AssertIsNotAtRootLevel();
            NextItem();
            Write(ToJsonString(value));
        }

        public void BeginObject()
        {
            NextItem();
            BeginItemCollection("{");
        }

        public void NamedProperty(string name)
        {
            AssertIsNotAtRootLevel();
            NextItem();
            Write(ToJsonString(name));
            WriteSeperator(":");

            nextValueIsSubsequentItem = false;
            nextValueStartsOnNewLine = false;
        }

        public void EndObject()
        {
            EndItemCollection("}");
        }

        public void BeginArray()
        {
            NextItem();
            BeginItemCollection("[");
        }

        public void EndArray()
        {
            EndItemCollection("]");
        }

        void BeginItemCollection(string token)
        {
            AssertIsNotAtMaxGraphDepth();
            Write(token);

            indentLevel++;
            nextValueIsSubsequentItem = false;
        }

        void EndItemCollection(string token)
        {
            if (nextValueIsSubsequentItem)
                NewLine();

            indentLevel--;
            nextValueIsSubsequentItem = true;

            Write(token);
        }

        void NewLine()
        {
            if (formatted)
                writer.Write(newLine);

            isAtNewLine = true;
        }

        void NextItem()
        {
            if (nextValueIsSubsequentItem)
                writer.Write(",");

            if (nextValueStartsOnNewLine)
                NewLine();

            nextValueIsSubsequentItem = true;
            nextValueStartsOnNewLine = true;
        }

        void WriteSeperator(string token)
        {
            writer.Write(formatted ? (' ' + token + ' ') : token);
        }

        void Write(string s)
        {
            if (isAtNewLine)
            {
                if (formatted)
                {
                    for (var i = 0; i < indentLevel; i++)
                        writer.Write(indentFill);
                }

                isAtNewLine = false;
            }

            writer.Write(s);
        }

        void AssertIsNotAtRootLevel()
        {
            if (indentLevel == 0)
                throw new InvalidOperationException("Root level must be object or array");
        }

        void AssertIsNotAtMaxGraphDepth()
        {
            if (indentLevel > maxDepth)
                throw new InvalidOperationException("Max depth for serializing graph exceeded");
        }

        static string ToJsonString(string s)
        {
            const string hexDigits = "0123456789ABCDEF";

            var jsonString = new StringBuilder(s.Length + 2);
            var hasEscapedCharacter = false;

            jsonString.Append('"');

            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];

                if (c <= 0x001F || c == '"' || c == '\\')
                {
                    if (!hasEscapedCharacter)
                    {
                        jsonString.Append(s, 0, i);
                        hasEscapedCharacter = true;
                    }

                    jsonString.Append('\\');

                    switch (c)
                    {
                        case '"':
                        case '\\':
                            jsonString.Append(c);
                            break;

                        case '\b':
                            jsonString.Append('b');
                            break;

                        case '\f':
                            jsonString.Append('f');
                            break;

                        case '\n':
                            jsonString.Append('n');
                            break;

                        case '\r':
                            jsonString.Append('r');
                            break;

                        case '\t':
                            jsonString.Append('t');
                            break;

                        default:
                            jsonString.Append('u');
                            jsonString.Append(hexDigits[(c >> 12) & 0xF]);
                            jsonString.Append(hexDigits[(c >> 8) & 0xF]);
                            jsonString.Append(hexDigits[(c >> 4) & 0xF]);
                            jsonString.Append(hexDigits[c & 0xF]);
                            break;
                    }
                }
                else if (hasEscapedCharacter)
                    jsonString.Append(c);

            }

            if (!hasEscapedCharacter)
                jsonString.Append(s);

            jsonString.Append('"');

            return jsonString.ToString();
        }

    
    }
}
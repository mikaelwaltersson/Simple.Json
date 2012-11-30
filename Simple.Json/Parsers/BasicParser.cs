using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Parsers
{
    abstract class BasicParser
    {
        string input;
        int position;

        protected BasicParser(string input)
        {
            this.input = Argument.NotNull(input, "input");
        }

        protected char Token()
        {
            if (position >= input.Length)
                Fail("unexpected end of input");

            return input[position++];
        }

        protected void Token(char validToken)
        {
            if (Token() != validToken)
                Fail("expected '" + validToken + "'");              
        }

        protected bool TryToken(char validToken)
        {
            if (Peek() != validToken)
                return false;

            position++;
            return true;
        }

        bool IsValidDigit(int token)
        {
            return token >= '0' && token <= '9';
        }

        int ToDigit(int token)
        {
            return token - '0';
        }

        protected bool TryDigit(out int digit)
        {
            var token = Peek();

            if (!IsValidDigit(token))
            {
                digit = 0;
                return false;
            }

            digit = ToDigit(Token());
            return true;                
        }

        protected int Digit()
        {
            var token = Token();

            if (!IsValidDigit(token))
                Fail("expected digit ('0' - '9')");

            return ToDigit(token);
        }

        protected int HexDigit()
        {
            var token = Token();

            if (!IsValidHexDigit(token))
                Fail("expected hexadecimal digit ('0' - '9' | 'A' - 'F' | 'a' - 'f')");

            return ToHexDigit(token);
        }

        protected int Integer()
        {
            var integer = 0;

            var digit = Digit();
            do
            {
                integer = integer*10 + digit;
            }
            while (TryDigit(out digit));

            return integer;
        }

        protected bool TryInteger(int numberOfDigits, out int integer)
        {
            if (!TryDigit(out integer))
                return false;

            for (var i = 1; i < numberOfDigits; i++)
                integer = integer * 10 + Digit();

            return true;
        }

        protected int Integer(int numberOfDigits)
        {
            return Integer(numberOfDigits, numberOfDigits, out numberOfDigits);
        }

        protected int Integer(int minNumberOfDigits, int maxNumberOfDigits, out int numberOfDigits)
        {
            var integer = 0;

            var i = 0;
            for (; i < minNumberOfDigits; i++)
                integer = integer * 10 + Digit();

            int digit;
            for (; i < maxNumberOfDigits && TryDigit(out digit); i++)
                integer = integer * 10 + digit;

            numberOfDigits = i;

            return integer;
        }



        protected double DecimalFraction()
        {
            var unscaledFraction = 0.0;
            var scale = 1.0;

            var digit = Digit();
            do
            {
                unscaledFraction = unscaledFraction * 10 + digit;
                scale *= 10;
            }
            while (TryDigit(out digit));

            return unscaledFraction / scale;
        }

        bool IsValidHexDigit(int token)
        {
            return IsValidDigit(token) || (token >= 'A' && token <= 'F') || (token >= 'a' && token <= 'f');
        }

        int ToHexDigit(int token)
        {
            return IsValidDigit(token) ? ToDigit(token) : 10 + (token <= 'F' ? token - 'A' : token - 'a');
        }

        protected bool TryEnd()
        {
            return Peek() < 0;
        }

        protected void End()
        {
            if (Peek() >= 0)
                Fail("expected end of input");
        }

        protected void Whitespace()
        {
            while (position < input.Length)
            {
                var token = input[position];
                switch (token)
                {
                    case '\n':
                    case ' ':
                    case '\t':
                    case '\r':                    
                        position++;
                        continue;
                }
                break;
            }
        }

        protected void Fail(string message)
        {
            var linePosition = 1;
            var charPosition = 1;

            for (var i = 0; i < position; i++)
            {
                if (input[i] == '\n')
                {
                    linePosition++;
                    charPosition = 0;
                }
                else
                    charPosition++;
            }

            throw new FormatException(string.Format("Line {0}, Char {1}: {2}", linePosition, charPosition, message ?? "unexpected input"));
        }

        int Peek()
        {
            return position < input.Length ? input[position] : -1;
        }
    }
}
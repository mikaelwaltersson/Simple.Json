using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Parsers
{
    class Iso8601TimeSpanParser : BasicParser
    {
        public Iso8601TimeSpanParser(string input)
            : base(input)
        {
        }

        public TimeSpan Parse()
        {
            var sign = TryToken('-') ? -1 : 1;

            Token('P');

            var ticks = 0L;

            double n;            
            bool hasDecimalFraction;

            if (!TryToken('T'))
            {
                n = Decimal(out hasDecimalFraction);

                if (TryToken('Y') || TryToken('M'))
                    throw new FormatException("Years and Month in time span is ambiguous and not permitted");
                
                if (TryToken('W'))
                {
                    ticks = (long)(n*TimeSpan.TicksPerDay*7);                    
                    return End(ticks*sign);
                }
                                
                Token('D');
                ticks = (long)(n*TimeSpan.TicksPerDay);

                if (hasDecimalFraction || TryEnd())
                    return End(ticks*sign);
                
                Token('T');
            }

            n = Decimal(out hasDecimalFraction);

            if (TryToken('H'))
            {
                ticks += (long)(n*TimeSpan.TicksPerHour);

                if (hasDecimalFraction || TryEnd())
                    return End(ticks*sign);

                n = Decimal(out hasDecimalFraction);
            }

            if (TryToken('M'))
            {
                ticks += (long)(n*TimeSpan.TicksPerMinute);

                if (hasDecimalFraction || TryEnd())
                    return End(ticks*sign);

                n = Decimal(out hasDecimalFraction);
            }

            Token('S');
            ticks += (long)(n*TimeSpan.TicksPerSecond);

            return End(ticks*sign);
        }

        TimeSpan End(long ticks)
        {
            End();
            return new TimeSpan(ticks);
        }

        double Decimal(out bool hasDecimalFraction)
        {
            var n = (double)Integer();
            
            hasDecimalFraction = TryToken(',') || TryToken('.');
            if (hasDecimalFraction)
                n += DecimalFraction();
            
            return n;
        }
    }
}
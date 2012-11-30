using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Parsers
{
    class Iso8601DateTimeParser : BasicParser
    {

        public Iso8601DateTimeParser(string input)
            : base(input)
        {
        }

        public DateTime Parse()
        {
            bool isExtendedFormat;

            var dateTime = ParseDatePart(out isExtendedFormat);

            if (TryToken('T'))
            {
                dateTime = ParseTimePart(dateTime, isExtendedFormat);

                if (TryToken('Z'))
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

                var isNegativeDifference = TryToken('-');
                if (isNegativeDifference || TryToken('+'))
                    dateTime = ParseTimeZoneDifferencePart(dateTime, isNegativeDifference, isExtendedFormat);
            }

            End();

            return dateTime;
        }

        DateTime ParseDatePart(out bool isExtendedFormat)
        {
            var year = Integer(4);

            isExtendedFormat = TryToken('-');
            var isWeekDate = TryToken('W');

            if (isWeekDate)
            {
                var week = Integer(2);

                if (isExtendedFormat)
                    Token('-');

                var weekDay = Digit();

                return Iso8601.WeekDate(year, week, weekDay);
            }

            int numberOfDigitsNextInteger;
            var nextInteger = Integer(2, (isExtendedFormat ? 3 : 4), out numberOfDigitsNextInteger);
            var isOrdinalDate = numberOfDigitsNextInteger == 3;

            if (isOrdinalDate)
            {
                var ordinalDateDay = nextInteger;

                return Iso8601.OrdinalDate(year, ordinalDateDay);
            }

            int month, day;
            if (isExtendedFormat)
            {
                Token('-');

                month = nextInteger;
                day = Integer(2);
            }
            else
            {
                month = nextInteger / 100;
                day = nextInteger % 100;
            }

            return new DateTime(year, month, day);

        }

        DateTime ParseTimePart(DateTime date, bool isExtendedFormat)
        {
            var hour = Integer(2);
            var minute = 0;
            var second = 0;
            var ticksPerFraction = TimeSpan.TicksPerHour;

            if ((isExtendedFormat && TryToken(':')) || TryInteger(2, out minute))
            {
                ticksPerFraction = TimeSpan.TicksPerMinute;

                if (isExtendedFormat)
                    minute = Integer(2);

                if ((isExtendedFormat && TryToken(':')) || TryInteger(2, out second))
                {
                    ticksPerFraction = TimeSpan.TicksPerSecond;

                    if (isExtendedFormat)
                        second = Integer(2);
                }
            }

            var fractionTicks =
                (TryToken(',') || TryToken('.'))
                    ? DecimalFraction()*ticksPerFraction
                    : 0.0;
  
            if (hour == 24 && minute == 0 && second == 0 && fractionTicks == 0.0)
            {
                hour = 0;
                fractionTicks = TimeSpan.TicksPerDay;
            }

            return new DateTime(date.Year, date.Month, date.Day, hour, minute, second).AddTicks((long)fractionTicks);
        }


        DateTime ParseTimeZoneDifferencePart(DateTime dateTime, bool isNegativeDifference, bool isExtendedFormat)
        {
            var sign = isNegativeDifference ? -1 : 1;
            var hours = Integer(2);
            var minutes = 0;

            if ((isExtendedFormat && TryToken(':')) || TryInteger(2, out minutes))
            {                
                if (isExtendedFormat)
                    minutes = Integer(2);
            }            

            return dateTime.AddTicks(-sign*(hours*TimeSpan.TicksPerHour + minutes*TimeSpan.TicksPerMinute)).ToLocalTime();
        }


    }
}

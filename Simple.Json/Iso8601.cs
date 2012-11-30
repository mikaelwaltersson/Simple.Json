using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Simple.Json.Parsers;

namespace Simple.Json
{
    public static class Iso8601
    {
        public static DateTime ToDateTime(string dateTime)
        {
            return new Iso8601DateTimeParser(dateTime).Parse();
        }

        public static TimeSpan ToTimeSpan(string duration)
        {
            return new Iso8601TimeSpanParser(duration).Parse();
        }

        public static string ToString(DateTime dateTime)
        {
            var output = new StringBuilder();
 
            OutputValue(dateTime.Year, 4, output);
            output.Append('-');
            OutputValue(dateTime.Month, 2, output);
            output.Append('-');
            OutputValue(dateTime.Day, 2, output);

            var timeOfDayTicks = dateTime.TimeOfDay.Ticks;

            if (timeOfDayTicks != 0)
            {
                output.Append('T');

                
                OutputValue((int)(timeOfDayTicks/TimeSpan.TicksPerHour), 2, output);
                timeOfDayTicks %= TimeSpan.TicksPerHour;

                output.Append(':');

                OutputValue((int)(timeOfDayTicks/TimeSpan.TicksPerMinute), 2, output);
                timeOfDayTicks %= TimeSpan.TicksPerMinute;
                
                if (timeOfDayTicks != 0)
                {
                    output.Append(':');

                    OutputValueWithFractions((int)timeOfDayTicks, (int)TimeSpan.TicksPerSecond, 2, output);               
                }
            }

            switch (dateTime.Kind)
            {
                case DateTimeKind.Local:
                    {
                        var timeZoneDifference = dateTime - dateTime.ToUniversalTime();
                        var timeZoneDifferenceAbsolute = timeZoneDifference.Duration();

                        output.Append("+-"[(int)((ulong)timeZoneDifference.Ticks >> 63)]);
                        OutputValue(timeZoneDifferenceAbsolute.Hours, 2, output);
                        output.Append(':');
                        OutputValue(timeZoneDifferenceAbsolute.Minutes, 2, output);
                    }
                    break;

                case DateTimeKind.Utc:
                    output.Append('Z');
                    break;
            }

            return output.ToString();
        }

        public static string ToString(TimeSpan duration)
        {
            var output = new StringBuilder();

            if (duration.Ticks < 0)
            {
                duration = new TimeSpan(-duration.Ticks);
                output.Append('-');
            }

            output.Append('P');
            OutputValueIfNotZero(duration.Days, 'D', output);
            if (duration.TotalDays != Math.Truncate(duration.TotalDays))
            {
                var hours = duration.Hours;
                var minutes = duration.Minutes;
                var secondsWithFractions = (int)(duration.Ticks%TimeSpan.TicksPerMinute);

                output.Append('T');
                OutputValueIfNotZero(hours, 'H', output);
                OutputValueIfNotZero(minutes, 'M', output);
                OutputValueWithFractionsIfNotZero(secondsWithFractions, (int)TimeSpan.TicksPerSecond, 'S', output);
            }

            return output.ToString();
        }

        public static DateTime OrdinalDate(int year, int day)
        {
            var ticks = new DateTime(year, 1, 1).Ticks + (day - 1) * TimeSpan.TicksPerDay;

            if (day < 1 || day > (DateTime.IsLeapYear(year) ? 366 : 365))
                throw new ArgumentOutOfRangeException(null, "Year and Day parameters describe an un-representable DateTime");

            return new DateTime(ticks);
        }

        public static DateTime WeekDate(int year, int week, int day)
        {
            var ticks = new DateTime(year, 1, 1).Ticks + (week*7 + day - (DayOfWeek(new DateTime(year, 1, 4)) + 3) - 1)*TimeSpan.TicksPerDay;

            if (week < 1 || week > NumberOfWeeks(year) || day < 1 || day > 7)
                throw new ArgumentOutOfRangeException(null, "Year, Week and Day parameters describe an un-representable DateTime");            

            return new DateTime(ticks);
        }

        public static int NumberOfWeeks(int year)
        {
            var date = new DateTime(year, 12, 28);

            return (date.DayOfYear - DayOfWeek(date) + 10) / 7;
        }

        static void OutputValueWithFractionsIfNotZero(int value, int divisior, char suffix, StringBuilder output)
        {
            if (value != 0)
            {
                OutputValueWithFractions(value, divisior, GetOutputFieldLength(value/divisior), output);                
                output.Append(suffix);
            }
        }

        static void OutputValueIfNotZero(int value, char suffix, StringBuilder output)
        {
            if (value != 0)
            {
                OutputValue(value, output);
                output.Append(suffix);
            }
        }

        static void OutputValue(int value, StringBuilder output)
        {
            OutputValue(value, GetOutputFieldLength(value), output);
        }

        static void OutputValueWithFractions(int valueWithFractions, int divisor, int fieldLength, StringBuilder output)
        {
            var value = valueWithFractions/divisor;
            var fractions = valueWithFractions%divisor;

            OutputValue(value, fieldLength, output);

            var fractionsFieldLength = GetOutputFieldLength(divisor - 1);

            if (fractions != 0)
            {
                while ((fractions%10) == 0)
                {
                    fractions /= 10;
                    fractionsFieldLength--;
                }

                output.Append('.');
                OutputValue(fractions, fractionsFieldLength, output);
            }       
        }

        static void OutputValue(int value, int fieldLength, StringBuilder output)
        {
            var chars = new char[fieldLength];
                        
            for (var i = fieldLength - 1; i >= 0; i--)
            {
                chars[i] = (char)('0' + value%10);
                value /= 10;
            }

            output.Append(chars);
        }

        static int GetOutputFieldLength(int value)
        {
            var fieldLength = 0;

            do
            {
                value /= 10;
                fieldLength++;
            }
            while (value != 0);

            return fieldLength;
        }

        static int DayOfWeek(DateTime value)
        {
            return 1 + (int)((value.Ticks / TimeSpan.TicksPerDay) % 7);
        }      
    }
}

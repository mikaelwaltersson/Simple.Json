using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Simple.Json.Tests
{
    public class Iso8601Tests
    {
        [Fact]
        public void CanParseDateTime()
        {
            Assert.Equal(new DateTime(1985, 4, 12), Iso8601.ToDateTime("19850412"));
            Assert.Equal(new DateTime(1985, 4, 12), Iso8601.ToDateTime("1985-04-12"));
            Assert.Equal(new DateTime(1985, 4, 12, 23, 20, 13, 0), Iso8601.ToDateTime("1985-04-12T23:20:13"));
            Assert.Equal(new DateTime(1985, 4, 12, 23, 20, 0, 0), Iso8601.ToDateTime("19850412T2320"));
            Assert.Equal(new DateTime(1985, 4, 12, 23, 20, 13, 0), Iso8601.ToDateTime("19850412T232013"));
            Assert.Equal(new DateTime(1985, 4, 12, 23, 30, 0, 0), Iso8601.ToDateTime("19850412T23,50"));
            Assert.Equal(new DateTime(1985, 4, 12, 23, 43, 20, 0), Iso8601.ToDateTime("1985-04-12T23:43.333333334"));
            Assert.Equal(new DateTime(1985, 4, 12, 23, 43, 12, 20, DateTimeKind.Utc), Iso8601.ToDateTime("1985-04-12T23:43:12,02Z"));
            Assert.Equal(new DateTime(1985, 4, 12, 21, 43, 12, 20).ToLocalTime(), Iso8601.ToDateTime("1985-04-12T23:43:12,02+02:00"));
            Assert.Equal(new DateTime(1985, 4, 12, 21, 43, 12, 20).AddTicks(5000).ToLocalTime(), Iso8601.ToDateTime("1985-04-12T19:43:12,0205-02:00"));
            Assert.Equal(new DateTime(1985, 1, 1, 23, 43, 12, 20), Iso8601.ToDateTime("1985-001T23:43:12,02"));
            Assert.Equal(new DateTime(2012, 1, 2, 23, 43, 12, 20), Iso8601.ToDateTime("2012W011T234312,02"));
            Assert.Equal(new DateTime(2012, 1, 2, 23, 43, 12, 20), Iso8601.ToDateTime("2012-W01-1T23:43:12,02"));
            Assert.Equal(new DateTime(2012, 1, 3), Iso8601.ToDateTime("2012-W01-1T24:00:00,000000"));
            Assert.Equal(new DateTime(2012, 1, 3), Iso8601.ToDateTime("2012-003"));
        }

        [Fact]
        public void CanNotParseInvalidDateTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Iso8601.ToDateTime("2012-W01-0"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Iso8601.ToDateTime("2012-W01-8"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Iso8601.ToDateTime("2012-W00-1"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Iso8601.ToDateTime("2012-W54-1"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Iso8601.ToDateTime("2012-W00-1"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Iso8601.ToDateTime("2012-000"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Iso8601.ToDateTime("2012-367"));
            Assert.Throws<ArgumentOutOfRangeException>(() => Iso8601.ToDateTime("2013-366"));
        }

        [Fact]
        public void CanFormatDateTime()
        {
            var localTimeZone = new DateTime(1985, 4, 12, 23, 43, 12, DateTimeKind.Local).ToString("zzz");

            Assert.Equal("1985-04-12", Iso8601.ToString(new DateTime(1985, 4, 12)));
            Assert.Equal("1985-04-12T23:20:13", Iso8601.ToString(new DateTime(1985, 4, 12, 23, 20, 13, 0)));
            Assert.Equal("1985-04-12T23:20", Iso8601.ToString(new DateTime(1985, 4, 12, 23, 20, 0, 0)));
            Assert.Equal("1985-04-12T23:43:20", Iso8601.ToString(new DateTime(1985, 4, 12, 23, 43, 20, 0)));
            Assert.Equal("1985-04-12T23:43:12.02Z", Iso8601.ToString(new DateTime(1985, 4, 12, 23, 43, 12, 20, DateTimeKind.Utc)));
            Assert.Equal("1985-04-12T23:43:12.02" + localTimeZone, Iso8601.ToString(new DateTime(1985, 4, 12, 23, 43, 12, 20, DateTimeKind.Local)));
            Assert.Equal("1985-04-12T23:43:12.0205" + localTimeZone, Iso8601.ToString(new DateTime(1985, 4, 12, 23, 43, 12, 20, DateTimeKind.Local).AddTicks(5000)));
            Assert.Equal("1985-01-01T23:43:12.02", Iso8601.ToString(new DateTime(1985, 1, 1, 23, 43, 12, 20)));
            Assert.Equal("2012-01-02T23:43:12.02", Iso8601.ToString(new DateTime(2012, 1, 2, 23, 43, 12, 20)));
            Assert.Equal("2012-01-03", Iso8601.ToString(new DateTime(2012, 1, 3)));
        }

        [Fact]
        public void CanParseTimeSpan()
        {
            Assert.Equal(TimeSpan.FromDays(10*7), Iso8601.ToTimeSpan("P10W"));
            Assert.Equal(TimeSpan.FromDays(7*1.5), Iso8601.ToTimeSpan("P1.5W"));
            Assert.Equal(TimeSpan.FromDays(5.5), Iso8601.ToTimeSpan("P5.5D"));
            Assert.Equal(TimeSpan.FromHours(28.5), Iso8601.ToTimeSpan("P1DT4.5H"));
            Assert.Equal(new TimeSpan(2, 4, 3, 20, 35), Iso8601.ToTimeSpan("P2DT4H3M20,035S"));
            Assert.Equal(TimeSpan.FromMinutes(3) + new TimeSpan(5), Iso8601.ToTimeSpan("PT3M0.0000005S"));
            Assert.Equal(TimeSpan.FromMinutes(-3.5), Iso8601.ToTimeSpan("-PT3,5M"));
        }

        [Fact]
        public void CanNotParseYearAndMonthInTimeSpan()
        {
            Assert.Throws<FormatException>(() => Iso8601.ToTimeSpan("P1Y"));
            Assert.Throws<FormatException>(() => Iso8601.ToTimeSpan("P1M"));
        }

        [Fact]
        public void CanFormatTimeSpan()
        {
            Assert.Equal("P70D", Iso8601.ToString(TimeSpan.FromDays(10 * 7)));
            Assert.Equal("P10DT12H", Iso8601.ToString(TimeSpan.FromDays(7 * 1.5)));
            Assert.Equal("P2DT4H3M20.035S", Iso8601.ToString(new TimeSpan(2, 4, 3, 20, 35)));
            Assert.Equal("PT3M0.0000005S", Iso8601.ToString(TimeSpan.FromMinutes(3) + new TimeSpan(5)));
            Assert.Equal("-PT3M30S", Iso8601.ToString(TimeSpan.FromMinutes(-3.5)));
        }


    }
}
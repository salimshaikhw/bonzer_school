using System;
using System.Globalization;

namespace Application.Common.Helpers
{
    public static class DateHelper
    {
        private const string DateFormat = "dd/MM/yyyy";

        public static string ToAppDateString(DateTime date)
        {
            return date.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        public static DateTime ParseAppDateString(string dateString)
        {
            return DateTime.ParseExact(dateString, DateFormat, CultureInfo.InvariantCulture);
        }

        public static bool TryParseAppDateString(string dateString, out DateTime date)
        {
            return DateTime.TryParseExact(dateString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KasirCokro.Helpers
{
    public static class ValidationHelper
    {
        public static bool TryParseDecimal(string input, out decimal result, bool allowNegative = false)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Replace("Rp", "").Replace(".", "").Replace(",", ".").Trim();

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            {
                if (!allowNegative && result < 0)
                    return false;

                return true;
            }

            return false;
        }

        public static bool TryParseInt(string input, out int result, bool allowNegative = false)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (int.TryParse(input.Trim(), out result))
            {
                if (!allowNegative && result < 0)
                    return false;

                return true;
            }

            return false;
        }

        public static bool IsValidBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;

            return Regex.IsMatch(barcode.Trim(), @"^[a-zA-Z0-9]{6,50}$");
        }

        public static bool IsValidProductName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return name.Trim().Length >= 2 && name.Trim().Length <= 100;
        }

        public static bool IsValidPercentage(string percentage)
        {
            if (string.IsNullOrWhiteSpace(percentage))
                return true;
            percentage = percentage.Replace("%", "").Trim();

            if (decimal.TryParse(percentage, out decimal result))
            {
                return result >= 0 && result <= 100;
            }

            return false;
        }

        public static string FormatCurrency(decimal value)
        {
            return $"Rp {value:N0}";
        }

        public static string FormatPercentage(decimal value)
        {
            return $"{value:F2}%";
        }
    }
}
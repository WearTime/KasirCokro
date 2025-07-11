using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KasirCokro.Helpers
{
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates and parses decimal input
        /// </summary>
        /// <param name="input">String input to parse</param>
        /// <param name="result">Parsed decimal value</param>
        /// <param name="allowNegative">Allow negative values</param>
        /// <returns>True if parsing successful</returns>
        public static bool TryParseDecimal(string input, out decimal result, bool allowNegative = false)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Remove any currency symbols and whitespace
            input = input.Replace("Rp", "").Replace(".", "").Replace(",", ".").Trim();

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            {
                if (!allowNegative && result < 0)
                    return false;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates and parses integer input
        /// </summary>
        /// <param name="input">String input to parse</param>
        /// <param name="result">Parsed integer value</param>
        /// <param name="allowNegative">Allow negative values</param>
        /// <returns>True if parsing successful</returns>
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

        /// <summary>
        /// Validates barcode format
        /// </summary>
        /// <param name="barcode">Barcode to validate</param>
        /// <returns>True if valid format</returns>
        public static bool IsValidBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;

            // Basic barcode validation - alphanumeric, length 6-50
            return Regex.IsMatch(barcode.Trim(), @"^[a-zA-Z0-9]{6,50}$");
        }

        /// <summary>
        /// Validates product name
        /// </summary>
        /// <param name="name">Product name to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidProductName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Product name should be 2-100 characters
            return name.Trim().Length >= 2 && name.Trim().Length <= 100;
        }

        /// <summary>
        /// Validates percentage input
        /// </summary>
        /// <param name="percentage">Percentage string to validate</param>
        /// <returns>True if valid percentage format</returns>
        public static bool IsValidPercentage(string percentage)
        {
            if (string.IsNullOrWhiteSpace(percentage))
                return true; // Optional field

            // Remove % symbol if present
            percentage = percentage.Replace("%", "").Trim();

            if (decimal.TryParse(percentage, out decimal result))
            {
                return result >= 0 && result <= 100;
            }

            return false;
        }

        /// <summary>
        /// Formats decimal as currency
        /// </summary>
        /// <param name="value">Decimal value to format</param>
        /// <returns>Formatted currency string</returns>
        public static string FormatCurrency(decimal value)
        {
            return $"Rp {value:N0}";
        }

        /// <summary>
        /// Formats percentage
        /// </summary>
        /// <param name="value">Decimal value to format as percentage</param>
        /// <returns>Formatted percentage string</returns>
        public static string FormatPercentage(decimal value)
        {
            return $"{value:F2}%";
        }
    }
}
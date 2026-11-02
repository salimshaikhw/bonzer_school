using System.Linq;

namespace Application.Validation.Common
{
    /// <summary>
    /// Aadhaar number checks (UIDAI format): 12 digits, not starting with 0 or 1, and a valid Verhoeff check digit,
    /// which catches every single-digit typo and almost every swap of two neighbouring digits.
    /// </summary>
    public static class Aadhaar
    {
        private static readonly int[,] Multiplication =
        {
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, { 1, 2, 3, 4, 0, 6, 7, 8, 9, 5 }, { 2, 3, 4, 0, 1, 7, 8, 9, 5, 6 },
            { 3, 4, 0, 1, 2, 8, 9, 5, 6, 7 }, { 4, 0, 1, 2, 3, 9, 5, 6, 7, 8 }, { 5, 9, 8, 7, 6, 0, 4, 3, 2, 1 },
            { 6, 5, 9, 8, 7, 1, 0, 4, 3, 2 }, { 7, 6, 5, 9, 8, 2, 1, 0, 4, 3 }, { 8, 7, 6, 5, 9, 3, 2, 1, 0, 4 },
            { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 },
        };

        private static readonly int[,] Permutation =
        {
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, { 1, 5, 7, 6, 2, 8, 3, 0, 9, 4 }, { 5, 8, 0, 3, 7, 9, 6, 1, 4, 2 },
            { 8, 9, 1, 6, 0, 4, 3, 5, 2, 7 }, { 9, 4, 5, 3, 1, 2, 6, 8, 7, 0 }, { 4, 2, 8, 6, 5, 7, 3, 9, 0, 1 },
            { 2, 7, 9, 3, 8, 0, 6, 4, 1, 5 }, { 7, 0, 4, 6, 9, 1, 3, 2, 5, 8 },
        };

        /// <summary>Digits only ("1234 5678 9012" -> "123456789012"); null when blank.</summary>
        public static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());

        /// <summary>Null when valid; otherwise why not.</summary>
        public static string? Problem(string? value)
        {
            var digits = Normalize(value);
            if (digits == null)
                return "Aadhaar is required.";
            if (digits.Length != 12 || !digits.All(char.IsDigit))
                return "Aadhaar must be exactly 12 digits.";
            if (digits[0] is '0' or '1')
                return "Aadhaar cannot start with 0 or 1.";
            if (!HasValidCheckDigit(digits))
                return "Aadhaar number is not valid (check digit mismatch); please re-check the number.";
            return null;
        }

        public static bool IsValid(string? value) => Problem(value) == null;

        private static bool HasValidCheckDigit(string digits)
        {
            var check = 0;
            for (var i = 0; i < digits.Length; i++)
                check = Multiplication[check, Permutation[i % 8, digits[digits.Length - 1 - i] - '0']];
            return check == 0;
        }
    }
}

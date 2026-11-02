using System;
using System.Text.RegularExpressions;
using FluentValidation;

namespace Application.Validation.Common
{
    /// <summary>Reusable FluentValidation rules, so every module validates phones, emails and names the same way.</summary>
    public static class ValidationRules
    {
        // Digits with an optional leading + and spaces / hyphens, 7-20 characters: "+91 98765-43210", "020 2612 3456".
        private static readonly Regex PhonePattern = new(@"^\+?[0-9][0-9 \-]{5,18}[0-9]$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        /// <summary>Optional phone number; when given it must look like one.</summary>
        public static IRuleBuilderOptions<T, string?> PhoneNumber<T>(this IRuleBuilder<T, string?> rule) =>
            rule.Must(value => string.IsNullOrWhiteSpace(value) || PhonePattern.IsMatch(value.Trim()))
                .WithMessage("{PropertyName} must be a valid phone number (digits, optional + and spaces).");

        /// <summary>Optional email address, at most <paramref name="maxLength"/> characters.</summary>
        public static IRuleBuilderOptions<T, string?> OptionalEmail<T>(this IRuleBuilder<T, string?> rule, int maxLength = 150) =>
            rule.MaximumLength(maxLength)
                .Must(value => string.IsNullOrWhiteSpace(value) || IsEmail(value.Trim()))
                .WithMessage("{PropertyName} must be a valid email address.");

        private static bool IsEmail(string value) =>
            System.Net.Mail.MailAddress.TryCreate(value, out var address)
            && address.Address == value
            && address.Host.Contains('.');

        /// <summary>Required display name: not blank, at most <paramref name="maxLength"/> characters.</summary>
        public static IRuleBuilderOptions<T, string?> RequiredName<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
            rule.Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("{PropertyName} is required.")
                .MaximumLength(maxLength);
    }
}

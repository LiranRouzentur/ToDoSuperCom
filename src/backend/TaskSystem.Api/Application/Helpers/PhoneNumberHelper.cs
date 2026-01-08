using System.Text.RegularExpressions;

namespace TaskApp.Application.Helpers;

public static class PhoneNumberHelper
{
    private static readonly Regex IsraeliPhoneRegex = new(@"^(\+972|0)([23489]|5[0-9])[0-9]{7}$", RegexOptions.Compiled);

    public static bool IsValidIsraeliPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        return IsraeliPhoneRegex.IsMatch(phone.Trim());
    }

    public static string NormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return phone;

        var trimmed = phone.Trim();

        // If starts with 0, replace with +972
        if (trimmed.StartsWith("0"))
        {
            return "+972" + trimmed.Substring(1);
        }

        // If already starts with +972, return as is
        if (trimmed.StartsWith("+972"))
        {
            return trimmed;
        }

        // Otherwise return as is (should be validated before normalization)
        return trimmed;
    }
}


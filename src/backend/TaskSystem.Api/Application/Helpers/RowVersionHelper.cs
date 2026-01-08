using System.Text;

namespace TaskApp.Application.Helpers;

public static class RowVersionHelper
{
    public static string ToBase64String(byte[] rowVersion)
    {
        if (rowVersion == null || rowVersion.Length == 0)
            return string.Empty;

        return Convert.ToBase64String(rowVersion);
    }

    public static byte[] FromBase64String(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            return Array.Empty<byte>();

        try
        {
            return Convert.FromBase64String(base64String);
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }
}


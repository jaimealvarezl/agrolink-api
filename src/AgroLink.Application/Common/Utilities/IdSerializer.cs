using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace AgroLink.Application.Common.Utilities;

public static class IdSerializer
{
    public static string Encode(string type, int id)
    {
        var value = $"{type}:{id}";
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(value));
    }

    public static (string Type, int Id) Decode(string s)
    {
        var bytes = WebEncoders.Base64UrlDecode(s);
        var decoded = Encoding.UTF8.GetString(bytes);
        var parts = decoded.Split(':');

        if (parts.Length != 2 || !int.TryParse(parts[1], out var id))
        {
            throw new ArgumentException("Invalid Global ID format");
        }

        return (parts[0], id);
    }
}

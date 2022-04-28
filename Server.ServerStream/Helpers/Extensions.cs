using System.Text.RegularExpressions;

namespace Server.ServerStream.Helpers;

public static class Extensions
{
    private static readonly Regex GuidRegEx = new("^[A-Fa-f0-9]{32}$|" +
                                                  "^({|\\()?[A-Fa-f0-9]{8}-([A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}(}|\\))?$|" +
                                                  "^({)?[0xA-Fa-f0-9]{3,10}(, {0,1}[0xA-Fa-f0-9]{3,6}){2}, {0,1}({)([0xA-Fa-f0-9]{3,4}, {0,1}){7}[0xA-Fa-f0-9]{3,4}(}})$",
        RegexOptions.Compiled);

    public static Guid ToGuid(this string s)
    {
        if (!string.IsNullOrEmpty(s) && GuidRegEx.IsMatch(s))
            return new Guid(s);

        return default;
    }
}
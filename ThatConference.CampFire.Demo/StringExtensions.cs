using System.Text;
using System.Text.RegularExpressions;

namespace ThatConference.CampFire.Demo
{
    static class StringExtensions
    {
        public static string SplitCamelCase(this string value)
            => Regex.Replace(value, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");

        public static string ToTitleCase(this string str) => ToTitleCaseIpml(str);

        static string ToTitleCaseIpml(string value)
        {
            if (value == null) return null;
            if (value.Length == 0) return value;

            var result = new StringBuilder(value);
            result[0] = char.ToUpper(result[0]);
            for (int i = 1; i < result.Length; ++i)
            {
                if (char.IsWhiteSpace(result[i - 1]))
                {
                    result[i] = char.ToUpper(result[i]);
                }
                else
                {
                    result[i] = char.ToLower(result[i]);
                }
            }

            return result.ToString();
        }
    }
}

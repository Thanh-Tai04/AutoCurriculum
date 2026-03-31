using System.Text;
using System.Text.RegularExpressions;

namespace AutoCurriculum.Helpers
{
    public static class StringHelper
    {
        public static string ConvertToSlug(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            string normalized = input.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString().Normalize(NormalizationForm.FormC);

            result = Regex.Replace(result, @"\s+", "_");

            result = Regex.Replace(result, @"[^a-zA-Z0-9_]", "");

            return result;
        }
    }
}
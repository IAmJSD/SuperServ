using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SuperServ
{
    class InsanelySimpleRenderer
    {
        /// <summary>
        /// The goal of this renderer is to just be as quick as possible.
        /// This is NOT designed for rendering every page. Just pages with a very small amount of elements that need rendering.
        /// \$[a-zA-Z0-9-_]+\$ - This regex will pick up and use the text inside as a dictionary key. It will escape the dictionary value.
        /// ![a-zA-Z0-9-_]+! - This regex will pick up and use the text inside as a dictionary key. It will NOT escape the dictionary value.
        /// </summary>
        public static Regex DollarRegex = new Regex("\\$[a-zA-Z0-9-_]+\\$");
        public static Regex ExclamationMarkRegex = new Regex("![a-zA-Z0-9-_]+!");
        public static Dictionary<string, string> HTMLEscapes = new Dictionary<string, string>
        {
            { "<", "&lt;" },
            { ">", "&gt;" },
            { "&", "&amp;" },
            { "\"", "&quot;" },
            { "'", "&apos" }
        };

        public static Nancy.Response Render(string html_data, Dictionary<string, string> replacement_data, Nancy.HttpStatusCode status = Nancy.HttpStatusCode.OK)
        {
            foreach (Match match in DollarRegex.Matches(html_data))
            {
                string MatchKey = match.Value.Trim('$');
                string MatchResult = replacement_data[MatchKey];
                foreach (var item in HTMLEscapes)
                {
                    MatchResult.Replace(item.Key, item.Value);
                }
                html_data = html_data.Replace(match.Value, MatchResult);
            }
            foreach (Match match in ExclamationMarkRegex.Matches(html_data))
            {
                string MatchKey = match.Value.Trim('!');
                string MatchResult = replacement_data[MatchKey];
                html_data = html_data.Replace(match.Value, MatchResult);
            }
            return new Nancy.Response()
            {
                StatusCode = status,
                ContentType = "text/html",
                Contents = stream => (new StreamWriter(stream) { AutoFlush = true }).Write(html_data)
            };
        }
    }
}

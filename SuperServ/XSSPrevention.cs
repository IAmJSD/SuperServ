using System.Collections.Generic;

namespace SuperServ
{
    public class XSSPrevention
    {
        /// <summary>
        /// This class contains a function to strip things of XSS-able characters.
        /// </summary>

        public static string XSSParse(string data) {
            return data
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&#39;")
                .Replace("\"", "&#34;");
        }
    }
}

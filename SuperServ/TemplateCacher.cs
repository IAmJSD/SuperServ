using System;
using System.Collections.Generic;
using System.IO;

namespace SuperServ
{
    class TemplateCacher
    {
        // <summary>
        // The purpose of this class is extremely simple.
        // This class is used to cache the templates. This helps to lower disk usage.
        // </summary>
        public static Dictionary<string, string> CachedData = new Dictionary<string, string>();

        public static string ReadTemplate(string path)
        {
            try
            {
                return CachedData[path];
            } catch (Exception) {
                // Do nothing.
            }
            string content = File.ReadAllText(path);
            CachedData[path] = content;
            return content;
        }
    }
}

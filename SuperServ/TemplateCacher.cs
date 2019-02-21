using System;
using System.Collections.Generic;
using System.IO;

namespace SuperServ
{
    class TemplateCacher
    {
        /// <summary>
        /// The purpose of this class is extremely simple.
        /// This class is used to cache the templates. This helps to lower disk usage and make pages load quicker.
        /// </summary>
        public static Dictionary<string, Scriban.Template> CachedData = new Dictionary<string, Scriban.Template>();

        public static Scriban.Template ReadTemplate(string path)
        {
            try
            {
                return CachedData[path];
            } catch (Exception) {
                // Do nothing.
            }
            Scriban.Template content = Scriban.Template.Parse(File.ReadAllText(path));
            CachedData[path] = content;
            return content;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SuperServ
{
    class RegexCompilations
    {
        public static Regex password_complexity_re;
        public static Regex email_re;
        public static void LoadRegex()
        {
            password_complexity_re = new Regex("^(((?=.*[a-z])(?=.*[A-Z]))|((?=.*[a-z])(?=.*[0-9]))|((?=.*[A-Z])(?=.*[0-9])))(?=.{6,})");
            email_re = new Regex("^([a-zA-Z0-9_\\-\\.]+)@([a-zA-Z0-9_\\-\\.]+)\\.([a-zA-Z]{2,5})$");
        }
    }
}

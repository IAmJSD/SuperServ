using Nancy.Hosting.Self;
using System;
using System.Drawing;
using Console = Colorful.Console;

namespace SuperServ
{
    class Program
    {
        public static ConfigHandling config_handler;
        static void Main(string[] args)
        {
            RegexCompilations.LoadRegex();
            config_handler = new ConfigHandling();
            Console.WriteAscii("SuperServ", Color.Orange);
            Console.WriteLine("Copyright (C) Jake Gealer <jake@gealer.email> 2018-2019.", Color.Orange);
            config_handler.LoadConfig();
            Console.WriteLine("Config successfully loaded.", Color.LimeGreen);
            HostConfiguration hostConf = new HostConfiguration();
            hostConf.RewriteLocalhost = true;
            hostConf.UrlReservations.CreateAutomatically = true;
            NancyHost nancy = new NancyHost(hostConf, new Uri($"http://localhost:{config_handler.config.port}"));
            nancy.Start();
            Console.WriteLine($"Serving on port {config_handler.config.port}. Press CTRL+C or kill the container if this is running in Docker to stop.");
            while (true) {
                Console.ReadLine();
            }
        }
    }
}

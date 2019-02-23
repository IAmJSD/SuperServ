using Nancy.Hosting.Self;
using System;
using System.Drawing;
using Console = Colorful.Console;

namespace SuperServ
{
    class Program
    {
        public static ConfigHandling config_handler;
        public static bool InDockerContainer;
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
            InDockerContainer = Environment.GetEnvironmentVariable("IN_DOCKER_CONTAINER") == "true";
            string killMessage = "Press CTRL+C to stop.";
            if (InDockerContainer) {
                killMessage = "Shut down the Docker container to stop.";
            }
            Console.WriteLine($"Serving on port {config_handler.config.port}. {killMessage}");
            while (true) {
                Console.ReadLine();
            }
            nancy.Stop();
        }
    }
}

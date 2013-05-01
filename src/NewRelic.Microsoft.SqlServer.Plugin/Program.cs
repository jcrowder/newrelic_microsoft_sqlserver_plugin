﻿using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using CommandLine;
using NewRelic.Microsoft.SqlServer.Plugin.Configuration;
using NewRelic.Microsoft.SqlServer.Plugin.Core;
using log4net;
using log4net.Config;

namespace NewRelic.Microsoft.SqlServer.Plugin
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var options = new Options();

                var log = SetUpLogConfig();

                // If bad args were passed, will exit and print usage
                Parser.Default.ParseArgumentsStrict(args, options);

                var installController = new InstallController(log);
                if (options.Uninstall)
                {
                    installController.Uninstall();
                }
                else if (options.Install)
                {
                    installController.Install();
                }
                else if (options.Start)
                {
                    installController.StartService();
                }
                else if (options.Stop)
                {
                    installController.StopService();
                }
                else if (options.InstallOrStart)
                {
                    installController.InstallOrStart();
                }
                else
                {
                    Settings settings = ConfigurationParser.ParseSettings(log, options.ConfigFile);
                    settings.CollectOnly = options.CollectOnly;

                    if (Environment.UserInteractive)
                    {
                        Console.Out.WriteLine("Starting Interactive mode");
                        RunInteractive(settings);
                    }
                    else
                    {
                        ServiceBase[] services = {new SqlMonitorService(settings)};
                        ServiceBase.Run(services);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);

                if (Environment.UserInteractive)
                {
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }

                return -1;
            }
        }

        public static ILog SetUpLogConfig()
        {
            const string log4NetConfig = "log4net.config";
            XmlConfigurator.ConfigureAndWatch(new FileInfo(log4NetConfig));
            return LogManager.GetLogger(Constants.SqlMonitorLogger);

        }

        /// <summary>
        ///     Runs from the command shell, printing to the Console.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        private static void RunInteractive(Settings settings)
        {
            Console.Out.WriteLine("Starting Server");

            // Start our services
            var monitor = new SqlMonitor(settings);
            monitor.Start();

            // Capture Ctrl+C
            Console.TreatControlCAsInput = true;

            char key;
            do
            {
                Console.Out.WriteLine("Press Q to quit...");
                ConsoleKeyInfo consoleKeyInfo = Console.ReadKey();
                Console.WriteLine();
                key = consoleKeyInfo.KeyChar;
            } while (key != 'q' && key != 'Q');

            Console.Out.WriteLine("Stopping...");

            // Stop our services
            monitor.Stop();

#if DEBUG
            if (Debugger.IsAttached)
            {
                Console.Out.WriteLine("Press any key to stop debugging...");
                Console.ReadKey();
            }
#endif
        }
    }
}
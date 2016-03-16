﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using log4net.Config;
using NewCamd;

namespace Test.NewCamdClient
{
    class Program
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof (Program));
        readonly NewCamdClient _client = new NewCamdClient();

        const string IpAdress = "localhost";
        const int Port = 15050;

        static void Main(string[] args)
        {
            try
            {
                var values = GetValues<NewCamdMessageType>();
                foreach (var value in values)
                {
                    Console.WriteLine($"{value}: {(int)value}");
                }

                var encdata = File.ReadAllBytes(@"C:\temp\src\fromubuntu\src\data\encrypted2.dat");
                var uncdata = File.ReadAllBytes(@"C:\temp\src\fromubuntu\src\data\unEncrypted4.dat");

                var mes1 = (NewCamdMessageType) encdata[0];
                var mes2 = (NewCamdMessageType) uncdata[0];

                XmlConfigurator.ConfigureAndWatch(new FileInfo("Log4net.config"));
                Logger.Info("Welcome to the NewCamd Test Client");
                var program = new Program();
                program.Run();
            }
            catch (Exception ex)
            {
                Logger.Fatal("An unhandled exception occured", ex);
            }
            Logger.Info("Thanks for using the NewCamd Test Client");
        }

        void Run()
        {
            ShowHelp();
            var line = "";
            try
            {
                while (line != "6")
                {
                    line = Console.ReadLine();
                    HandleLine(line);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception occured during job: {line}", ex);
                _client.Disconnect();
            }
        }

        void HandleLine(string line)
        {
            switch (line)
            {
                case "1":
                    _client.Connect(IpAdress, Port);
                    break;
                case "2":
                    _client.Login();
                    break;
                case "3":
                    _client.GetKey();
                    break;
                case "4":
                    _client.Disconnect();
                    break;
                case "5":
                    UpdateCredentials();
                    break;
                case "6":
                    Logger.Info("Exit test module");
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }

        void UpdateCredentials()
        {
            Console.WriteLine($"Change username, hit just enter to ignore (current: {_client.UserName})");
            var line = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(line)) _client.UserName = line;
            Console.WriteLine($"Change password, hit just enter to ignore (current: {_client.Password})");
            line = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(line)) _client.Password = line;
            Console.WriteLine($"Change DES key, hit just enter to ignore (current: {_client.DesKey})");
            line = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(line)) _client.DesKey = line;
        }

        static void ShowHelp()
        {
            Logger.Info("Hit one of the following numbers and end with enter:");
            Logger.Info(" 1: Connect");
            Logger.Info(" 2: Login");
            Logger.Info(" 3: ReceiveKey");
            Logger.Info(" 4: Disconnect");
            Logger.Info(" 5: Change Settings");
            Logger.Info(" 6: Exit");
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using log4net;
using SharedComponents;
using SharedComponents.DependencyInjection;

namespace NewCamd
{
    public class Program : BaseModule
    {
        readonly ILog _logger;
        readonly Settings _settings;
        readonly Func<NewCamdApi> _clientFactory;

        readonly object _syncObject = new object();
        readonly List<NewCamdApi> _activeClients;
        TcpListener _listener;
        bool _listening;
        Task _listeningTask;

        public Program(ILog logger, Settings settings, Func<NewCamdApi> clientFactory)
        {
            _logger = logger;
            _settings = settings;
            _clientFactory = clientFactory;
            _activeClients = new List<NewCamdApi>();
        }

        static void Main()
        {
            var container = SharedContainer.CreateAndFill<DependencyConfig>("Log4net.config");
            var prog = container.GetInstance<Program>();

            prog.Start();
            Console.WriteLine("Hit 'Enter' to exit");
            Console.ReadLine();
            prog.Stop();
        }

        protected override void StartModule()
        {
            try
            {
                _logger.Info("Welcome to NewCamd");
                _settings.Load();
                StartServer();
                _listeningTask = Listen();
            }
            catch (Exception ex)
            {
                _logger.Fatal("An unhandled exception occured", ex);
                Error();
            }
        }

        protected override void StopModule()
        {
            try
            {
                _listening = false;
                _listener.Stop();
                _logger.Info("Stopped listening");
                CloseClients();
                if (_listeningTask == null || _listeningTask.Status != AsyncTaskIsRunning) return;

                _logger.Warn("Wait max 10 sec for the Listener to stop");
                _listeningTask.Wait(10000);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop the tcp listener", ex);
            }
        }

        void StartServer()
        {
            var ip = GetIpAdress();
            _listener = new TcpListener(ip, _settings.Port);
            _listener.Start();
            _listening = true;
            _logger.Info($"Start listening at {ip}:{_settings.Port}");
        }

        IPAddress GetIpAdress()
        {
            if(string.IsNullOrWhiteSpace(_settings.IpAdress)) return IPAddress.Any;
            try
            {
                return IPAddress.Parse(_settings.IpAdress);
            }
            catch (Exception)
            {
                _logger.Warn($"Failed to parse IpAdress to listen on: {_settings.IpAdress}, use Any");
                return IPAddress.Any;
            }
        }

        async Task Listen()
        {
            try
            {
                while (_listening)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _logger.Debug("Try to accept new api");
                    var clientHandler = _clientFactory();
                    clientHandler.Closed += ClientClosed;
                    AddClientToWatchList(clientHandler);
                    clientHandler.HandleClient(client);
                }
            }
            catch (ObjectDisposedException)
            {
                if (_listening)
                {
                    throw;
                }
                //Ignore because this is expected to happen when we stopped listening    
            }
        }

        void ClientClosed(object sender, EventArgs e)
        {
            var client = (NewCamdApi)sender;
            _logger.Info($"Stop monitoring client {client.Name}");
            RemoveClientFromWatchList(client);
        }

        void CloseClients()
        {
            _logger.Info($"Close {_activeClients.Count} clients");
            try
            {
                NewCamdApi[] clients;
                lock (_syncObject)
                {
                    clients = _activeClients.ToArray();
                }
                foreach (var client in clients)
                {
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to close one of the clients");
                _logger.Debug("CloseClients", ex);
            }
        }

        void AddClientToWatchList(NewCamdApi api)
        {
            lock (_syncObject)
            {
                _activeClients.Add(api);
            }
            _logger.Debug($"Added client {api.Name} to the watchlist");
        }

        void RemoveClientFromWatchList(NewCamdApi api)
        {
            lock (_syncObject)
            {
                _activeClients.Remove(api);
            }
            _logger.Debug($"Removed client {api.Name} from the watchlist");
        }
    }
}


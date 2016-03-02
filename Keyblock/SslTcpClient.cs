﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using log4net;

namespace Keyblock
{
    public class SslTcpClient
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SslTcpClient));
        readonly IniSettings _settings;
        string Communicationfolder => Path.Combine(_settings.DataFolder, _settings.CommunicationFolder);

        public SslTcpClient(IniSettings settings)
        {
            _settings = settings;
        }

        public byte[] SendAndReceive(string msg, string server, int port, bool useSsl = true, [CallerMemberName] string caller = null)
        {
            Logger.Debug("Convert ASCII message into bytes");
            // Encode a test message into a byte array.
            var messsage = Encoding.ASCII.GetBytes(msg);

            // ReSharper disable once ExplicitCallerInfoArgument
            // Just redirecting, we want the original caller
            return SendAndReceive(messsage, server, port, useSsl, caller);
        }

        public byte[] SendAndReceive(byte[] msg, string server, int port, bool useSsl = true, [CallerMemberName] string caller = null)
        {
            byte[] response = null;
            try
            {
                response = _settings.DontUseRealServerButMessagesFromDisk ? 
                    CommunicateWithDisk(caller) : 
                    CommunicateWithServer(msg, server, port, useSsl, caller);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed during send and receive", ex);
            }
            return response;
        }

        byte[] CommunicateWithDisk(string caller)
        {
            var path = ResponsePath(caller);
            Logger.Warn($"Read response from disk: {path}");
            if(!File.Exists(path)) throw new FileNotFoundException($"Failed to find communication response file: {path}");
            var response = File.ReadAllBytes(path);
            Logger.Info($"Received {response.Length} bytes from disk");
            return response;
        }

        byte[] CommunicateWithServer(byte[] msg, string server, int port, bool useSsl, string caller)
        {
            Logger.Info($"Send message to {server}:{port}");
            byte[] response;
            WriteToDisk(RequestPath(caller), msg);
            using (var stream = OpenPort(server, port, useSsl))
            {
                Send(stream, msg);
                response = Read(stream);
            }
            WriteToDisk(ResponsePath(caller), response);
            Logger.Info($"Received {response.Length} bytes from the server");
            return response;
        }

        void WriteToDisk(string path, byte[] msg)
        {
            if (!_settings.WriteAllCommunicationToDisk)
            {
                Logger.Debug("WriteAllCommunicationToDisk is disabled in ini file");    
            }
            _settings.EnsureDataFolderExists(Communicationfolder);
            Logger.Debug($"Write {msg.Length} to disk '{path}'");
            File.WriteAllBytes(path, msg);
        }

        static Stream OpenPort(string server, int port, bool useSsl)
        {
            //Open a tcp connection with the server
            Logger.Debug($"Open a TCP connection with {server}:{port}");
            var client = new TcpClient(server, port);
            // Create an SSL stream that will close the client's stream.
            Stream stream;
            if (useSsl)
            {
                Logger.Debug("Wrap the TCP connection in a SSL stream");
                var sslStream = new SslStream(client.GetStream(), true, ValidateServerCertificate, null);
                sslStream.AuthenticateAsClient(server);
                stream = sslStream;
            }
            else
            {
                Logger.Debug("Use TCP connection stream");
                stream = client.GetStream();
            }
            Logger.Debug($"Succesfully connected to {server}:{port}");
            return stream;
        }

        static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //We already trust the server, so ignore this part
            return true;
        }

        static byte[] Read(Stream stream)
        {
            // Read the  message sent by the server.
            Logger.Debug("Start reading bytes from the server");
            var buffer = new byte[2048];
            var receivedBytes = new List<byte>();
            int bytes;
            do
            {
                bytes = stream.Read(buffer, 0, buffer.Length);
                receivedBytes.AddRange(buffer.Take(bytes));
            } while (bytes != 0);
            return receivedBytes.ToArray();
        }

        static void Send(Stream stream, byte[] message)
        {
            //Send the bytes to the server
            Logger.Debug($"Write {message.Length} bytes to the server");
            stream.Write(message, 0, message.Length);
            stream.Flush();
        }

        string ResponsePath(string caller)
        {
            return Path.Combine(Communicationfolder, $"{caller}.response");
        }

        string RequestPath(string caller)
        {
            return Path.Combine(Communicationfolder, $"{caller}.request");
        }
    }
}
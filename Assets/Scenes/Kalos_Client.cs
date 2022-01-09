using System.IO.Pipes;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using UnityEngine;
using Kalos.Sockets.v1.Server;
using Kalos.Sockets.v2.Client;
using Kalos.Utilities.Debug;

namespace Kalos.Sockets.v1.Client
{
    public class Kalos_Client : MonoBehaviour
    {
        private static Socket client;
        public SocketVerson Version;

        void Start(){
            if (Version == SocketVerson.v1)
                Initv1();
            else
                Initv2();
        }

        /// <summary>
        /// Initialize Function for version 1
        /// </summary>
        public void Initv1()
        {
            //Initialize Socket
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //Loop Connect
            int attempts = 0;
            while (!client.Connected){
                try{
                    attempts++;
                    client.Connect(IPAddress.Loopback, Kalos_Server.PORT_NUMBER);
                }
                catch (SocketException){
                    KDebug.Log($"Connection Attempts : {attempts}");
                }
            }
        }

        /// <summary>
        /// Initialize Function for Version 2
        /// </summary>
        public void Initv2()
        {
            PipeClient server = new PipeClient();
            server.Init("Hello from this part of the world!");
        }
    }
}

namespace Kalos.Sockets.v2.Client
{
    public class PipeClient
    {
        public string message { get; set; }
        public void Init(string message)
        {
            Thread serverWrite = new Thread(OnServerWrite);
            this.message = message;
        }

        private async void OnServerWrite()
        {
            //Create Stream and wait
            NamedPipeClientStream stream = new NamedPipeClientStream(".", "kalos",PipeDirection.Out);
            await stream.ConnectAsync();
            KDebug.Log("Connected Buddy!");

            //Write
            StreamString str = new StreamString(stream);
            str.WriteString(this.message);

            //Close
            stream.Close();
        }
    }
}

namespace Kalos.Sockets
{
    public enum SocketVerson
    {
        v1,
        v2,
        v3_beta
    }
}
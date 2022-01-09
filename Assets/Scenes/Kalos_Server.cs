using System;
using System.Net;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Kalos.Main;
using Kalos.Sockets.v2.Server;
using Kalos.Utilities.Debug;
using Kalos.Utilities.General;
using Kalos.Utilities.Interfaces;
using Kalos.Utilities.Interfaces.Empty;

namespace Kalos.Sockets.v1.Server
{
    public class Kalos_Server : MonoBehaviour, KMono
    {
        //Server Version
        public SocketVerson version;

        //List of clients online
        private static List<Socket> clients = new List<Socket>();

        //Reference to the Socket
        private static Socket serverSocket;

        //Port Number
        public const int PORT_NUMBER = 100;

        //Buffer
        private static byte[] buffer = new byte[1024];

        //Data Recieved
        private static string data = string.Empty;

        void Start(){
            Init();
        }

        /// <summary>
        /// Initialize Methods
        /// </summary>
        public void Init(DebugMode verbose = DebugMode.yes)
        {
            if(version == SocketVerson.v1){
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SetupServer(verbose);
            }
            else{
                PipeServer server = new PipeServer();
                server.Init();
            }
        }
        public void SetupServer(DebugMode verbose)
        {
            if (verbose == DebugMode.yes)
                KDebug.Log("Setting Up Server");

            //Bind Server
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT_NUMBER));
            serverSocket.Listen(5);
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        /// <summary>
        /// Accept Callback
        /// </summary>
        private static void AcceptCallback(IAsyncResult AR)
        {
            //Add it to the List
            Socket client = serverSocket.EndAccept(AR);
            clients.Add(client);

            //Begin Data Reccursion
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        /// <summary>
        /// Client Data Recieve Callback
        /// </summary>
        private static void ReceiveCallback(IAsyncResult AR)
        {
            //Recieve Data
            Socket client = (Socket)AR.AsyncState;
            int recieved = client.EndReceive(AR);

            //Get The Data Buffer
            byte[] databuffer = new byte[recieved];

            //Copy the Array to a shallow one
            Array.Copy(buffer, databuffer, recieved);

            //Get Text
            string text = Encoding.ASCII.GetString(databuffer);

            data = text;
            KDebug.Log($"Data Recived  : {text}");
        }


        /// <summary>
        /// Client Send Callback
        /// </summary>
        private static void SendCallback(IAsyncResult AR)
        {
            Socket client = (Socket)AR.AsyncState;
            client.EndSend(AR);
        }

        /// <summary>
        /// Method to Send Message
        /// </summary>
        public void SendMessageSocket(string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            serverSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
        }
    }

}

namespace Kalos.Sockets.v2.Server
{
    public class PipeServer : KCloneable, KDestroyable
    {
        public string message { get; set; }
        public int users { get; set; }

        public void Init()
        {
            Thread serverRead = new Thread(OnServerThreadRead);
            serverRead.Start();
        }

        private async void OnServerThreadRead()
        {
            //Create Stream and Wait
            NamedPipeServerStream stream = new NamedPipeServerStream("kalos", PipeDirection.In);
            await stream.WaitForConnectionAsync();
            KDebug.Log("Connected Buddy!");

            //Read Message
            StreamString str = new StreamString(stream);
            string message = str.ReadString();
            this.message = message;
            KDebug.Log("RCV : " + message);
            users++;
            stream.Close();
            Init();
        }

        public object Clone(Kalos_VirtualAssistant main)
        {
            return this;
        }

        public void Destroy(Kalos_VirtualAssistant main){
            Init();
        }
    }
}

namespace Kalos.Sockets.v2
{

    public class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len = 0;

            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }

    // Contains the method executed in the context of the impersonated user
    public class ReadFileToStream
    {
        private string fn;
        private StreamString ss;

        public ReadFileToStream(StreamString str, string filename)
        {
            fn = filename;
            ss = str;
        }

        public void Start()
        {
            string contents = File.ReadAllText(fn);
            ss.WriteString(contents);
        }
    }
}
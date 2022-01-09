using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using UnityEngine;
using Kalos.Main;
using Kalos.Python.Objects;
using Kalos.Utilities.Debug;
using Kalos.Utilities.Interfaces.Empty;
using Newtonsoft.Json;

namespace Kalos.Python
{
    namespace Communication
    {
        public class Kalos_Communication : MonoBehaviour, KMono
        {
            public const string IP = "192.168.0.105";
            public const int Port = 25001;
            public TcpClient Client;
            public TcpListener Listener;
            public IPAddress IPAddress;
            public event OnDataRecieved OnDataRecieved;

            private void Start(){
                InitializeServer();
            }

            public void InitializeServer()
            {
                //Create IP address
                IPAddress = IPAddress.Parse(IP);

                //Create Listener
                Listener = new TcpListener(IPAddress.Any, Port);
                Listener.Start();

                //Create Client
                Client = Listener.AcceptTcpClient();

                KDebug.Log("Connection initialized");
                //while (true){
                //    RecieveData();
                //}
            }

            public void Send(string message)
            {
                //Strem
                NetworkStream nwStream = Client.GetStream();

                //Converting string to byte data
                byte[] myWriteBuffer = Encoding.ASCII.GetBytes(message); 
                nwStream.Write(myWriteBuffer, 0, myWriteBuffer.Length);
            }

            public void RecieveData()
            {
                Thread.Sleep(1000 / 300);
                NetworkStream stream = Client.GetStream();
                byte[] buffer = new byte[Client.ReceiveBufferSize];

                //Check for Recieving data
                int bytesRead = stream.Read(buffer, 0, Client.ReceiveBufferSize); //Getting data in Bytes from Python
                string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead); //Converting byte data to string

                //Check if we got something
                if (dataReceived != null){
                    OnDataRecieved?.Invoke(bytesRead, dataReceived, this);
                }
            }

            public void AssignMain(Kalos_VirtualAssistant ar)
            {
                return;
            }
        }
    }

    namespace Objects
    {
        [Serializable]
        public class PythonData
        {
            [JsonProperty(PropertyName = "intent")]
            public string Intent;

            [JsonProperty(PropertyName = "main")]
            public string Message;
        }

        public delegate void OnDataRecieved(int data, string data_string, object sender);
    }
}

namespace Kalos.Utilities.Interfaces.Empty
{
    public interface KW2V
    {

    }

    public interface KTraining
    {

    }
}
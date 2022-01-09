using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Events;
using System.Threading;


/// <summary>
/// MIT license
/// Created by Haikun Huang
/// Date: 2021
/// </summary>
///

namespace DCXR
{
    [AddComponentMenu("DCXR/UPython2")]
    [HelpURL("https://www.notion.so/UPython2-7431b13d3f0f4a41aa6fb6e16da782a3")]
    public class UPython2 : MonoBehaviour
    {

        [Header("Listen To")]
        public UPython2ChannelSO listenToChannel = default;

        List<Socket> activatedSockets = new List<Socket>();

        CancellationTokenSource taskCancelToken = new CancellationTokenSource();


        private void OnEnable()
        {
            // subscribe the channels
            listenToChannel.python_cmd += Call;
        }


        private void OnDisable()
        {
            taskCancelToken.Cancel();

            // close all activated sockets
            while (activatedSockets.Count > 0)
            {
                activatedSockets[0].Disconnect(false);
                activatedSockets[0].Close();
                activatedSockets.RemoveAt(0);
            }

            // un-subscribe the channels
            listenToChannel.python_cmd -= Call;

        }


        // call python cmd
        async void Call(string cmd, UnityAction<string> result)
        {
            Socket c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            if (listenToChannel.showLog)
                Debug.Log($"[UPython2] Connecting to {listenToChannel.host}:{listenToChannel.port} ...");

            c.Connect(listenToChannel.host, listenToChannel.port);

            activatedSockets.Add(c);

            while (!c.Connected)
                await Task.Yield();

            if (listenToChannel.showLog)
                Debug.Log("[UPython2] Connected!");

            if (listenToChannel.showLog)
                Debug.Log($"[UPython2] Send: {cmd}");


            byte[] bs = Encoding.UTF8.GetBytes(cmd);
            c.Send(bs);

            string recvStr = "";
            byte[] recvBytes = new byte[listenToChannel.dataBuffer];
            int bytes = 0;

            var task = new Task(() =>
            {
                try
                {
                    bytes = c.Receive(recvBytes, recvBytes.Length, 0);
                }
                catch (Exception e)
                {
                    if (listenToChannel.showLog)
                        Debug.Log(e);
                }

            }, taskCancelToken.Token);


            task.Start();

            await Task.WhenAll(task);


            c.Close();
            activatedSockets.Remove(c);

            if (listenToChannel.showLog)
                Debug.Log($"[UPython2] Disconnected with {listenToChannel.host}:{ listenToChannel.port}");


            recvStr += Encoding.UTF8.GetString(recvBytes, 0, bytes);

            if (recvStr.Length > 0 && result != null)
                result.Invoke(recvStr);

        }
    }
}

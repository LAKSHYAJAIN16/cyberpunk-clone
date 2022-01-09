using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
///
/// MIT license
/// Created by Haikun Huang
/// Date: 2021
/// </summary>
///


namespace DCXR
{
    [CreateAssetMenu(menuName ="DCXR/UPython2 Channel SO", fileName = "UPython2 Channel SO")]
    public class UPython2ChannelSO : ScriptableObject
    {

        public string host = "127.0.0.1";
        public int port = 8888;
        public int dataBuffer = 4096;
        public bool showLog = true;

        // Action
        public UnityAction<string, UnityAction<string>> python_cmd;

        public void Call(string cmd, UnityAction<string> result)
        {
            python_cmd.Invoke(cmd, result);
        }
    }
}
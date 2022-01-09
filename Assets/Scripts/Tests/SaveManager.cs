using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; set; }
    
    private void Start()
    {
        Instance = this;
    }

    public void SaveData(Data data)
    {
        BinaryFormatter Formatter = new BinaryFormatter(); 
    }
}

[System.Serializable]
public class Data
{
    private float[] positions = new float[3];

    public Data(float[] pos)
    {
        positions = pos;
    }
}

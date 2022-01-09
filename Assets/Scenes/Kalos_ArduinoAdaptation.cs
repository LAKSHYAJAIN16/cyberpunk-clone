using System;
using System.IO.Ports;
using Kalos.Arduino.Delegates;

namespace Kalos.Arduino
{
    public class ArduinoModule
    {
        //Variables
        public SerialPort serialPort { get; set; }
        public Int64 baudrate { get; set; }
        public string portname { get; set; }

        //Delegates
        public event ArduinoWriteSuccessHandler OnWriteSuccess;
        public event ArduinoWriteFailureHandler OnWriteFail;

        public ArduinoModule(int baudrate, string portname)
        {
            serialPort = new SerialPort(portname, baudrate);
            this.baudrate = baudrate;
            this.portname = portname;
            serialPort.Open();
        }

        public void Init(SerialDataReceivedEventHandler handler)
        {
            serialPort.DataReceived += handler;
        }

        public void Write(string text)
        {
            try
            {
                serialPort.Write(text);
                OnWriteSuccess?.Invoke(this, "Success!");
            }
            catch (Exception e)
            {
                OnWriteFail?.Invoke(this, e.Message);
            }
        }
        public void WriteLine(string text)
        {
            try
            {
                serialPort.WriteLine(text);
                OnWriteSuccess?.Invoke(this, "Success!");
            }
            catch (Exception e)
            {
                OnWriteFail?.Invoke(this, e.Message);
            }
        }

        public Int64 Baud
        {
            get
            {
                return baudrate;
            }
            set
            {
                baudrate = value;
            }
        }

        public string name
        {
            get
            {
                return portname;
            }
            set
            {
                portname = value;
            }
        }
    }
}

namespace Kalos.Arduino.Delegates
{
    public delegate void ArduinoWriteSuccessHandler(object sender, string text);
    public delegate void ArduinoWriteFailureHandler(object sender, string text);
}
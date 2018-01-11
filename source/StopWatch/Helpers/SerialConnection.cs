using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;



namespace StopWatch
{
    class SerialConnection
    {
        static SerialPort _serialPort;
        static Thread _readThread;
        static bool _continueReading;
        public Settings settings { get; private set; }

        public delegate void PositionEventDelegate(object sender, PositionEventArgs e);


        public event PositionEventDelegate PositionEvent;


        public SerialConnection(Settings settings)
        {
            this.settings = settings;
            _serialPort = null;
        }

        public bool isConnected()
        {
            if(_serialPort == null)
            {
                return false;
            }
            else
            {
                return _serialPort.IsOpen;
            }
        }

        
        public void initComPort()
        {
            if (settings.SelectedComPort.CompareTo("") != 0)
            {
                Debug.Write("connect to com-port: ");
                Debug.WriteLine(settings.SelectedComPort);
                // Create a new SerialPort object with default settings.
                _serialPort = new SerialPort();

                _readThread = new Thread(Read);

                _continueReading = true;

                // Allow the user to set the appropriate properties.
                _serialPort.PortName = settings.SelectedComPort;
                _serialPort.BaudRate = 115200;
                _serialPort.Parity = Parity.None;
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Handshake = Handshake.None;

                // Set the read/write timeouts
                _serialPort.ReadTimeout = 500;
                _serialPort.WriteTimeout = 500;

                try
                {
                    _serialPort.Open();
                    _readThread.Start();

                }
                catch(System.IO.IOException)
                {
                    Debug.WriteLine("could not connect to selected com port");
                }                                
            }
            else
            {
                Debug.WriteLine("no com port was set. No connection is started!");
            }
        }

        public void Disconnect()
        {
            //todo: diplay dissconnect on ui

                Debug.WriteLine("closing com port ");

            _continueReading = false;

            _readThread.Join();
            _serialPort.Close();
               
        }

        public void GetComPorts(List<string> portNamesList)
        {
            Debug.WriteLine("port Names: ");

            string[] portNamesStr;

            portNamesStr = SerialPort.GetPortNames();

            foreach (string portName in portNamesStr)
            {
                Debug.WriteLine(portName);
                portNamesList.Add(portName);
            }
        }

        public void Read()
        {
            while (_continueReading)
            {
                try
                {
                    String message = _serialPort.ReadLine();
                    Debug.Write("received Serial Data:");
                    Debug.WriteLine(message);

                    String[] substrings = message.Split(':');

                    if(substrings[0].CompareTo("position") == 0)
                    {
                        
                        double x = Convert.ToDouble(substrings[1]);
                        double y = Convert.ToDouble(substrings[2]);
                        double z = Convert.ToDouble(substrings[3]);
                        
                        PositionEventArgs e = new PositionEventArgs(x, y, z);


                        OnPositionEvent(e);
                    }
                }
                catch (TimeoutException) 
                { 
                    this.Disconnect();
                }
                catch (System.UnauthorizedAccessException)
                {
                    this.Disconnect();
                }
            }
        }
         
        public void OnPositionEvent(PositionEventArgs e)
        {
            // checks if the event has an subscriber
            if (PositionEvent != null)
                PositionEvent(this, e);
        }

    }




    public class PositionEventArgs : EventArgs
    {
        private double x = 0;
        private double y = 0;
        private double z = 0;
        
        // Constructor
        public PositionEventArgs(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public double xPosition
        {
            get { return x; }
            set { this.x = value; }
        }
        public double yPosition
        {
            get { return y; }
            set { this.y = value; }
        }
        public double zPosition
        {
            get { return z; }
            set { this.z = value; }
        }          
    }
}



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Timers;



namespace StopWatch
{
    class TcpConnection
    {
        TcpClient _client;
         System.Timers.Timer _t1;
        static Thread _readThread;        
        static bool _continueReading;
        public Settings settings { get; private set; }

        public delegate void PositionEventDelegate(object sender, PositionEventArgs e);


        public event PositionEventDelegate PositionEvent;


        public TcpConnection(Settings settings)
        {
            this.settings = settings;
            //_client = null;

            _client = new TcpClient();

            
            _t1 = new System.Timers.Timer(); // Timer anlegen

            _t1.Elapsed += new ElapsedEventHandler(OnTimedReconnectEvent);
            _t1.Interval = 2000;
            
        }

        private void OnTimedReconnectEvent(object source, ElapsedEventArgs e)
        {
            Console.WriteLine("try to connect to server ...");

            initTcpPort();
        }

        public bool isConnected()
        {
            if(_client == null)
            {
                return false;
            }
            else
            {
                bool connected;
                try
                {
                    connected = _client.Connected;
                }
                catch (System.NullReferenceException)
                {                                        
                    return false;
                }
                return connected;
            }
        }

        
        public void initTcpPort()
        {
            if ((settings.SelectedTcpPort != 0) & (!_client.Connected))
            {
                Debug.Write("connect to localhost on tcp-port: ");
                Debug.WriteLine(settings.SelectedTcpPort);
                // Create a new TCPConnection object on localhost
                var ipAddress = IPAddress.Parse("127.0.0.1");               
  
                try
                {
                    var result = _client.BeginConnect(ipAddress.ToString(), settings.SelectedTcpPort, null, null);

                    // give the client 5 seconds to connect
                    result.AsyncWaitHandle.WaitOne(5000);


                    if (!_client.Connected)
                    {
                        Debug.WriteLine("could not connect to selected tcp port");

                        _t1.Enabled = true;
                    }     
                    else
                    {
                        _t1.Enabled = false;

                         _readThread = new Thread(Read);

                        _continueReading = true;

                        _readThread.Start();
                    }
                    


                }
                catch(System.IO.IOException)
                {
                    Debug.WriteLine("could not connect to selected tcp port");
                    _t1.Enabled = true;
                }                                
            }
            else
            {
                Debug.WriteLine("no tcp port was set. No connection is started!");
                
            }
        }

        public void Disconnect()
        {
            //todo: diplay dissconnect on ui
            Debug.WriteLine("closing tcp-port ");

            _continueReading = false;
            try
            {
                _readThread.Join();
            }
            catch (Exception e)
            {
                Debug.WriteLine("execption while closing tcp-port: ");
                Debug.WriteLine(e.ToString());
            }

            _client.Close();

            //generate new client object for reconnect
            _client = new TcpClient();
        }
        
        public void Read()
        {
            NetworkStream stream = _client.GetStream();
                        
            stream.ReadTimeout = 100;

            string readStr = "";

            while (_continueReading)
            {

                if (stream.CanRead)
                {
                    byte[] myReadBuffer = new byte[1024];
                    StringBuilder myCompleteMessage = new StringBuilder();
                    int numberOfBytesRead = 0;

                    // Incoming message may be larger than the buffer size. 
                    while (stream.DataAvailable)
                    {
                        numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);

                        myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
                    }
                    
                    readStr = myCompleteMessage.ToString();
                }
                else
                {
                    Console.WriteLine("cannot read from the NetworkStream");
                }

            if(readStr.CompareTo("") == 0)
            {
                //wait for 2 seconds
                Thread.Sleep(2000);
                
                continue;
            }
                
                /*
                try
                {
                    string readStr;
                 
                    byte[] data = new byte[1024];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int numBytesRead;
                        while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
                        {
                            ms.Write(data, 0, numBytesRead);


                        }
                        readStr = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
                    }
                 */
                    
                    Debug.Write("received TCP Data:");
                    Debug.WriteLine(readStr);

                    String[] substrings = readStr.Split(':');

                    if(substrings.Length > 1)
                    {

                        if(substrings[1].CompareTo("position") == 0)
                        {
                        
                            double x = Convert.ToDouble(substrings[2]);
                            double y = Convert.ToDouble(substrings[3]);
                            double z = Convert.ToDouble(substrings[4]);
                        
                            PositionEventArgs e = new PositionEventArgs(x, y, z);

                            OnPositionEvent(e);
                        }
                    }

                /*
                }
                catch (TimeoutException) 
                { 
//                    this.Disconnect();
                }
                catch (System.UnauthorizedAccessException)
                {
                    this.Disconnect();
                }
                 */
            }

            stream.Close();
            stream.Flush();
        }
         
        public void OnPositionEvent(PositionEventArgs e)
        {
            // checks if the event has an subscriber
            if (PositionEvent != null)
                PositionEvent(this, e);
        }

    }



/* use declaration in SerialCOnenction
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
 */
}



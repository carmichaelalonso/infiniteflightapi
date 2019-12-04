using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfiniteFlight.ApiClient
{

    public class APICall
    {
        public int stateID { get; set; }

        public bool BoolValue { get; set; }

        public int IntValue { get; set; }

        public float FloatValue { get; set; }

        public double DoubleValue { get; set; }

        public string StringValue { get; set; }

        public long LongValue { get; set; }
    }

    public struct StateInfo
    {
        public string Path;
        public Type Type;
        public int ID;
    }

    public class State
    {
        public string Path { get; set; }
        public object Value { get; set; }
        public int ID { get; internal set; }
    }


    public class CommandInfo
    {
        public string Path { get; set; }
        public int ID { get; internal set; }
    }

    public class InfiniteFlightAPIClient
    {
        public const int CommandBase = 0x100000;

        public event EventHandler ManifestReceived = delegate { };
        public event EventHandler StateReceived = delegate { };

        private StateInfo[] stateInfo = null;

        public StateInfo[] StateInfo { get => stateInfo; set => stateInfo = value; }

        private Dictionary<int, StateInfo> StateInfoByID { get; set; } = new Dictionary<int, StateInfo>();

        private TcpClient client = new TcpClient();
        private NetworkStream networkStream { get; set; }


        ReaderWriterLockSlim apiCallQueueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private Queue<APICall> apiCallQueue = new Queue<APICall>();

        public List<State> States { get; set; } = new List<State>();

        public Dictionary<int, State> StateByID { get; set; } = new Dictionary<int, State>();


        public List<CommandInfo> Commands { get; set; } = new List<CommandInfo>();

        public void RefreshAllValues()
        {
            if (stateInfo != null)
            {
                foreach (var item in stateInfo)
                {
                    GetState(item.ID);
                }
            }
        }

        public void Connect(string host = "localhost", int port = 10112)
        {
            Console.WriteLine("Connecting to: {0}:{1}", host, port);

            try
            {
                client.Connect(host, port);
                client.NoDelay = true;

                this.networkStream = client.GetStream();

                Task.Run(() =>
                {

                    while (true)
                    {
                        string commandString = string.Empty;
                        try
                        {
                            ReadCommand(this.networkStream);                            
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                });

                Task.Run(() =>
                {

                    while (true)
                    {
                        if (stateInfo == null)
                        {
                            SendCommand(networkStream, -1); // request list until we get it... this is lame, but good for now
                        }
                        else
                        {
                            
                        }

                        Thread.Sleep(500);

                        //apiCallQueueLock.EnterReadLock();
                        //var pendingItems = apiCallQueue.Any();
                        //apiCallQueueLock.ExitReadLock();
                        //if (pendingItems)
                        //{
                        //    try
                        //    {
                        //        apiCallQueueLock.EnterWriteLock();
                        //        var apiCall = apiCallQueue.Dequeue();
                        //        apiCallQueueLock.ExitWriteLock();
                        //        if (apiCall != null)
                        //        {

                        //        }
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        Console.WriteLine("Error Sending Command: {0}", ex);
                        //    }
                        //}
                        //else
                        //{
                        //    Thread.Sleep(60);
                        //}
                    }
                });

            }
            catch (System.Net.Sockets.SocketException e)
            {

                Console.WriteLine("Caught exception: {0}", e);

            }

        }

        internal void RunCommand(int iD)
        {
            lock (this)
            {
                SendInt(this.networkStream, iD);
                SendBoolean(this.networkStream, true);
                //SendString(this.networkStream, aircraftID);
                //SendString(this.networkStream, liveryID);
                //SendDouble(this.networkStream, latitude);
                //SendDouble(this.networkStream, longitude);
            }
        }

        internal void AddAircraft(string aircraftID, string liveryID, double latitude, double longitude)
        {
            lock(this)
            {
                SendInt(this.networkStream, 0x100001);
                SendBoolean(this.networkStream, true);
                SendString(this.networkStream, aircraftID);
                SendString(this.networkStream, liveryID);
                SendDouble(this.networkStream, latitude);
                SendDouble(this.networkStream, longitude);
            }
        }
        internal void UpdateAirplanePosition(double latitude, double longitude,
            double altitude, double heading, double pitch, double roll)
        {
            lock (this)
            {
                SendInt(this.networkStream, 0x100002);
                SendBoolean(this.networkStream, true);
                SendDouble(this.networkStream, latitude);
                SendDouble(this.networkStream, longitude);
                SendDouble(this.networkStream, altitude);
                SendDouble(this.networkStream, heading);
                SendDouble(this.networkStream, pitch);
                SendDouble(this.networkStream, roll);
            }
        }

        internal void SetState(int commandID, bool value)
        {
            lock (this)
            {
                SendInt(this.networkStream, commandID);
                SendBoolean(this.networkStream, true); // set
                SendBoolean(networkStream, value);
            }
        }

        internal void SetState(int commandID, int value)
        {
            lock (this)
            {
                SendInt(this.networkStream, commandID);
                SendBoolean(this.networkStream, true); // set
                SendInt(networkStream, value);
            }
        }

        internal void SetState(int commandID, float value)
        {
            lock (this)
            {
                SendInt(this.networkStream, commandID);
                SendBoolean(this.networkStream, true); // set
                SendFloat(networkStream, value);
            }
        }

        internal void SetState(int commandID, string value)
        {
            lock (this)
            {
                SendInt(this.networkStream, commandID);
                SendBoolean(this.networkStream, true); // set
                SendString(networkStream, value);
            }
        }

        internal void SetState(int commandID, double value)
        {
            lock (this)
            {
                SendInt(this.networkStream, commandID);
                SendBoolean(this.networkStream, true); // set
                SendDouble(networkStream, value);
            }
        }

        internal void SetState(int commandID, long value)
        {
            lock (this)
            {
                SendInt(this.networkStream, commandID);
                SendBoolean(this.networkStream, true); // set
                SendLong(networkStream, value);
            }
        }
        public void GetState(int iD)
        {
            lock (this)
            {
                SendInt(this.networkStream, iD);
                SendBoolean(this.networkStream, false);
            }
        }

        private void SendCommand(NetworkStream networkStream, int commandID)
        {
            lock (this)
            {
                SendInt(networkStream, -1); // index of command list
                SendBoolean(networkStream, false);
                //SendString(networkStream, builder.ToString());
            }
        }

        private void SendInt(NetworkStream networkStream, int v)
        {
            var data = BitConverter.GetBytes(v);
            networkStream.Write(data, 0, 4);
        }

        private void SendBoolean(NetworkStream networkStream, bool v)
        {
            var data = BitConverter.GetBytes(v);
            networkStream.Write(data, 0, 1);
        }

        private void SendString(NetworkStream networkStream, string v)
        {
            var data = UTF8Encoding.UTF8.GetBytes(v);
            SendInt(networkStream, data.Length);
            networkStream.Write(data, 0, data.Length);
        }

        private void SendFloat(NetworkStream networkStream, float v)
        {
            var data = BitConverter.GetBytes(v);
            networkStream.Write(data, 0, 4);
        }

        private void SendDouble(NetworkStream networkStream, double v)
        {
            var data = BitConverter.GetBytes(v);
            networkStream.Write(data, 0, 8);
        }

        private void SendLong(NetworkStream networkStream, long v)
        {
            var data = BitConverter.GetBytes(v);
            networkStream.Write(data, 0, 8);
        }


        private Int32 ReadInt(NetworkStream networkStream)
        {
            byte[] data = new byte[4];
            networkStream.Read(data, 0, 4);
            return BitConverter.ToInt32(data, 0);
        }

        private double ReadDouble(NetworkStream networkStream)
        {
            byte[] data = new byte[8];
            networkStream.Read(data, 0, 8);
            return BitConverter.ToDouble(data, 0);
        }

        private float ReadFloat(NetworkStream networkStream)
        {
            byte[] data = new byte[4];
            networkStream.Read(data, 0, 4);
            return BitConverter.ToSingle(data, 0);
        }

        private long ReadLong(NetworkStream networkStream)
        {
            byte[] data = new byte[8];
            networkStream.Read(data, 0, 8);
            return BitConverter.ToInt64(data, 0);
        }

        private bool ReadBoolean(NetworkStream networkStream)
        {
            byte[] data = new byte[1];
            networkStream.Read(data, 0, 1);
            return BitConverter.ToBoolean(data, 0);
        }

        private string ReadString(NetworkStream networkStream)
        {
            var size = ReadInt(networkStream);

            byte[] data = new byte[size];
            var totalRead = 0;
            var sizeToRead = size;
            while (totalRead != size)
            {
                var read = networkStream.Read(data, totalRead, sizeToRead);
                Console.WriteLine("Read: {0} (out of {1})", read, sizeToRead);
                sizeToRead -= read;
                totalRead += read;
            }

            var str = UTF8Encoding.UTF8.GetString(data);

            Console.WriteLine("Received: {0}", str);

            return str;
        }

        private void ReadCommand(NetworkStream networkStream)
        {
            var commandID = ReadInt(networkStream);
            var dataLength = ReadInt(networkStream);

            if (commandID == -1)
            {
                //ReadManifest(networkStream);                
            }
            else
            {
                var stateInfo = StateInfoByID[commandID];
                var state = StateByID[commandID];

                if (stateInfo.Type == typeof(bool))
                {
                    var value = ReadBoolean(networkStream); // only double for now
                    Console.WriteLine("{0}: {1}", stateInfo.Path, value);
                    state.Value = value;
                }
                else if (stateInfo.Type == typeof(int))
                {
                    var value = ReadInt(networkStream); // only double for now
                    state.Value = value;
                }
                else if (stateInfo.Type == typeof(float))
                {
                    var value = ReadFloat(networkStream); // only double for now
                    state.Value = value;
                }
                else if (stateInfo.Type == typeof(double))
                {
                    var value = ReadDouble(networkStream); // only double for now
                    state.Value = value;
                }
                else if (stateInfo.Type == typeof(string))
                {
                    var value = ReadString(networkStream); // only double for now
                    state.Value = value;
                }
                else if (stateInfo.Type == typeof(long))
                {
                    var value = ReadLong(networkStream); // only double for now
                    state.Value = value;
                }

                StateReceived(commandID, EventArgs.Empty);
            }


            //var sizeToRead = ReadInt(NetworkStream);
            //var buffer = new byte[sizeToRead];
            //var offset = 0;

            //while (sizeToRead != 0)
            //{
            //    var read = NetworkStream.Read(buffer, offset, sizeToRead);
            //    offset += read;
            //    sizeToRead -= read;
            //}

            //string str = Encoding.UTF8.GetString(buffer);
            //return str;
        }

        private void ReadManifest(NetworkStream networkStream)
        {
            Console.WriteLine("Reading Manifest...");
            var str = ReadString(networkStream);

            var lines = str.Split('\n');

            Console.WriteLine("States: {0}", lines.Length);

            stateInfo = new StateInfo[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                var items = lines[i].Split(',');

                if (items.Length == 3)
                {
                    var stateID = Int32.Parse(items[0]);

                    if ((stateID & CommandBase) == CommandBase)
                    {
                        // store commands

                        Commands.Add(new CommandInfo { ID = stateID, Path = items[2] });
                    }
                    else
                    {
                        stateInfo[i] = new StateInfo { ID = stateID, Type = GetTypeFromIndex(Int32.Parse(items[1])), Path = items[2] };
                        StateInfoByID[stateID] = stateInfo[i];

                        var stateData = new State { Path = items[2], ID = stateID, Value = string.Empty };
                        States.Add(stateData);
                        StateByID[stateID] = stateData;
                    }
                }
            }

            ManifestReceived(this, EventArgs.Empty);

            // Once we get the manifest, refresh all values once.
            RefreshAllValues();
        }

        private void QueueCall(APICall call)
        {
            apiCallQueueLock.EnterWriteLock();
            apiCallQueue.Enqueue(call);
            apiCallQueueLock.ExitWriteLock();
        }

        public static short GetTypeIndex(Type type)
        {
            if (type == typeof(bool))
            {
                return 0;
            }
            else if (type == typeof(int))
            {
                return 1;
            }
            else if (type == typeof(float))
            {
                return 2;
            }
            else if (type == typeof(double))
            {
                return 3;
            }
            else if (type == typeof(string))
            {
                return 4;
            }
            else if (type == typeof(long))
            {
                return 5;
            }

            return -1;
        }

        public static Type GetTypeFromIndex(int index)
        {
            if (index == 0)
                return typeof(bool);
            if (index == 1)
                return typeof(int);
            if (index == 2)
                return typeof(float);
            if (index == 3)
                return typeof(double);
            if (index == 4)
                return typeof(string);
            if (index == 5)
                return typeof(long);

            return null;
        }
    }
}

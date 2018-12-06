using System;
using System.Windows.Forms;

using System.Net.Sockets;
using System.Net;

using ChatApplication;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ChatClient
{

    public partial class Client : Form
    {
        #region Private Members

        private String filePath = "COSC635_P2_DataSent.txt";
        //Set the size for the payload of the packet (message)
        private static int messageSize = 1000;
        //Sets the overall size for the packet in bytes
        private static int packetSize = 1024;

        private Random rn;
        private Stopwatch sw = new Stopwatch();
        private Stopwatch swe = new Stopwatch();
        private int userNumInput;
        //Counts for the total number of packets sent and lost
        private static int countTotalFinal;
        private static int countLostFinal;
        //Counter for the most recent sequence number received
        private static int currentSeqNum = 0;
        //Variable to hold the most recently received acknowledgement
        private int receivedAck;
        private int receivedSeq = -1;
        private int windowSize;
        private int algControl;
        private long executionTime;

        public event System.EventHandler AckChanged;
        public event System.EventHandler SeqChanged;

        protected virtual void OnAckChanged()
        {
            if (AckChanged != null) AckChanged(this, EventArgs.Empty);
        }

        protected virtual void OnSeqChanged()
        {
            if (SeqChanged != null) SeqChanged(this, EventArgs.Empty);
        }

        //Property for the acknowledgement with an event handler to detect changes made to the value.
        public int ReceivedAck
        {
            get { return receivedAck; }
            set
            {
                receivedAck = value;
                OnAckChanged();
            }
        }

        //Property for the sequence number with an event handler to detect changes made to the value.
        public int ReceivedSeq
        {
            get { return receivedSeq; }
            set
            {
                receivedSeq = value;
                OnSeqChanged();
            }
        }

        //Byte array to hold the data of the file
        private byte[] fileData;

        // Socket and server endpoint variables
        private Socket clientSocket;
        private EndPoint endPointServer;

        private byte[] dataStream = new byte[packetSize];

        private delegate void DisplayMessageDelegate(string message);
        private DisplayMessageDelegate displayMessageDelegate = null;

        #endregion

        #region Constructor

        public Client()
        {
            InitializeComponent();
            this.algSelector.Items.Add("Stop and Wait");
            this.algSelector.Items.Add("Go Back N");
            fileData = File.ReadAllBytes(filePath);
        }

        #endregion

        #region Events

        private void Client_Load(object sender, EventArgs e)
        {
            // Initialise delegate
            this.displayMessageDelegate = new DisplayMessageDelegate(this.DisplayMessage);
        }

        TaskCompletionSource<int> AckChangeReceived = new TaskCompletionSource<int>();
        TaskCompletionSource<int> SeqChangeReceived = new TaskCompletionSource<int>();

        private async void SendGBNAsync()
        {
            //Initialize packet counters and current sequence number to 0
            int countLost = 0;
            int countTotal = 0;
            currentSeqNum = 0;

            try
            {
                //Initialize the buffer to the expected length of a message (1000 bytes)
                byte[] buffer = new byte[messageSize];
                //Offset for copying sections from the file data
                int byteOffset = 0;
                //Create a new packet to store data for sending
                Packet[] window = new Packet[windowSize];

                while (byteOffset < fileData.Length)
                {
                    
                    byte[] byteData;

                    for (int i = 0; i < windowSize; i++)
                    {
                        
                        if (window[i] == null)
                        {
                            //Copy data from the file into the buffer to be loaded into the packet
                            int dataLength = Math.Min(buffer.Length, fileData.Length - byteOffset);
                            Buffer.BlockCopy(fileData, byteOffset, buffer, 0, dataLength);
                            //Adjust the offset to increment a chunk of 1000 bytes in the file data array
                            byteOffset += dataLength;

                            //Load the packet with contents from the buffer and the current sequence number
                            Packet sendData = new Packet();
                            sendData.Message = buffer;
                            sendData.SequenceNum = currentSeqNum.ToString();
                            sendData.ClientDataIdentifier = DataIdentifier.Message;

                            window[i] = sendData;

                            // Convert packet to byte array
                            byteData = sendData.GetDataStream();
                        }
                        else
                        {
                            byteData = window[i].GetDataStream();
                        }

                        // Send packet
                        countTotal++;
                        rn = new Random(DateTime.Now.Millisecond);
                        //Send packet if the random number is greater than the user threshold for packet loss
                        if (rn.Next(100) >= userNumInput)
                        {
                            
                            clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, endPointServer, new AsyncCallback(this.SendData), null);
                            SeqChangeReceived = new TaskCompletionSource<int>();
                            await SeqChangeReceived.Task;
                            if (!(Convert.ToInt32(window[i].SequenceNum) < currentSeqNum))
                            //Increment the current sequence number
                            currentSeqNum++;
                        }
                        else
                        {
                            if (!(Convert.ToInt32(window[i].SequenceNum) < currentSeqNum))
                                currentSeqNum++;
                            //Increment lost packet count
                            countLost++;
                        }
                        //Start a brief delay of 250ms to allow for synchronization between sender and receiver
                        sw.Reset();
                        sw.Start();
                        while (sw.IsRunning)
                        {
                            if (sw.ElapsedMilliseconds >= 250)
                            {
                                break;
                            }
                        }
                        sw.Stop();
                        
                    }
                    int rcvdSeq = ReceivedSeq;
                    int windowCount = 0;
                    bool lost = false;
                    int count = 0;
                    for(int i=0; i<windowSize; i++)
                    {
                        if ((Convert.ToInt32(window[i].SequenceNum) == rcvdSeq + 1 && lost == false) && !(rcvdSeq == -1))
                        {
                            window[windowCount] = window[i];
                            windowCount++;
                            lost = true;
                            count++;
                        }
                        else if (lost == true)
                        {
                            window[windowCount] = window[i];
                            windowCount++;
                            count++;
                        }
                        if(!(Convert.ToInt32(window[3].SequenceNum) - rcvdSeq == 4))
                        {
                            //Clear the packet at position i in the window
                            window[i] = null;
                            count++;
                        }
                        if (count == 0)
                        {
                            break;
                        }
                    }
                }

                //Send a final packet containing statistics data
                Packet finalPacket = new Packet();
                double percentageLost = (double)(((double)countLost / (double)countTotal) * 100);
                Console.WriteLine(percentageLost);
                finalPacket.Message = BitConverter.GetBytes(percentageLost);
                finalPacket.SequenceNum = currentSeqNum.ToString();
                finalPacket.ClientDataIdentifier = DataIdentifier.LogOut;

                statsOutput.Text += "Total Packets Sent: " + countTotal + "\n";
                statsOutput.Text += "Total Packets Lost: " + countLost + "\n";
                statsOutput.Text += "Percentage of Packets Lost: " + percentageLost + "\n";
                swe.Stop();
                executionTime = swe.ElapsedMilliseconds;
                statsOutput.Text += "Total Execution Time: " + executionTime;

                byte[] byteData2 = finalPacket.GetDataStream();

                // Send packet
                clientSocket.BeginSendTo(byteData2, 0, byteData2.Length, SocketFlags.None, endPointServer, new AsyncCallback(this.SendData), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Send Error: " + ex.Message, "UDP Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SendSAWAsync()
        {
            receivedSeq = 0;
            //Initialize packet counters and current sequence number to 0
            int countLost = 0;
            int countTotal = 0;
            currentSeqNum = 0;

            try
            {
                //Initialize the buffer to the expected length of a message (1000 bytes)
                byte[] buffer = new byte[messageSize];
                //Offset for copying sections from the file data
                int byteOffset = 0;
                //Create a new packet to store data for sending
                Packet sendData = new Packet();

                while (byteOffset < fileData.Length)
                {
                    //Copy data from the file into the buffer to be loaded into the packet
                    int dataLength = Math.Min(buffer.Length, fileData.Length - byteOffset);
                    Buffer.BlockCopy(fileData, byteOffset, buffer, 0, dataLength);
                    //Adjust the offset to increment a chunk of 1000 bytes in the file data array
                    byteOffset += dataLength;

                    //Load the packet with contents from the buffer and the current sequence number
                    sendData = new Packet();
                    sendData.Message = buffer;
                    sendData.SequenceNum = currentSeqNum.ToString();
                    sendData.ClientDataIdentifier = DataIdentifier.Message;

                    // Convert packet to byte array
                    byte[] byteData = sendData.GetDataStream();

                    // Send packet
                    while (ReceivedAck == 0)
                    {
                        countTotal++;
                        rn = new Random(DateTime.Now.Millisecond);
                        //Send packet if the random number is greater than the user threshold for packet loss
                        if (rn.Next(100) >= userNumInput) 
                        {
                            clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, endPointServer, new AsyncCallback(this.SendData), null);
                            AckChangeReceived = new TaskCompletionSource<int>();
                            await AckChangeReceived.Task;
                        }
                        else
                        {
                            //If packet is not sent, delay for 250ms to allow for server to catch up, then continue
                            sw.Reset();
                            sw.Start();
                            while (sw.IsRunning)
                            {
                                if (sw.ElapsedMilliseconds >= 250)
                                {
                                    break;
                                }
                            }
                            sw.Stop();
                            //Increment lost packet count
                            countLost++;
                        }
                        //Start a brief delay of 250ms to allow for synchronization between sender and receiver
                        sw.Reset();
                        sw.Start();
                        while (sw.IsRunning)
                        {
                            if (sw.ElapsedMilliseconds >= 250)
                            {
                                break;
                            }
                        }
                        sw.Stop();
                    }
                    //Reset the received acknowlegement to 0
                    ReceivedAck = 0;
                    //Increment the current sequence number
                    currentSeqNum++;
                }

                //Send a final packet containing statistics data
                sendData = new Packet();
                double percentageLost = (double)(((double)countLost / (double)countTotal) * 100);
                Console.WriteLine(percentageLost);
                sendData.Message = BitConverter.GetBytes(percentageLost);
                sendData.SequenceNum = currentSeqNum.ToString();
                sendData.ClientDataIdentifier = DataIdentifier.LogOut;

                statsOutput.Text += "Total Packets Sent: " + countTotal + "\n";
                statsOutput.Text += "Total Packets Lost: " + countLost + "\n";
                statsOutput.Text += "Percentage of Packets Lost: " + percentageLost + "\n";
                swe.Stop();
                executionTime = swe.ElapsedMilliseconds;
                statsOutput.Text += "Total Execution Time: " + executionTime;

                byte[] byteData2 = sendData.GetDataStream();

                // Send packet
                clientSocket.BeginSendTo(byteData2, 0, byteData2.Length, SocketFlags.None, endPointServer, new AsyncCallback(this.SendData), null);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Send Error: " + ex.Message, "UDP Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            swe.Reset();
            swe.Start();
            if (algControl == 0)
            { 
                SendSAWAsync();
            }
            else if(algControl == 1)
            {
                SendGBNAsync();
            }
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (this.clientSocket != null)
                {
                    // Initialise a packet object to store the data to be sent
                    Packet sendData = new Packet();
                    sendData.ClientDataIdentifier = DataIdentifier.LogOut;
                    sendData.Message = null;

                    // Get packet as byte array
                    byte[] byteData = sendData.GetDataStream();

                    // Send packet to the server
                    this.clientSocket.SendTo(byteData, 0, byteData.Length, SocketFlags.None, endPointServer);

                    this.clientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Closing Error: " + ex.Message, "UDP Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {

                // Initialise a packet object to store the data to be sent
                Packet sendData = new Packet();
                sendData.Message = null;
                sendData.ClientDataIdentifier = DataIdentifier.LogIn;

                this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPAddress serverIP = IPAddress.Parse(txtServerIP.Text.Trim());
                IPEndPoint server = new IPEndPoint(serverIP, 30000);
                endPointServer = (EndPoint)server;

                // Get packet as byte array
                byte[] data = sendData.GetDataStream();

                clientSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, endPointServer, new AsyncCallback(this.SendData), null);

                this.dataStream = new byte[packetSize];

                clientSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref endPointServer, new AsyncCallback(this.ReceiveData), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Error: " + ex.Message, "UDP Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion

        #region Send And Receive

        private void SendData(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Send Data: " + ex.Message, "UDP Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AckMethod()
        {
            ReceivedAck = 1;
            AckChangeReceived.SetResult(1);
        }

        private void SeqMethod(string str)
        {
            ReceivedSeq = Convert.ToInt32(str);
            SeqChangeReceived.SetResult(1);
        }

        private void ReceiveData(IAsyncResult ar)
        {
            try
            {
                // Receive all data
                this.clientSocket.EndReceive(ar);

                // Initialise a packet object to store the received data
                Packet receivedData = new Packet(this.dataStream);
                //Retrieve the acknowlegement value from the received packet
                
                if (algControl == 1 && receivedData.ClientDataIdentifier != DataIdentifier.LogOut)
                {
                    SeqMethod(receivedData.SequenceNum);
                }
                else if (Convert.ToInt32(receivedData.Acknowledgement) == 1)
                {
                    AckMethod();
                }
                // Update display
                if (receivedData.Message != null)
                    if(receivedData.Algorithm == "0")
                        this.Invoke(this.displayMessageDelegate, new object[] { System.Text.Encoding.Default.GetString(receivedData.Message) });

                // Reinitialize the datastream array for new packets
                this.dataStream = new byte[packetSize];

                // Continue listening for packets
                clientSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref endPointServer, new AsyncCallback(this.ReceiveData), null);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show("Receive Data: " + ex.Message, "UDP Client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Other Methods

        private void DisplayMessage(string messge)
        {
            rtxtConversation.Text += messge + Environment.NewLine;
        }

        #endregion

        private void numInput_ValueChanged(object sender, EventArgs e)
        {
            userNumInput = (int)numInput.Value;
        }

        private void algSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(algSelector.SelectedItem.Equals("Stop and Wait"))
            {
                algControl = 0;
            }
            else if (algSelector.SelectedItem.Equals("Go Back N"))
            {
                algControl = 1;
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            windowSize = (int)numericUpDown1.Value;
        }
    }

    internal class CustomEventArg : EventArgs
    {

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApplication
{

    public enum DataIdentifier
    {
        Message,
        LogIn,
        LogOut,
        Null
    }


    public class Packet
    {
        #region Private Members
        private DataIdentifier dataIdentifier;
        private string ack;
        private string algorithm;
        private string windowSize;
        private byte[] message;
        private string sequenceNum;
        private static int messageSize = 1000;
        private static int packetSize = 1024;
        #endregion

        #region Public Properties

        public DataIdentifier ClientDataIdentifier
        {
            get { return dataIdentifier; }
            set { dataIdentifier = value; }
        }

        public byte[] Message
        {
            get { return message; }
            set { message = value; }
        }

        public String SequenceNum
        {
            get { return sequenceNum; }
            set { sequenceNum = value; }
        }

        public String Acknowledgement
        {
            get { return ack; }
            set { ack = value; }
        }

        public String Algorithm
        {
            get { return algorithm; }
            set { algorithm = value; }
        }

        public String WindowSize
        {
            get { return windowSize; }
            set { windowSize = value; }
        }
        #endregion

        #region Methods

        // Default Constructor
        public Packet()
        {
            this.message = new byte[messageSize];
            this.ack = "0";
            this.algorithm = "0";
            this.windowSize = "0";
            this.sequenceNum = "0";
        }

        public Packet(byte[] dataStream)
        {
            this.dataIdentifier = (DataIdentifier)BitConverter.ToInt32(dataStream, 0);

            this.sequenceNum = BitConverter.ToInt32(dataStream, 4).ToString();

            this.ack = BitConverter.ToInt32(dataStream, 8).ToString();

            this.algorithm = BitConverter.ToInt32(dataStream, 12).ToString();

            this.windowSize = BitConverter.ToInt32(dataStream, 16).ToString();

            // Read the length of the message (4 bytes)
            int msgLength = BitConverter.ToInt32(dataStream, 20);

            // Read the message field
            if (msgLength > 0) {
                this.message = new byte[messageSize];
                Buffer.BlockCopy(dataStream, 24, message, 0, dataStream.Length - 24);
            }
            else
                this.message = null;
        }

        // Converts the packet into a byte array for sending/receiving 
        public byte[] GetDataStream()
        {
            List<byte> dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes((int)this.dataIdentifier));

            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(Convert.ToInt32(this.sequenceNum)));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(Convert.ToInt32(this.ack)));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(Convert.ToInt32(this.algorithm)));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(Convert.ToInt32(this.windowSize)));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            // Add the message length
            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(this.message.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            // Add the message
            if (this.message != null)
                dataStream.AddRange(this.message);

            return dataStream.ToArray();
        }

        #endregion
    }
}

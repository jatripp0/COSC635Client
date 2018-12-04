using System;
using System.Collections.Generic;
using System.Text;

namespace ChatApplication
{
    // ----------------
    // Packet Structure
    // ----------------

    // Description   -> |dataIdentifier|name length|message length|    name   |    message   |
    // Size in bytes -> |       4      |     4     |       4      |name length|message length|
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
        private string message;
        private string sequenceNum;
        #endregion

        #region Public Properties

        public DataIdentifier ClientDataIdentifier
        {
            get { return dataIdentifier; }
            set { dataIdentifier = value; }
        }

        public string Message
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
        }
        #endregion

        #region Methods

        // Default Constructor
        public Packet()
        {
            this.message = null;
            this.ack = "0";
            this.algorithm = "0";
            this.windowSize = "0";
            this.sequenceNum = "0";
        }

        public Packet(byte[] dataStream)
        {
            this.dataIdentifier = (DataIdentifier)BitConverter.ToInt32(dataStream, 0);

            int sequenceNum = BitConverter.ToInt32(dataStream, 4);

            int ack = BitConverter.ToInt32(dataStream, 8);

            int algorithm = BitConverter.ToInt32(dataStream, 12);

            int windowSize = BitConverter.ToInt32(dataStream, 16);

            // Read the length of the message (4 bytes)
            int msgLength = BitConverter.ToInt32(dataStream, 20);

            // Read the message field
            if (msgLength > 0)
                this.message = Encoding.UTF8.GetString(dataStream, 24, msgLength);
            else
                this.message = null;
        }

        // Converts the packet into a byte array for sending/receiving 
        public byte[] GetDataStream()
        {
            List<byte> dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes((int)this.dataIdentifier));

            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(this.sequenceNum.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(this.ack.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(this.algorithm.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(this.windowSize.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            // Add the message length
            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(this.message.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            // Add the message
            if (this.message != null)
                dataStream.AddRange(Encoding.UTF8.GetBytes(this.message));

            return dataStream.ToArray();
        }

        #endregion
    }
}

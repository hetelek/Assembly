using System;
using System.IO;

namespace XBDMCommunicator
{
    public abstract class XbdmMemoryStream : Stream
    {
        // IO Functions
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return 0x100000000; }
        }

        public override sealed long Position { get; set; }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
        }
    }
}

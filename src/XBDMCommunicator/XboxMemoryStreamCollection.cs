using System;
using System.Collections.Generic;
using System.IO;

namespace XBDMCommunicator
{
    public class XboxMemoryStreamCollection : XbdmMemoryStream
    {
        // Private Modifiers
        private readonly List<XbdmDevice> _xbdmDevices;
        public XboxMemoryStreamCollection(XbdmDevice[] xbdmDevices)
        {
            _xbdmDevices = new List<XbdmDevice>(xbdmDevices);
        }

        public XboxMemoryStreamCollection(List<XbdmDevice> xbdmDevices)
        {
            _xbdmDevices = xbdmDevices;
        }

        // IO Functions
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Cannot read from multiple streams.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            foreach (XbdmDevice xbdmDevice in _xbdmDevices)
                Position = xbdmDevice.MemoryStream.Seek(offset, origin);

            return Position;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            foreach (XbdmDevice xbdmDevice in _xbdmDevices)
            {
                xbdmDevice.MemoryStream.Write(buffer, offset, count);
                Position = xbdmDevice.MemoryStream.Position;
            }
        }
    }
}

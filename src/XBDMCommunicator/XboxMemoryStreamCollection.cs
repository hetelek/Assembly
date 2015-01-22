using System;
using System.Collections.Generic;
using System.IO;

namespace XBDMCommunicator
{
    public class XboxMemoryStreamCollection : XbdmMemoryStream
    {
        // Private Modifiers
        private readonly List<IXbdm> _xbdmDevices;
        public XboxMemoryStreamCollection(IXbdm[] xbdmDevices)
        {
            _xbdmDevices = new List<IXbdm>(xbdmDevices);
        }

        public XboxMemoryStreamCollection(List<IXbdm> xbdmDevices)
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
            long position = Position;
            foreach (IXbdm xbdmDevice in _xbdmDevices)
                position = xbdmDevice.MemoryStream.Seek(offset, origin);

            return position;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            foreach (IXbdm xbdmDevice in _xbdmDevices)
                xbdmDevice.MemoryStream.Write(buffer, offset, count);
        }
    }
}

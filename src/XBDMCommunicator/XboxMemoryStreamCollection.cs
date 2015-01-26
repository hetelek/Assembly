using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

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

        public async override void Write(byte[] buffer, int offset, int count)
        {
            IEnumerable<Task> tasks = _xbdmDevices.Select(device =>
            {
                Task t = device.MemoryStream.WriteAsync(buffer, offset, count);
                Position = device.MemoryStream.Position;
                return t;
            });
            await Task.WhenAll(tasks);
        }
    }
}

using System;
using System.IO;

namespace XBDMCommunicator
{
    // Memory IO
    public class XboxMemoryStream : XbdmMemoryStream
    {
        // Private Modifiers
        private readonly XbdmDevice _xbdm;

        public XboxMemoryStream(XbdmDevice xbdm)
        {
            _xbdm = xbdm;
            Position = 0;
        }

        // IO Functions
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_xbdm.Connect())
                return 0;

            bool alreadyStopped = true;
            if (count > 20)
                _xbdm._xboxDebugTarget.Stop(out alreadyStopped);

            uint bytesRead;
            if (offset == 0)
            {
                _xbdm._xboxDebugTarget.GetMemory((uint)Position, (uint)count, buffer, out bytesRead);
            }
            else
            {
                // Offset isn't 0, so read into a temp buffer and then copy it into the output
                var tempBuffer = new byte[count];
                _xbdm._xboxDebugTarget.GetMemory((uint)Position, (uint)count, tempBuffer, out bytesRead);
                Buffer.BlockCopy(tempBuffer, 0, buffer, offset, count);
            }
            Position += bytesRead;

            if (!alreadyStopped)
                _xbdm._xboxDebugTarget.Go(out alreadyStopped);

            return (int)bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;

                case SeekOrigin.Current:
                    Position += offset;
                    break;

                case SeekOrigin.End:
                    Position = 0x100000000 - offset;
                    break;
            }
            return Position;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_xbdm.Connect())
                return;

            bool alreadyStopped = true;
            if (count > 20)
                _xbdm._xboxDebugTarget.Stop(out alreadyStopped);

            byte[] pokeArray = buffer;
            if (offset != 0)
            {
                // Offset isn't 0, so copy into a second buffer before poking
                pokeArray = new byte[count];
                Buffer.BlockCopy(buffer, offset, pokeArray, 0, count);
            }

            uint bytesWritten;
            _xbdm._xboxDebugTarget.SetMemory((uint)Position, (uint)count, pokeArray, out bytesWritten);
            Position += bytesWritten;

            if (!alreadyStopped)
                _xbdm._xboxDebugTarget.Go(out alreadyStopped);
        }
    }
}

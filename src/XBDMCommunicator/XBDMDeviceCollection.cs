using System;
using System.Collections.Generic;
using System.IO;

namespace XBDMCommunicator
{
    public class XbdmDeviceCollection : IXbdm
    {
        private List<IXbdm> _xbdmDevices = new List<IXbdm>();

        public XbdmDeviceCollection(string deviceIdents, bool openConnection = false)
        {
            DeviceIdent = deviceIdents;

            string[] splitDeviceIdents = deviceIdents.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < splitDeviceIdents.Length; i++)
                _xbdmDevices.Add(new XbdmDevice(splitDeviceIdents[i], openConnection));
        }

        public XbdmMemoryStream MemoryStream
        {
            get
            {
                return new XboxMemoryStreamCollection(_xbdmDevices);
            }
            //set { _xboxMemoryStream = value; }
        }

        public string DeviceIdent { get; private set; }
        public string XboxType
        {
            get
            {
                if (_xbdmDevices.Count < 1)
                    return String.Empty;

                return _xbdmDevices[0].XboxType;
            }
        }

        public bool IsConnected
        {
            get
            {
                foreach (IXbdm xbdmDevice in _xbdmDevices)
                    if (!xbdmDevice.IsConnected)
                        return false;
                return true;
            }
        }

        public void UpdateDeviceIdent(string deviceIdents)
        {
            DeviceIdent = deviceIdents;
            List<string> newSplitDeviceIdents = new List<string>(deviceIdents.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            // disconnect and remove any devices not included
            for (int i = 0; i < _xbdmDevices.Count; i++)
            {
                IXbdm xbdmDevice = _xbdmDevices[i];

                if (!newSplitDeviceIdents.Contains(xbdmDevice.DeviceIdent))
                {
                    xbdmDevice.Disconnect();
                    _xbdmDevices.RemoveAt(i--);
                }
                else
                    newSplitDeviceIdents.Remove(xbdmDevice.DeviceIdent);
            }

            // add any new devices
            foreach (string newSplitDeviceIdent in newSplitDeviceIdents)
                _xbdmDevices.Add(new XbdmDevice(newSplitDeviceIdent));
        }

        public string SendStringCommand(string command)
        {
            if (_xbdmDevices.Count < 1)
                return String.Empty;

            string[] responses = new string[_xbdmDevices.Count];
            for (int i = 0; i < responses.Length; i++)
                responses[i] = _xbdmDevices[i].SendStringCommand(command);

            return responses[0];
        }

        public bool Connect()
        {
            bool connectedToAll = true;
            
            foreach (IXbdm xbdmDevice in _xbdmDevices)
                if (!xbdmDevice.Connect())
                    connectedToAll = false;

            return connectedToAll;
        }

        public void Disconnect()
        {
            foreach (IXbdm xbdmDevice in _xbdmDevices)
                xbdmDevice.Disconnect();
        }

        public void Freeze()
        {
            foreach (IXbdm xbdmDevice in _xbdmDevices)
                xbdmDevice.Freeze();
        }

        public void Unfreeze()
        {
            foreach (IXbdm xbdmDevice in _xbdmDevices)
                xbdmDevice.Unfreeze();
        }

        public void Reboot(RebootType rebootType)
        {
            foreach (IXbdm xbdmDevice in _xbdmDevices)
                xbdmDevice.Reboot(rebootType);
        }

        public void Shutdown()
        {
            foreach (IXbdm xbdmDevice in _xbdmDevices)
                xbdmDevice.Shutdown();
        }

        public bool GetScreenshot(string savePath, bool freezeDuring = false)
        {
            if (_xbdmDevices.Count < 1)
                return false;

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            foreach (IXbdm xbdmDevice in _xbdmDevices)
            {
                string path = Path.Combine(new string[] {desktop, xbdmDevice.DeviceIdent + ".png" });
                xbdmDevice.GetScreenshot(path);
            }

            return _xbdmDevices[0].GetScreenshot(savePath, freezeDuring);
        }
    }
}

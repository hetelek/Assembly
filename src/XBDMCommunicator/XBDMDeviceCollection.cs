using System;
using System.Collections.Generic;
using System.IO;

namespace XBDMCommunicator
{
    public class XbdmDeviceCollection : IXbdm
    {
        public XbdmDeviceCollection(string deviceIdents, bool openConnection = false)
        {
            XbdmDevices = new List<XbdmDevice>();
            DeviceIdent = deviceIdents;

            string[] splitDeviceIdents = deviceIdents.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < splitDeviceIdents.Length; i++)
                XbdmDevices.Add(new XbdmDevice(splitDeviceIdents[i], openConnection));
        }

        public XbdmMemoryStream MemoryStream
        {
            get
            {
                return new XboxMemoryStreamCollection(XbdmDevices);
            }
            //set { _xboxMemoryStream = value; }
        }

        public List<XbdmDevice> XbdmDevices { get; private set; }
        public string DeviceIdent { get; private set; }
        public string XboxType
        {
            get
            {
                if (XbdmDevices.Count < 1)
                    return String.Empty;

                return XbdmDevices[0].XboxType;
            }
        }

        public bool IsConnected
        {
            get
            {
                foreach (IXbdm xbdmDevice in XbdmDevices)
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
            for (int i = 0; i < XbdmDevices.Count; i++)
            {
                IXbdm xbdmDevice = XbdmDevices[i];

                if (!newSplitDeviceIdents.Contains(xbdmDevice.DeviceIdent))
                {
                    xbdmDevice.Disconnect();
                    XbdmDevices.RemoveAt(i--);
                }
                else
                    newSplitDeviceIdents.Remove(xbdmDevice.DeviceIdent);
            }

            // add any new devices
            foreach (string newSplitDeviceIdent in newSplitDeviceIdents)
                XbdmDevices.Add(new XbdmDevice(newSplitDeviceIdent));
        }

        public string SendStringCommand(string command)
        {
            if (XbdmDevices.Count < 1)
                return String.Empty;

            string[] responses = new string[XbdmDevices.Count];
            for (int i = 0; i < responses.Length; i++)
                responses[i] = XbdmDevices[i].SendStringCommand(command);

            return responses[0];
        }

        public bool Connect()
        {
            bool connectedToAll = true;
            
            foreach (IXbdm xbdmDevice in XbdmDevices)
                if (!xbdmDevice.Connect())
                    connectedToAll = false;

            return connectedToAll;
        }

        public void Disconnect()
        {
            foreach (IXbdm xbdmDevice in XbdmDevices)
                xbdmDevice.Disconnect();
        }

        public void Freeze()
        {
            foreach (IXbdm xbdmDevice in XbdmDevices)
                xbdmDevice.Freeze();
        }

        public void Unfreeze()
        {
            foreach (IXbdm xbdmDevice in XbdmDevices)
                xbdmDevice.Unfreeze();
        }

        public void Reboot(RebootType rebootType)
        {
            foreach (IXbdm xbdmDevice in XbdmDevices)
                xbdmDevice.Reboot(rebootType);
        }

        public void Shutdown()
        {
            foreach (IXbdm xbdmDevice in XbdmDevices)
                xbdmDevice.Shutdown();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

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
			{
				string trimmedSplitDeviceIdent = splitDeviceIdents[i].Trim();
				if (trimmedSplitDeviceIdent != String.Empty)
					XbdmDevices.Add(new XbdmDevice(trimmedSplitDeviceIdent, openConnection));
			}
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

        public async Task<string> SendStringCommandAsync(string command)
        {
            IEnumerable<Task<string>> tasks = XbdmDevices.Select(device =>
            {
                Task<string> t = Task.Run(() => device.SendStringCommand(command));
                return t;
            });
            string[] result = await Task.WhenAll(tasks);

            if (result.Length < 1)
                return String.Empty;
            return result[0];
        }

        public string SendStringCommand(string command)
        {
            return SendStringCommandAsync(command).Result;
        }

        public async Task<bool> ConnectAsync()
        {
            IEnumerable<Task<bool>> tasks = XbdmDevices.Select(device =>
            {
                Task<bool> t = Task.Run(() => device.Connect());
                return t;
            });
            bool[] result = await Task.WhenAll(tasks);

            return result.Count(i => i == false) == 0;
        }

        public bool Connect()
        {
            return ConnectAsync().Result;
        }

        public async void Disconnect()
        {
            IEnumerable<Task> tasks = XbdmDevices.Select(device =>
            {
                Task t = Task.Run(() => device.Disconnect());
                return t;
            });
            await Task.WhenAll(tasks);
        }

        public async void Freeze()
        {
            IEnumerable<Task> tasks = XbdmDevices.Select(device =>
            {
                Task t = Task.Run(() => device.Freeze());
                return t;
            });
            await Task.WhenAll(tasks);
        }

        public async void Unfreeze()
        {
            IEnumerable<Task> tasks = XbdmDevices.Select(device =>
            {
                Task t = Task.Run(() => device.Unfreeze());
                return t;
            });
            await Task.WhenAll(tasks);
        }

        public async void Reboot(RebootType rebootType)
        {
            IEnumerable<Task> tasks = XbdmDevices.Select(device =>
            {
                Task t = Task.Run(() => device.Reboot(rebootType));
                return t;
            });
            await Task.WhenAll(tasks);
        }

        public async void Shutdown()
        {
            IEnumerable<Task> tasks = XbdmDevices.Select(device =>
            {
                Task t = Task.Run(() => device.Shutdown());
                return t;
            });
            await Task.WhenAll(tasks);
        }
    }
}

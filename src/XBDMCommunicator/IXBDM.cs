using System;

namespace XBDMCommunicator
{
    public enum RebootType
    {
        Cold,
        Title
    }

    public interface IXbdm
    {
        XbdmMemoryStream MemoryStream { get; }

        string DeviceIdent { get; }
        string XboxType { get; }
        bool IsConnected { get; }

        void UpdateDeviceIdent(string deviceIdent);
        string SendStringCommand(string command);

        bool Connect();
        void Disconnect();
		
        void Freeze();
		void Unfreeze();
		
        void Reboot(RebootType rebootType);
		void Shutdown();

        bool GetScreenshot(string savePath, bool freezeDuring = false);
    }
}

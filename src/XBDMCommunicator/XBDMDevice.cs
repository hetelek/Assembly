using System;
using System.IO;
using System.Threading.Tasks;
using XDevkit;

namespace XBDMCommunicator
{
    public class XbdmDevice : IXbdm
	{
		private readonly XbdmMemoryStream _xboxMemoryStream;
		private uint _xboxConnectionCode;
		private XboxConsole _xboxConsole;
		private IXboxManager _xboxManager;

        internal IXboxDebugTarget _xboxDebugTarget;

		/// <summary>
		///     Create a new instance of the XBDM Communicator
		/// </summary>
		/// <param name="deviceIdent">The Name of IP of the XBox Console running xbdm.</param>
		/// <param name="openConnection">Open a connection to the XBox Console</param>
		public XbdmDevice(string deviceIdent, bool openConnection = false)
		{
			DeviceIdent = deviceIdent;
			_xboxMemoryStream = new XboxMemoryStream(this);

			if (openConnection)
				Connect();
		}

		// Public Modifiers
		public string DeviceIdent { get; private set; }
		public string XboxType { get; private set; }
		public bool IsConnected { get; private set; }

		public XbdmMemoryStream MemoryStream
		{
			get { return _xboxMemoryStream; }
			//set { _xboxMemoryStream = value; }
		}

		// Public Functions
		/// <summary>
		///     Update the Xbox Device Ident (XDK Name or IP)
		/// </summary>
		/// <param name="deviceIdent">The new XBox XDK Name or IP</param>
		public void UpdateDeviceIdent(string deviceIdent)
		{
			if (DeviceIdent != deviceIdent)
				Disconnect();
			DeviceIdent = deviceIdent;
		}

		/// <summary>
		///     Open a connection to the XBox Console
		/// </summary>
		/// <returns>true if the connection was successful.</returns>
		public bool Connect()
		{
			if (!IsConnected)
			{
				try
				{
					_xboxManager = new XboxManager();
					_xboxConsole = _xboxManager.OpenConsole(DeviceIdent);
					_xboxDebugTarget = _xboxConsole.DebugTarget;
					_xboxConnectionCode = _xboxConsole.OpenConnection(null);
				}
				catch
				{
					_xboxManager = null;
					_xboxConsole = null;
					_xboxDebugTarget = null;
					return false;
				}

				try
				{
					XboxType = _xboxConsole.ConsoleType.ToString();
				}
				catch
				{
					XboxType = "Unable to get.";
				}

				IsConnected = true;
			}
			return true;
		}

		/// <summary>
		///     Close the connection to the XBox Console
		/// </summary>
		public void Disconnect()
		{
			if (!IsConnected) return;

			if (_xboxConsole != null)
				_xboxConsole.CloseConnection(_xboxConnectionCode);

			_xboxManager = null;
			_xboxDebugTarget = null;
			_xboxConsole = null;
			IsConnected = false;
		}

		/// <summary>
		///     Send a string-based command, such as "bye", "reboot", "go", "stop"
		/// </summary>
		/// <param name="command">The command to send.</param>
		/// <returns>The responce from the console, or null if sending the command failed.</returns>
		public string SendStringCommand(string command)
		{
			if (!Connect())
				return null;

			string response;
			_xboxConsole.SendTextCommand(_xboxConnectionCode, command, out response);
			//if (!(response.Contains("202") | response.Contains("203")))
			return response;
			/*else
                throw new Exception("String command wasn't accepted by the Xbox 360 Console. It might not be valid or the Xbox is just being annoying. The response was:\n" + response);*/
		}

		/// <summary>
		///     Freeze the XBox Console
		/// </summary>
		public void Freeze()
		{
			if (!Connect())
				return;

			SendStringCommand("stop");
		}

		/// <summary>
		///     UnFreeze the XBox Console
		/// </summary>
		public void Unfreeze()
		{
			if (!Connect())
				return;

			SendStringCommand("go");
		}

		/// <summary>
		///     Reboot the XBox Console
		/// </summary>
		/// <param name="rebootType">The type of Reboot to do (Cold or Title)</param>
		public void Reboot(RebootType rebootType)
		{
			if (!Connect())
				return;

			switch (rebootType)
			{
				case RebootType.Cold:
					SendStringCommand("reboot");
					break;
				case RebootType.Title:
					SendStringCommand("reboot");
					break;
                case RebootType.ActiveTitle:
                    XBOX_PROCESS_INFO info = _xboxConsole.RunningProcessInfo;
                    string executablePath = info.ProgramName;
                    string executableDirectory = Directory.GetParent(executablePath).FullName;
                    SendStringCommand(String.Format("magicboot title=\"{0}\" directory=\"{1}\"", executablePath, executableDirectory));
                    break;

			}
		}

		/// <summary>
		///     Shutdown the XBox Console
		/// </summary>
		public void Shutdown()
		{
			if (!Connect())
				return;

			// Tell console to go bye-bye
			SendStringCommand("bye");

			Disconnect();
		}

		/// <summary>
		///     Save a screenshot from the XBox Console
		/// </summary>
		/// <param name="savePath">The location to save the image to.</param>
		/// <param name="freezeDuring">Do you want to freeze while the screenshot is being taken.</param>
		public bool GetScreenshot(string savePath, bool freezeDuring = false)
		{
			if (!Connect())
				return false;

			// Stop the Console
			if (freezeDuring)
				Freeze();

			// Screensnap that console
			_xboxConsole.ScreenShot(savePath);

			// Start the Console
			if (freezeDuring)
				Unfreeze();

			return true;
		}

        /// <summary>
        ///     Save a screenshot from the XBox Console asynchronously
        /// </summary>
        /// <param name="savePath">The location to save the image to.</param>
        /// <param name="freezeDuring">Do you want to freeze while the screenshot is being taken.</param>
        public async Task<bool> GetScreenshotAsync(string savePath, bool freezeDuring = false)
        {
            return await Task.Run(() =>
            {
                return GetScreenshot(savePath, freezeDuring);
            });
        }
	}
}
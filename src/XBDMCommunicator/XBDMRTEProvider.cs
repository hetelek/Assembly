﻿using Blamite.Blam;
using Blamite.IO;
using Blamite.RTE;

namespace XBDMCommunicator
{
	/// <summary>
	///     An XBDM real-time editing provider.
	/// </summary>
	public class XBDMRTEProvider : IRTEProvider
	{
		private readonly IXbdm _xbdm;

		/// <summary>
		///     Constructs a new XBDMRTEProvider based off of an Xbdm object.
		/// </summary>
		/// <param name="xbdm">The Xbdm object to use to connect to the console.</param>
        public XBDMRTEProvider(IXbdm xbdm)
		{
			_xbdm = xbdm;
		}

		/// <summary>
		///     The type of connection that the provider will establish.
		/// </summary>
		public RTEConnectionType ConnectionType
		{
			get { return RTEConnectionType.ConsoleX360; }
		}

		/// <summary>
		///     Obtains a stream which can be used to read and write a cache file's meta in realtime.
		///     The stream will be set up such that offsets in the stream correspond to meta pointers in the cache file.
		/// </summary>
		/// <param name="cacheFile">The cache file to get a stream for.</param>
		/// <returns>The stream if it was opened successfully, or null otherwise.</returns>
		public IStream GetMetaStream(ICacheFile cacheFile)
		{
			// Okay, so technically we should be checking to see if the cache file is actually loaded into memory first
			// But that's kinda hard to do...
			return _xbdm.Connect() ? new EndianStream(_xbdm.MemoryStream, Endian.BigEndian) : null;
		}
	}
}
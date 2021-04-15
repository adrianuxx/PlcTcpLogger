using System;

namespace TgwPlcTcpLogger
{
	public struct plc
	{
		public string plcName;

		public string plcIpAdress;

		public int plcPort;

		public ALFTcpClient plcTcpClient;
	}
}
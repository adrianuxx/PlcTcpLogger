using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace TgwPlcTcpLogger
{
	public class ALFTcpClient
	{
		private TcpClient _tcpClient = new TcpClient();

		private NetworkStream _clientStream;

		private readonly ASCIIEncoding _encoder;

		private readonly ManualResetEvent _timeoutObject = new ManualResetEvent(false);

		public bool Connected
		{
			get;
			set;
		}

		public string ConnectionName
		{
			get;
			private set;
		}

		public bool LogTcpStream
		{
			get;
			set;
		}

		public Exception SocketException
		{
			get;
			private set;
		}

		public ALFTcpClient(string connectionName)
		{
			this.ConnectionName = connectionName;
			this.Connected = false;
			this._encoder = new ASCIIEncoding();
		}

		private void AsyncConnectCallback(IAsyncResult ar)
		{
			try
			{
				try
				{
					this.Connected = false;
					TcpClient asyncState = ar.AsyncState as TcpClient;
					if (asyncState != null)
					{
						asyncState.EndConnect(ar);
						this.Connected = asyncState.Connected;
					}
					else
					{
						return;
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					this.Connected = false;
					this.SocketException = exception;
				}
			}
			finally
			{
				this._timeoutObject.Set();
			}
		}

		public bool Connect(string ipAddress, int port)
		{
			bool connected;
			this.Disconnect();
			this._tcpClient = new TcpClient();
			this.SocketException = new TimeoutException("Connection Timeout occurred");
			this._tcpClient.BeginConnect(ipAddress, port, new AsyncCallback(this.AsyncConnectCallback), this._tcpClient);
			if (!this._timeoutObject.WaitOne(1000, false))
			{
				this.Connected = false;
			}
			else
			{
				this._tcpClient.ReceiveBufferSize = 65535;
				this._tcpClient.SendBufferSize = 1024;
				this._tcpClient.NoDelay = true;
				this.Connected = this._tcpClient.Connected;
			}
			if (this.Connected)
			{
				Thread thread = new Thread(new ParameterizedThreadStart(this.HandleClientComm));
				thread.Start(this._tcpClient);
				this._clientStream = this._tcpClient.GetStream();
				connected = this.Connected;
			}
			else
			{
				connected = this.Connected;
			}
			return connected;
		}

		public void Disconnect()
		{
			try
			{
				this._tcpClient.Client.Close();
				this._tcpClient.Close();
			}
			catch (Exception exception)
			{
				this.SocketException = exception;
			}
		}

		private void HandleClientComm(object client)
		{
			int num;
			TcpClient tcpClient = (TcpClient)client;
			NetworkStream stream = tcpClient.GetStream();
			byte[] numArray = new byte[tcpClient.ReceiveBufferSize];
			while (true)
			{
				try
				{
					num = stream.Read(numArray, 0, tcpClient.ReceiveBufferSize);
				}
				catch (Exception exception)
				{
					break;
				}
				if (num != 0)
				{
					string str = (new ASCIIEncoding()).GetString(numArray, 0, num);
					str = str.Replace(Convert.ToString('\0'), "");
					string[] strArrays = str.Split(new char[] { '\n' });
					for (int i = 0; i < (int)strArrays.Length; i++)
					{
						strArrays[i] = strArrays[i].Replace("\n", "");
						strArrays[i] = strArrays[i].Replace("\r", "");
						string str1 = strArrays[i];
						this.MessageReceived(tcpClient, str1);
					}
					Thread.Sleep(2);
				}
				else
				{
					break;
				}
			}
		}

		public void Send(string telegramToSend)
		{
			telegramToSend = string.Concat(telegramToSend, "\r\n");
			try
			{
				if (this.Connected)
				{
					byte[] bytes = this._encoder.GetBytes(telegramToSend);
					this._clientStream.Write(bytes, 0, (int)bytes.Length);
					this._clientStream.Flush();
				}
			}
			catch (Exception exception)
			{
				this.Connected = false;
			}
		}

		public event ALFTcpClient.MessageReceivedEventHandler MessageReceived;

		public delegate void MessageReceivedEventHandler(TcpClient tcpClient, string messageString);
	}
}
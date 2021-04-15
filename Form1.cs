using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace TgwPlcTcpLogger
{
	public class Form1 : Form
	{
		private TcpClient conn1 = new TcpClient();

		private List<plc> myPlcs = new List<plc>(200);

        private List<string> myLogPaths = new List<string>(200);
        private DateTime ActualDate = DateTime.Today;
        private List<KeyValuePair<string,string>> mylogfilenames = new List<KeyValuePair<string, string>>(200);

        private List<string> LogSorter = new List<string>(100);

		private System.Windows.Forms.Timer recheckTimer = new System.Windows.Forms.Timer();

		private System.Windows.Forms.Timer reConnectTimer = new System.Windows.Forms.Timer();

		private string credits = "Loging Tool Modified in 2021. (Respect sintaxis in Config file)\r\n";

		private string errorMessage = "";

		private IContainer components = null;

		private TextBox textBox1;

		private plc aLFTcpClient = new plc();
        private List<StreamWriter> _streamWriter = new List<StreamWriter>(200);

        public Form1()
		{

			string[] allKeys = ConfigurationManager.AppSettings.AllKeys;
			for (int i = 0; i < (int)allKeys.Length; i++)
			{
				string str = allKeys[i];
				if (str.ToString().StartsWith("PLC"))
				{
					
					try
					{
						aLFTcpClient.plcName = ConfigurationManager.AppSettings[str.ToString()].Split(new char[] { ';' })[0];
						aLFTcpClient.plcIpAdress = ConfigurationManager.AppSettings[str.ToString()].Split(new char[] { ';' })[1];
						aLFTcpClient.plcPort = int.Parse(ConfigurationManager.AppSettings[str.ToString()].Split(new char[] { ';' })[2]);
						aLFTcpClient.plcTcpClient = new ALFTcpClient(aLFTcpClient.plcName);
						aLFTcpClient.plcTcpClient.MessageReceived += new ALFTcpClient.MessageReceivedEventHandler(this.PlcTcpClient_MessageReceived);
						aLFTcpClient.plcTcpClient.Connect(aLFTcpClient.plcIpAdress, aLFTcpClient.plcPort);
						this.myPlcs.Add(aLFTcpClient);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						MessageBox.Show(string.Format("Invalid configuration file - Please check\r\n{0}", exception.Message));
					}
				}
			}

            GetLogPath();

            this.recheckTimer.Interval = 100;
			this.recheckTimer.Tick += new EventHandler(this.RecheckTimer_Tick);
			this.recheckTimer.Enabled = true;
			this.recheckTimer.Start();
			this.reConnectTimer.Interval = 10000;
			this.reConnectTimer.Tick += new EventHandler(this.ReConnectTimer_Tick);
			this.reConnectTimer.Enabled = true;
			this.reConnectTimer.Start();
			this.InitializeComponent();
		}

		protected override void Dispose(bool disposing)
		{
			if ((!disposing ? false : this.components != null))
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			this.textBox1.Text = string.Concat(this.textBox1.Text, "Waiting for finishing all threads to close application - Please wait!");
			this.textBox1.Refresh();
			this.reConnectTimer.Enabled = false;
			this.reConnectTimer.Tick -= new EventHandler(this.ReConnectTimer_Tick);
			this.StopConnection();
			this.recheckTimer.Enabled = false;
			this.recheckTimer.Tick -= new EventHandler(this.RecheckTimer_Tick);
			this.RecheckTimer_Tick(null, null);
		}

		private void Form1_Shown(object sender, EventArgs e)
		{
			this.textBox1.Text = string.Concat(this.textBox1.Text, "Another amazing project by FAL ;-)");
		}

        //Get the TXT file name
		private string GetCurrentLogFileName_Date(string plc)
		{
            ActualDate = DateTime.Today;
            int year = ActualDate.Year;
			string str = year.ToString().PadLeft(4, '0');
			year = ActualDate.Month;
			string str1 = year.ToString().PadLeft(2, '0');
			year = ActualDate.Day;
			string textFileName = string.Format("{0}{1}{2}{3}", plc +"_", str, str1, year.ToString().PadLeft(2, '0'));
			return textFileName;
		}

		private void GetLogPath()
		{           
            mylogfilenames = new List<KeyValuePair<string, string>>();
            string[] allKeys = ConfigurationManager.AppSettings.AllKeys;
            string LogPath = "";

            for (int i = 0; i < (int)allKeys.Length; i++)
            {
                string str = allKeys[i];                

                if (str.ToString().Contains("LogPath"))
                {
                    LogPath = ConfigurationManager.AppSettings[str];
                    try
                    {
                        myLogPaths.Add(string.Format("{0}", LogPath));

                        if (!Directory.Exists(LogPath))
                        {
                            Directory.CreateDirectory(LogPath);
                        }
                        string p = str.Substring(0, str.IndexOf("_"));
                        KeyValuePair<string, string> logfile = new KeyValuePair<string, string>(p, string.Format("{0}\\{1}.txt", LogPath, this.GetCurrentLogFileName_Date(p)));
                        mylogfilenames.Add(logfile);

                    }
                    catch (Exception exception1)
                    {
                        Exception exception = exception1;
                        MessageBox.Show(string.Format("Invalid configuration file - Please check\r\n{0}", exception.Message));
                    }
                }
                
            }
		}

        private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(40, 81);
            this.textBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(971, 378);
            this.textBox1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ClientSize = new System.Drawing.Size(1073, 496);
            this.Controls.Add(this.textBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "PlcTcpLogger";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		private void PlcTcpClient_MessageReceived(TcpClient tcpClient, string messageString)
		{
            this.LogSorter.Add(messageString);
		}

        private void RecheckTimer_Tick(object sender, EventArgs e)
        {

            if (ActualDate.Date != DateTime.Today)
            {
                GetLogPath();
            }
            this.recheckTimer.Enabled = false;
            this.Refresh();
            this.textBox1.Refresh();
          
            try
            {
              
                string[] strArrays = new string[this.LogSorter.Count];
                List<string> logSorter = this.LogSorter;
                Monitor.Enter(logSorter);
                try
                {
                    this.LogSorter.CopyTo(strArrays);
                    this.LogSorter.Clear();
                }
                finally
                {
                    Monitor.Exit(logSorter);
                }

                string[] strArrays1 = strArrays;

                StreamWriter streamWriter = null;
                for (int i = 0; i < (int)strArrays1.Length; i++)
                {
                    string str = strArrays1[i];
                    foreach (var h in mylogfilenames)
                    {
                        if (str.Contains(h.Key))
                        {
                             streamWriter = new StreamWriter(h.Value, true);
                        }                       
                    }

                    if (streamWriter == null)
                    {
                        streamWriter = new StreamWriter(mylogfilenames[0].Key, true);						
                    }

                    if (str != "")
                    {
                        streamWriter.WriteLine(str);
                    }
					streamWriter.Close();
				}
				this.textBox1.Text = string.Format("{0}\r\n", this.credits); ;
				foreach (var t in mylogfilenames)
                {
					this.textBox1.Text = this.textBox1.Text + string.Format("{0} {1}\r\n", "Log written to", t.Value);
				}
				
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                this.textBox1.Text = string.Format("{0}\r\n{1} {2}", this.credits, "Error writing Log:\r\n", exception.Message);
            }
            this.textBox1.Text = string.Concat(this.textBox1.Text, this.errorMessage);            
            this.recheckTimer.Enabled = true;
        }

        private void ReConnectTimer_Tick(object sender, EventArgs e)
		{
			this.reConnectTimer.Enabled = false;
			this.errorMessage = "";
			foreach (plc myPlc in this.myPlcs)
			{
				if (!myPlc.plcTcpClient.Connected)
				{
					try
					{
						this.errorMessage = string.Format("Trying to reconnect to {0}\r\n", myPlc.plcName);
						myPlc.plcTcpClient.Connect(myPlc.plcIpAdress, myPlc.plcPort);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						this.errorMessage = string.Format("Error reconnecting to {0}\r\n{1}\r\n", myPlc.plcName, exception.Message);
					}
				}
			}
			this.reConnectTimer.Enabled = true;
		}

		private void StopConnection()
		{
			foreach (plc myPlc in this.myPlcs)
			{
				try
				{
					if (myPlc.plcTcpClient.Connected)
					{
						myPlc.plcTcpClient.MessageReceived -= new ALFTcpClient.MessageReceivedEventHandler(this.PlcTcpClient_MessageReceived);
						myPlc.plcTcpClient.Disconnect();
					}
				}
				catch (SocketException socketException)
				{
				}
			}
		}

    }
}
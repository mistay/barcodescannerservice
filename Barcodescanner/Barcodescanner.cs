using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Net.Http;
using System.Reflection;

namespace Barcodescanner
{
    public partial class Barcodescanner : ServiceBase
    {
        public Barcodescanner()
        {
            InitializeComponent();

            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("MySource"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "MySource", "MyNewLog");
            }
            eventLog1.Source = "MySource";
            eventLog1.Log = "MyNewLog";
        }

        private static readonly HttpClient httpClient = new HttpClient();

        protected override void OnStart(string[] args)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            eventLog1.WriteEntry("Barcodescanner::OnStart(). version: " + version);

            string[] portNames = SerialPort.GetPortNames();

            if (portNames.Length<=0)
            {
                eventLog1.WriteEntry("No serial Ports found, please attach barcodereader to COM port (serial comport profile SPP)");

            } else
            {
                eventLog1.WriteEntry("Available serialports: " + string.Join(", ", portNames));

                string comPort = portNames[0];
                eventLog1.WriteEntry("Trying to open serialport: " + comPort);


                try
                {
                    // strategy, use first available serialport. todo: read configuration
                    serialPort1.PortName = comPort;
                    serialPort1.Open();
                    eventLog1.WriteEntry("succesfully opened serialport: " + serialPort1.PortName);
                }
                catch (Exception ex)
                {
                    eventLog1.WriteEntry("tried to open serialport: " + serialPort1.PortName + ", but failed. exception: " + ex.ToString());
                }
            }
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("Barcodescanner::OnStop.");
        }
        protected override void OnContinue()
        {
            eventLog1.WriteEntry("Barcodescanner::OnContinue.");
        }

        private void serialPort1_DataReceived_1(object sender, SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialDataReceivedEventArgs args = (System.IO.Ports.SerialDataReceivedEventArgs)e;

            string foo = serialPort1.ReadExisting();
            eventLog1.WriteEntry("read byte: " + foo.Replace('%', ' '));


            Task task = sendHttpAsync(foo);

        }
        async Task sendHttpAsync(string barcode)
        {
            eventLog1.WriteEntry("sendHttpAsync()");

            var values = new Dictionary<string, string>
            {
                { "barcode", barcode },
            };

            var content = new FormUrlEncodedContent(values);


            eventLog1.WriteEntry("trying to sendHttpAsync() ...");
            var response = await httpClient.PostAsync("https://langhofer.at/foo.php", content);
            eventLog1.WriteEntry("sendHttpAsync() sent.");

        }
    }
}

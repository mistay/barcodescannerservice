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
using System.Configuration;

namespace Barcodescanner
{
    public partial class Barcodescanner : ServiceBase
    {
        public Barcodescanner()
        {
            InitializeComponent();

            eventLog1 = new System.Diagnostics.EventLog();
            eventLog1.Source = "Barcodescanner";
            eventLog1.Log = "Application"; // default log. log name other than "Application" needs specific permissions to create different logs
        }

        private static readonly HttpClient httpClient = new HttpClient();

        protected override void OnStart(string[] args)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            eventLog1.WriteEntry("Barcodescanner::OnStart(). version: " + version);
            eventLog1.WriteEntry("Barcodescanner::OnStart(). args:" + string.Join(", ", args));
            // Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Barcodescanner

            eventLog1.WriteEntry("Barcodescanner::OnStart(). Read ConfigurationFile: " + ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);


            //Properties.Settings.Default.accessURL = "asdf1234asdf";
            //Properties.Settings.Default.Save();

            eventLog1.WriteEntry("Barcodescanner::OnStart(). Read accessURL :" + Properties.Settings.Default.accessURL);

            string[] portNames = SerialPort.GetPortNames();

            if (portNames.Length<=0)
            {
                eventLog1.WriteEntry("No serial Ports found, please attach barcodereader to serialport (serial comport profile SPP)");
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
            eventLog1.WriteEntry("Barcodescanner::serialPort1_DataReceived_1 read byte: " + foo.Replace('%', ' '));

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

            string url = "https://example.com/barcode.php";
            eventLog1.WriteEntry("trying to sendHttpAsync() to '" + url + "' ...");
            var response = await httpClient.PostAsync(url, content);
            eventLog1.WriteEntry("sendHttpAsync() sent.");
        }
    }
}

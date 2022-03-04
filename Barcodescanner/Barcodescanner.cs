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
            eventLog1.WriteEntry("In OnStart. version 7");

            serialPort1.Open();
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("In OnStop.");
        }
        protected override void OnContinue()
        {
            eventLog1.WriteEntry("In OnContinue.");
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

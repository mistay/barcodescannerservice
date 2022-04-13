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
using System.IO;
using System.Threading;
using System.Text.Json;


namespace Barcodescanner
{
    public partial class Barcodescanner : ServiceBase
    {

        string accessURL = "";
        string serialPort = "";
        bool debug = false;
        static readonly int RETRY_TIMEOUT = 10000;
        
        public Barcodescanner()
        {
            InitializeComponent();

            eventLog1 = new System.Diagnostics.EventLog
            {
                Source = "Barcodescanner",
                Log = "Application" // default log. log name other than "Application" needs specific permissions to create different logs
            };
        }

        private static readonly HttpClient httpClient = new HttpClient();
        
        private string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                return appSettings[key];
            }
            catch (ConfigurationErrorsException e)
            {
                eventLog1.WriteEntry("Error reading ConfigurationManager.AppSettings settings: " + e.ToString());
            }
            return null;
        }

        private void SaveSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException e)
            {
                eventLog1.WriteEntry("Error writing app settings: " + e.ToString());
            }
        }

        protected override void OnStart(string[] args)
        {
            System.ComponentModel.BackgroundWorker backgroundWorker1 = new BackgroundWorker();
        string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            eventLog1.WriteEntry("OnStart(). " + Assembly.GetExecutingAssembly().GetName().FullName + " version: " + version);

            SaveSettings("startTime", DateTime.Now.ToString());
            SaveSettings("version", version);

            eventLog1.WriteEntry("Configuration from file location: " + AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

            accessURL = ReadSetting("accessURL");
            if (accessURL == null)
            {
                accessURL = "https://example.com/barcodescanread.php";
                SaveSettings("accessURL", accessURL);
            }
            serialPort = ReadSetting("serialPort");
            if (serialPort == null)
            {
                serialPort = "COM1";
                SaveSettings("serialPort", serialPort);
            }

            string tmpDebug = ReadSetting("debug");
            if (tmpDebug == null)
            {
                debug = false;
                SaveSettings("debug", "false");
            } else
            {
                if (tmpDebug.ToLower().Contains("true"))
                    debug = true;
            }

            eventLog1.WriteEntry("Read accessURL from settings file: " + accessURL);
            eventLog1.WriteEntry("Read serialPort from settings file: " + serialPort);
            eventLog1.WriteEntry("Read debug from settings file: " + debug.ToString());

            backgroundWorker1.DoWork += new DoWorkEventHandler(BackgroundWorkerTryConnect);
            backgroundWorker1.RunWorkerAsync();
        }

        private void BackgroundWorkerTryConnect(object sender, DoWorkEventArgs e)
        {
            while (true) {
                
                if (!serialPort1.IsOpen) { 

                    string[] portNames = SerialPort.GetPortNames();

                    if (portNames.Length <= 0)
                    {
                        eventLog1.WriteEntry("No serial Ports found, please attach barcodereader to serialport (serial comport profile SPP)");
                    }
                    else
                    {
                        eventLog1.WriteEntry("Available serialports: " + string.Join(", ", portNames));

                        if (portNames.Any(x => x.Equals(serialPort))) { 


                            eventLog1.WriteEntry("Trying to open serialport: " + serialPort);

                            try
                            {
                                serialPort1.PortName = serialPort;
                                serialPort1.Open();
                                eventLog1.WriteEntry("succesfully opened serialport: " + serialPort1.PortName);
                            }
                            catch (Exception ex)
                            {
                                eventLog1.WriteEntry("tried to open serialport: " + serialPort1.PortName + ", but failed. exception: " + ex.ToString());
                            }
                        } else {
                            eventLog1.WriteEntry("Configured serialport " + serialPort + " is not available, waiting...");
                        }
                    }
                }

                eventLog1.WriteEntry("retrying to connect to " + serialPort + " in " + RETRY_TIMEOUT + " seconds");
                System.Threading.Thread.Sleep(RETRY_TIMEOUT);
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

        private void SerialPort1_DataReceived_1(object sender, SerialDataReceivedEventArgs e)
        {
            // System.IO.Ports.SerialDataReceivedEventArgs args = (System.IO.Ports.SerialDataReceivedEventArgs)e;

            string tag = serialPort1.ReadExisting();
            eventLog1.WriteEntry("Barcodescanner::serialPort1_DataReceived_1 read byte: " + tag.Replace('%', ' '));

            SendHttpAsync(tag);
        }

        async void SendHttpAsync(string tag)
        {
            eventLog1.WriteEntry("sendHttpAsync(). tag: " + tag);

            Barcodeparameter barcodeparameter = new Barcodeparameter
            {
                tag = tag,
                protocol_version = "1"
            };

            string myJSON = "";
            try
            {
                myJSON = JsonSerializer.Serialize(barcodeparameter);
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry("could not serialize to json: " + ex.ToString());
            }

            StringContent content = new StringContent(myJSON, Encoding.UTF8, "application/json");

            if (debug)
                eventLog1.WriteEntry("trying to sendHttpAsync() value '" + string.Join(", ", content) + "' to '" + accessURL + "' ...");

            try
            {
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(new Uri(accessURL), content);

                string body = await httpResponseMessage.Content.ReadAsStringAsync();
                eventLog1.WriteEntry("received http statuscode: " + (int)httpResponseMessage.StatusCode + " body: " + body);
            } catch (Exception ex)
            {
                eventLog1.WriteEntry("Could not PostAsync()" + ex.ToString());
            }
        }
    }
}

using MohawkWebserverDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Web;

/// <summary>
/// This namespace contains code to access the DP5 remote API via the RESTful interface.  The API has more functions than shown
/// below; the full documentation for this can be accessed at http://<host>:<port>/swagger-ui.html
/// </summary>
namespace MohawkWebServer
{
    // Notification Client Class to Generate and Print the Notifications
    public class NotificationWsClient : IDisposable
    {

        public int ReceiveBufferSize { get; set; } = 8192;
        private ClientWebSocket? WS;
        private CancellationTokenSource? CTS;
        public async Task ConnectAsync(string url)
        {
            if (WS != null)
            {
                if (WS.State == WebSocketState.Open) return;
                else WS.Dispose();
            }
            WS = new ClientWebSocket();
            if (CTS != null) CTS.Dispose();
            CTS = new CancellationTokenSource();
            await WS.ConnectAsync(new Uri(url), CTS.Token);
            await Task.Factory.StartNew(ReceiveLoop, CTS.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task DisconnectAsync()
        {
            if (WS is null) return;
            if (WS.State == WebSocketState.Open)
            {
                CTS.CancelAfter(TimeSpan.FromSeconds(2));
                await WS.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                await WS.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            WS.Dispose();
            CTS.Dispose();
        }

        private async Task ReceiveLoop()
        {
            var loopToken = CTS.Token;
            MemoryStream outputStream = null;
            WebSocketReceiveResult receiveResult = null;
            var buffer = new byte[ReceiveBufferSize];
            try
            {
                while (!loopToken.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(ReceiveBufferSize);
                    do
                    {
                        receiveResult = await WS.ReceiveAsync(buffer, CTS.Token);
                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                            outputStream.Write(buffer, 0, receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                    outputStream.Position = 0;
                    ResponseReceived(outputStream);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                outputStream?.Dispose();
            }
        }

        private void ResponseReceived(Stream inputStream)
        {
            // TODO: handle deserializing responses and matching them to the requests.
            // IMPORTANT: DON'T FORGET TO DISPOSE THE inputStream!
            StreamReader reader = new StreamReader(inputStream);
            string notification_message = reader.ReadToEnd();
            Console.WriteLine("Notification Message: " + notification_message);
            inputStream.Dispose();
        }

        public void Dispose() => DisconnectAsync().Wait();

    }

    /// <summary>
    /// Class to execute MohawkWebServer calls; note that this code contains no error handling; it is recommended to include this in 
    /// production code
    /// </summary>
    internal class MohawkWebServerExample
    {
        private string host;
        private int port;

        private HttpClient client = new HttpClient();

        /// <summary>
        /// Default contructor, assumes that the server is on localhost and listenign on port 8556.  Note that this expects the Mohawk service to be running
        /// if it is not then execute Mohawk Service (assuming you are on English language windows 
        /// and using a default install location)
        /// </summary>
        public MohawkWebServerExample() : this("localhost", 8556)
        {
        }

        /// <summary>
        /// Sample code to interact with Mohawk;
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            MohawkWebServerExample mohawkWebServerExample = new MohawkWebServerExample();
            NotificationWsClient notificationWsClient = new NotificationWsClient();
            var result = notificationWsClient.ConnectAsync("ws://localhost:8025/mohawk/notifications");
            Console.WriteLine($"Lid Status = {mohawkWebServerExample.LidStatus}");
            Console.WriteLine($"Mohawk Status = {mohawkWebServerExample.MohawkStatus}");
            Console.WriteLine($"Fan Speed = {mohawkWebServerExample.FanSpeed}");
            Console.WriteLine($"Version = {mohawkWebServerExample.Version}");
            Console.WriteLine($"Format = {mohawkWebServerExample.Format}");
            Console.WriteLine($"Temperature = {mohawkWebServerExample.Temperature}");
            Console.WriteLine($"Pins Status = {mohawkWebServerExample.PinsStatus}");
            Console.WriteLine($"Reset Pins = {mohawkWebServerExample.ResetPins}");
            Console.WriteLine($"Pins Up = {mohawkWebServerExample.PinsUp}");
            Console.WriteLine($"Configure Reader = {mohawkWebServerExample.ConfigureReader}");
            Console.WriteLine($"Load Worklist Json = {mohawkWebServerExample.LoadWorklistJson}");
            Console.WriteLine($"Get Worklist Status = {mohawkWebServerExample.WorklistStatus}");
            Console.WriteLine($"Get Worklist = {mohawkWebServerExample.GetWorklist}");
            Console.WriteLine($"Set Rack Barcode = {mohawkWebServerExample.SetRackBarcode}");
            Console.WriteLine($"Read Barcode = {mohawkWebServerExample.ReadBarcode}");
            Console.WriteLine($"Finish Worklist = {mohawkWebServerExample.FinishWorklist}");
            Console.WriteLine($"Load Worklist Excel = {mohawkWebServerExample.LoadWorklistExcel}");
            Console.WriteLine($"Finish Worklist = {mohawkWebServerExample.FinishWorklist}");
            Console.WriteLine($"Load Worklist XML = {mohawkWebServerExample.LoadWorklistXml}");
            Console.WriteLine($"Finish Worklist = {mohawkWebServerExample.FinishWorklist}");
            Console.WriteLine($"Load Worklist CSV = {mohawkWebServerExample.LoadWorklistCsv}");
            Console.WriteLine($"Get Report Json = {mohawkWebServerExample.GetReportJson}");
            Console.WriteLine($"Get Report Xml = {mohawkWebServerExample.GetReportXml}");
            Console.WriteLine($"Get Report Excel = {mohawkWebServerExample.GetReportExcel}");
            Console.WriteLine($"Get Report Csv = {mohawkWebServerExample.GetReportCsv}");
            result = notificationWsClient.DisconnectAsync();
            Console.WriteLine($"Get Report Csv = {mohawkWebServerExample.ShutDown}");
        }


        /// <summary>
        /// Public construcotr, note that this does not connect and assumes that the server is on localhost and listenign on port 8556.  
        /// Note that this expects the Mohawk service to be running if it is not then execute 
        /// </summary>
        /// <param name="host">The host the Mohawk server is running on</param>
        /// <param name="port">The psot the Mohawk server is listening on</param>
        public MohawkWebServerExample(String host, int port)
        {
            this.host = host;
            this.port = port;
        }

        /// <summary>
        /// Status of the Lid
        /// </summary>
        public string LidStatus => JObject.Parse(GetStringSync("lid_status", null)).SelectToken("result").Value<string>();

        /// <summary>
        /// Current Status of Mohawk
        /// </summary>
        public string MohawkStatus => JObject.Parse(GetStringSync("mohawk_status", null)).SelectToken("result").Value<string>();

        /// <summary>
        /// Current Fan Speed of Mohawk
        /// </summary>
        public string FanSpeed => JObject.Parse(GetStringSync("fan_speed", null)).SelectToken("result").Value<string>();

        /// <summary>
        /// Current version of Mohawk
        /// </summary>
        public string Version => JObject.Parse(GetStringSync("version", null)).SelectToken("result").Value<string>();

        /// <summary>
        /// Current format of Mohawk
        /// </summary>
        public string Format => JObject.Parse(GetStringSync("format", null)).SelectToken("result").Value<string>();

        /// <summary>
        /// Current temperature of Mohawk
        /// </summary>
        public string Temperature => JObject.Parse(GetStringSync("temperature", null)).SelectToken("result").Value<string>();

        /// <summary>
        /// Pins status of Mohaw
        /// </summary>
        public string PinsStatus => GetStringSync("pins_status", null).ToString();

        /// <summary>
        /// Reset pins of Mohawk
        /// </summary>
        public string ResetPins => JObject.Parse(PostStringSync("reset_pins", null, null)).SelectToken("result").Value<string>();

        /// <summary>
        /// Set pins up for Mohawk
        /// </summary>
        public string PinsUp
        {
            get
            {
                List<Pin> Pins = new List<Pin>();
                Pin pin1 = new Pin(1, 1);
                Pin pin2 = new Pin(1, 1);
                Pins.Add(pin1);
                Pins.Add(pin2);

                StringContent requestData =
                    new StringContent(JsonConvert.SerializeObject(Pins), Encoding.UTF8, "application/json");
                return PostStringSync("pins_up", null, requestData).ToString();
            }
        }

        /// <summary>
        /// Configure reader for Mohawk
        /// </summary>
        public string ConfigureReader
        {
            get
            {
                var myObject = (dynamic)new JsonObject();
                myObject.Add("type", "ZIATH");

                StringContent requestData = new StringContent(myObject.ToString(), Encoding.UTF8, "application/json");
                return PostStringSync("reader", null, requestData).ToString();
            }
        }

        /// <summary>
        /// Read barcode from Mohawk
        /// </summary>
        public string ReadBarcode => PostStringSync("read_barcode", null, null).ToString();

        /// <summary>
        /// Set rack barcode for Mohawk
        /// </summary>
        public string SetRackBarcode
        {
            get
            {
                var myObject = (dynamic)new JsonObject();
                myObject.Add("rack_barcode", "001");
                myObject.Add("reset_pins", true);

                StringContent requestData = new StringContent(myObject.ToString(), Encoding.UTF8, "application/json");
                return PostStringSync("set_rack_barcode", null, requestData).ToString();
            }
        }

        /// <summary>
        /// Get worklist for Mohawk
        /// </summary>
        public string GetWorklist => GetStringSync("worklist", null).ToString();

        /// <summary>
        /// Get worklist status for Mohawk
        /// </summary>
        public string WorklistStatus => GetStringSync("worklist/status", null).ToString();

        /// <summary>
        /// Finish worklist for Mohawk
        /// </summary>
        public string FinishWorklist => PostStringSync("worklist/finish", null, null).ToString();

        /// <summary>
        /// Load Json worklist for Mohawk
        /// </summary>
        public string LoadWorklistJson
        {
            get
            {
                JObject worlistItem1 = new JObject();
                worlistItem1.Add("rack_barcode", "001");
                worlistItem1.Add("row", 1);
                worlistItem1.Add("column", 1);
                JObject worlistItem2 = new JObject();
                worlistItem2.Add("rack_barcode", "002");
                worlistItem2.Add("row", 3);
                worlistItem2.Add("column", 1);
                JObject worlistItem3 = new JObject();
                worlistItem3.Add("rack_barcode", "002");
                worlistItem3.Add("row", 1);
                worlistItem3.Add("column", 4);
                JObject worlistItem4 = new JObject();
                worlistItem4.Add("rack_barcode", "001");
                worlistItem4.Add("row", 3);
                worlistItem4.Add("column", 3);
                JArray worklistItemArr = new JArray();
                worklistItemArr.Add(worlistItem1);
                worklistItemArr.Add(worlistItem2);
                worklistItemArr.Add(worlistItem3);
                worklistItemArr.Add(worlistItem4);

                StringContent requestData = new StringContent(JsonConvert.SerializeObject(worklistItemArr), Encoding.UTF8, "application/json");
                return PostStringSync("worklist/load_json", null, requestData).ToString();
            }
        }

        /// <summary>
        /// Load Excel worklist for Mohawk
        /// </summary>
        public string LoadWorklistExcel
        {
            get
            {
                string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string sFile = System.IO.Path.Combine(sCurrentDirectory, @"..\..\..\picklistSample.xlsx");
                string sFilePath = Path.GetFullPath(sFile);
                var byteArray = File.ReadAllBytes(sFilePath);
                ByteArrayContent fileContent = new ByteArrayContent(byteArray);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                return PostStringSync("worklist/load_excel", null, fileContent).ToString();
            }
        }

        /// <summary>
        /// Load XML worklist for Mohawk
        /// </summary>
        public string LoadWorklistXml
        {
            get
            {
                string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string sFile = System.IO.Path.Combine(sCurrentDirectory, @"..\..\..\picklistSample.xml");
                string sFilePath = Path.GetFullPath(sFile);
                var byteArray = File.ReadAllBytes(sFilePath);
                ByteArrayContent fileContent = new ByteArrayContent(byteArray);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");
                return PostStringSync("worklist/load_xml", null, fileContent).ToString();
            }
        }

        /// <summary>
        /// Load Csv worklist for Mohawk
        /// </summary>
        public string LoadWorklistCsv
        {
            get
            {
                string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string sFile = System.IO.Path.Combine(sCurrentDirectory, @"..\..\..\picklistSample.xml");
                string sFilePath = Path.GetFullPath(sFile);
                var byteArray = File.ReadAllBytes(sFilePath);
                ByteArrayContent fileContent = new ByteArrayContent(byteArray);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
                return PostStringSync("worklist/load_csv", null, fileContent).ToString();
            }
        }

        /// <summary>
        /// Get report in json Mohawk
        /// </summary>
        public string GetReportJson => GetStringSync("report_to_json", null).ToString();

        /// <summary>
        /// Get report in xml for Mohawk
        /// </summary>
        public string GetReportXml
        {
            get
            {
                string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string sFile = System.IO.Path.Combine(sCurrentDirectory, @"..\..\..\picklistSample1.xml");
                string sFilePath = Path.GetFullPath(sFile);
                HttpClient client = new HttpClient();
                var response = client.GetAsync(ConstructUrl("report_to_xml", null)).Result;
                var content = response.Content.ReadAsByteArrayAsync().Result;
                File.WriteAllBytes(sFilePath, content);
                return "Xml File Write Success: " + sFilePath;
            }
        }

        /// <summary>
        /// Get report in excel for Mohawk
        /// </summary>
        public string GetReportExcel
        {
            get
            {
                string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string sFile = System.IO.Path.Combine(sCurrentDirectory, @"..\..\..\picklistSample1.xlsx");
                string sFilePath = Path.GetFullPath(sFile);
                HttpClient client = new HttpClient();
                var response = client.GetAsync(ConstructUrl("report_to_excel", null)).Result;
                var content = response.Content.ReadAsByteArrayAsync().Result;
                File.WriteAllBytes(sFilePath, content);
                return "Excel File Write Success: " + sFilePath;
            }
        }

        /// <summary>
        /// Get report in csv for Mohawk
        /// </summary>
        public string GetReportCsv
        {
            get
            {
                string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string sFile = System.IO.Path.Combine(sCurrentDirectory, @"..\..\..\picklistSample1.csv");
                string sFilePath = Path.GetFullPath(sFile);
                HttpClient client = new HttpClient();
                var response = client.GetAsync(ConstructUrl("report_to_csv", null)).Result;
                var content = response.Content.ReadAsByteArrayAsync().Result;
                File.WriteAllBytes(sFilePath, content);
                return "CSV File Write Success: " + sFilePath;
            }
        }

        /// <summary>
        /// ShutDown Mohawk
        /// </summary>
        public string ShutDown => PostStringSync("shutdown", null, null).ToString();

        /// <summary>
        /// Force Shutdown Mohawk
        /// </summary>
        public string ForceShutDown => PostStringSync("force_shutdown", null, null).ToString();

        /// <summary>
        /// Executes a GET and waits indefinitely for the return
        /// </summary>
        /// <param name="path">The section of the URL to add to the stub of the Mohawk API URL</param>
        /// <returns>The body of the response</returns>
        private string GetStringSync(string path, Dictionary<string, string>? qParams)
        {
            if (qParams == null)
            {
                qParams = new Dictionary<string, string>();
            }
            Task<string> getTask = client.GetStringAsync(ConstructUrl(path, qParams));
            getTask.Wait();
            return getTask.Result;
        }

        /// <summary>
        /// Executes a POST and waits indefinitely for the return
        /// </summary>
        /// <param name="path">The section of the URL to add to the stub of the Mohawk API URL</param>
        /// <param name="qParams">A dictionary of query paramers to use</param>
        /// <returns>The body of the response</returns>
        private string PostStringSync(string path, Dictionary<string, string>? qParams, HttpContent? content)
        {
            if (content == null)
            {
                content = new StringContent("");
            }
            Task<HttpResponseMessage> responseTask = client.PostAsync(ConstructUrl(path, qParams), content);
            responseTask.Wait();
            Task<string> contentTask = responseTask.Result.Content.ReadAsStringAsync();
            contentTask.Wait();
            return contentTask.Result;
        }

        /// <summary>
        /// Appends the given path to the stub of the DP5 API URL plus the given query params
        /// </summary>
        /// <param name="path">The path to append to the stub</param>
        /// <param name="qParams">A key/value pair of the query params</param>
        /// <returns></returns>
        private string ConstructUrl(string path, Dictionary<string, string>? qParams)
        {
            UriBuilder ub = new UriBuilder($"{this.Stub}/{path}");
            NameValueCollection nvc = HttpUtility.ParseQueryString(ub.Query);
            if (qParams != null)
            {
                foreach (KeyValuePair<string, string> kvp in qParams)
                {
                    nvc[kvp.Key] = kvp.Value;
                }
            }
            ub.Query = nvc.ToString();
            return ub.ToString();
        }

        /// <summary>
        /// The stub of the Mohawk API URL; this is set according to the given host and port
        /// </summary>
        private string Stub
        {
            get
            {
                return $"http://{this.host}:{this.port}/mohawk/api/v1";
            }
        }

    }
}
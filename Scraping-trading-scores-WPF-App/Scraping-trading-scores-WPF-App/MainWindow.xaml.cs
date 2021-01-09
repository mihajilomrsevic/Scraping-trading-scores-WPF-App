using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scraping_trading_scores_WPF_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        const int BUFFER_SIZE = 1024;
        const int DefaultTimeout = 2 * 60 * 1000; // 2 minutes timeout
        static DataTable myDataTableFinal = new DataTable();

        static List<DataScraper> employees;

        // Abort the request if the timer fires.
        private static void TimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                HttpWebRequest request = state as HttpWebRequest;
                if (request != null)
                {
                    request.Abort();
                }
            }
        }

        private static double CalculateMovingAverage(DataScraper dt)
        {
            double sum = 0;
            int i = 0;
            foreach (var e in employees)
            {
                if (e.tradedatetimegmt.ToString("hh:mm:ss") == dt.tradedatetimegmt.ToString("hh:mm:ss"))
                {
                    sum += e.close;
                    i++;
                }
            }
            return sum / i;
        }

        private static void RespCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                // Read the response into a Stream object.
                Stream responseStream = myRequestState.response.GetResponseStream();
                myRequestState.streamResponse = responseStream;

                // Begin the Reading of the contents of the HTML page and print it to the console.
                IAsyncResult asynchronousInputRead = responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                return;
            }
            catch (WebException e)
            {
                MessageBox.Show("\nRespCallback Exception raised!");
                MessageBox.Show("\nMessage:{0}", e.Message);
                MessageBox.Show("\nStatus:{0}", e.Status.ToString());
            }
            allDone.Set();
        }

        // Parse a content when is retrieved
        private static void ReadCallBack(IAsyncResult asyncResult)
        {

            try
            {

                RequestState myRequestState = (RequestState)asyncResult.AsyncState;
                Stream responseStream = myRequestState.streamResponse;
                int read = responseStream.EndRead(asyncResult);

                if (read > 0)
                {
                    myRequestState.requestData.Append(Encoding.ASCII.GetString(myRequestState.BufferRead, 0, read));
                    IAsyncResult asynchronousResult = responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                    return;
                }
                else
                {
                    Console.WriteLine("\nThe contents of the Html page are : ");
                    if (myRequestState.requestData.Length > 1)
                    {
                        string stringContent;
                        stringContent = myRequestState.requestData.ToString();
                        Console.WriteLine(stringContent);
                    }

                    employees = new List<DataScraper>();

                    var readAll = myRequestState.requestData.ToString();
                    var jObj = JObject.Parse(readAll);
                    employees = JsonConvert.DeserializeObject<List<DataScraper>>(jObj["results"]["items"].ToString());

                    DataTable myDataTable = new DataTable();
                    myDataTable.Columns.Add("Row");
                    myDataTable.Columns.Add("Tradedatetimegmt");
                    myDataTable.Columns.Add("Open");
                    myDataTable.Columns.Add("High");
                    myDataTable.Columns.Add("Low");
                    myDataTable.Columns.Add("Close");
                    myDataTable.Columns.Add("Volume");
                    myDataTable.Columns.Add("Moving average");
                    int i = 0;
                    foreach (var a in employees)
                    {
                        myDataTable.Rows.Add(i = i + 1, a.tradedatetimegmt, a.open, a.high, a.low, a.close, a.volume, CalculateMovingAverage(a));
                    }
                    myDataTableFinal = myDataTable;

                    responseStream.Close();
                }
            }
            catch (WebException e)
            {
                MessageBox.Show("\nReadCallBack Exception raised!");
                MessageBox.Show("\nMessage:{0}", e.Message);
                MessageBox.Show("\nStatus:{0}", e.Status.ToString());
            }
            allDone.Set();
        }
        public MainWindow()
        {
            InitializeComponent();
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {

                var query = enteredValue.Text;
                var url = $"https://webservice.gvsi.com/query/json/GetDaily/tradedatetimegmt/open/high/low/close/volume?pricesymbol=%22{query}%22&daysBack=100";
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);

                var userName = "";
                var passwd = "";
                myHttpWebRequest.Credentials = new NetworkCredential(userName, passwd);
                // Create an instance of the RequestState and assign the previous myHttpWebRequest
                // object to its request field.
                RequestState myRequestState = new RequestState();
                myRequestState.request = myHttpWebRequest;

                // Start the asynchronous request.
                IAsyncResult result =
                  (IAsyncResult)myHttpWebRequest.BeginGetResponse(new AsyncCallback(RespCallback), myRequestState);

                // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), myHttpWebRequest, DefaultTimeout, true);

                // The response came in the allowed time. The work processing will happen in the
                // callback function.
                allDone.WaitOne();
                tableResult.DataContext = myDataTableFinal;

                // Release the HttpWebResponse resource.
                //myRequestState.response.Close();
            }
            catch (WebException we)
            {
                MessageBox.Show("\nMain Exception raised!");
                MessageBox.Show("\nMessage:{0}", we.Message);
                MessageBox.Show("\nStatus:{0}", we.Status.ToString());
                MessageBox.Show("Press any key to continue..........");
            }
            catch (Exception ex)
            {
                MessageBox.Show("\nMain Exception raised!");
                MessageBox.Show("Source :{0} ", ex.Source);
                MessageBox.Show("Message :{0} ", ex.Message);
                MessageBox.Show("Press any key to continue..........");
            }
        }
    }
}

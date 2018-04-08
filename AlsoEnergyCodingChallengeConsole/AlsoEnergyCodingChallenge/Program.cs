using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Threading;

namespace AlsoEnergyCodingChallenge
{
    class Program
    {
        static void Main(string[] args)
        {
            // *** Print even numbers ***
            List<int> numbers = new List<int> { 1, 5, 2, 4, 7, 11, 3 };
            int sum = SumOfEvenNumbers(numbers);
            Console.WriteLine(sum);

            // *** Thread work ***
            List<int> threadNumbers = new List<int> { 1, 2, 3, 4, 5 };
            ThreadedPrintout(threadNumbers);

            // *** Web Service calls ***
            GetTimeFromService("http://localhost:53231/CurrentDateTime.ashx"); // Normal operation
            //GetTimeFromService("http://localhost:53231/CurrentDateTime.ashx?error=test"); // Query string resulting in 500
            //GetTimeFromService("http://localhost:53231/CurrentDateTime.ashx", 1); // Timeout - when ran without the two above it will result in a timeout.

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }

        #region "Thread Work"
        public static void ThreadedPrintout(List<int> numbers)
        {
            Thread firstThread = new Thread(ThreadOneCounter);
            firstThread.Start(numbers);

            Thread secondThread = new Thread(ThreadTwoCounter);
            secondThread.Start(numbers);

            firstThread.Join();
            secondThread.Join();
        }

        public static void ThreadOneCounter(object numbers)
        {
            foreach(int number in (List<int>)numbers)
            {
                Thread.Sleep(500);
                Console.WriteLine("t1: " + number);
            }
        }

        public static void ThreadTwoCounter(object numbers)
        {
            foreach (int number in (List<int>)numbers)
            {
                Thread.Sleep(1000);
                Console.WriteLine("t2: " + number);
            }
        }
        #endregion

        public static int SumOfEvenNumbers(List<int> numberList)
        {
            int sum = 0;

            foreach (int number in numberList)
            {
                if (number % 2 != 0)
                {
                    sum += number;
                }
            }

            return sum;
        }

        public static void GetTimeFromService(string url, int timeout = 1000)
        {
            HttpResponseData responseData = new HttpResponseData
            {
                StatusString = string.Empty,
                StatusCode = 0,
                StartTime = DateTime.UtcNow,
                ServiceResponse = string.Empty,
                Status = -999
            };

            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
                httpRequest.Method = "GET";
                httpRequest.Timeout = timeout;
                HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                using (StreamReader sr = new StreamReader(httpResponse.GetResponseStream()))
                {
                    // 200
                    responseData.ServiceResponse = sr.ReadToEnd();
                    responseData.Status = 1;
                    responseData.StatusCode = (int)httpResponse.StatusCode;
                    responseData.StatusString = httpResponse.StatusDescription;
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;

                if (response != null)
                {
                    // Likely 500 but could be 404
                    responseData.ServiceResponse = ex.Message;
                    responseData.Status = 2;
                    responseData.StatusCode = (int)response.StatusCode;
                    responseData.StatusString = Convert.ToString(response.StatusDescription);
                }
                else
                {
                    // Timed out of connection refused.
                    responseData.ServiceResponse = ex.Message;
                    responseData.StatusString = Convert.ToString(ex.Status);
                }
            }

            responseData.EndTime = DateTime.UtcNow;

            // Display information back to the user
            Console.Write(responseData.ServiceResponse);

            // Record our HttpRequest events to the database
            RecordHttpRequest(responseData);
        }

        public static void RecordHttpRequest(HttpResponseData responseData)
        {
            using (SqlConnection sqlConn = new SqlConnection("Data Source=DESKTOP-C8NLQ8K\\SQLEXPRESS;Initial Catalog=ae_code_challenge;Integrated Security=True;"))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO server_response_log(StartTimeUTC, EndTimeUTC, HTTPStatusCode, DataString, Status, StatusString) " +
                                                 "VALUES (@StartTimeUTC, @EndTimeUTC, @HTTPStatusCode, @DataString, @Status, @StatusString)", sqlConn);

                    cmd.Connection.Open();
                    cmd.Parameters.AddWithValue("@StartTimeUTC", responseData.StartTime);
                    cmd.Parameters.AddWithValue("@EndTimeUTC", responseData.EndTime);
                    cmd.Parameters.AddWithValue("@HTTPStatusCode", responseData.StatusCode);
                    cmd.Parameters.AddWithValue("@DataString", responseData.ServiceResponse);
                    cmd.Parameters.AddWithValue("@Status", responseData.Status);
                    cmd.Parameters.AddWithValue("@StatusString", responseData.StatusString);
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
       
    }

    public class HttpResponseData
    {
        public string StatusString { get; set; }
        public int StatusCode { get; set; }
        public string ServiceResponse { get; set; }
        public int Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}

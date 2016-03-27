using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net; // For generating HTTP requests and getting responses.
using NiceHashBotLib; // Include this for Order class, which contains stats for our order.
using Newtonsoft.Json; // For JSON parsing of remote APIs.

public class HandlerClass
{
    /// <summary>
    /// This method is called every 0.5 seconds.
    /// </summary>
    /// <param name="OrderStats">Order stats - do not change any properties or call any methods. This is provided only as read-only object.</param>
    /// <param name="MaxPrice">Current maximal price. Change this, if you would like to change the price.</param>
    /// <param name="NewLimit">Current speed limit. Change this, if you would like to change the limit.</param>
    public static void HandleOrder(ref Order OrderStats, ref double MaxPrice, ref double NewLimit)
    {
        // Following line of code makes the rest of the code to run only once per minute.
        if ((++Tick % 121) != 0) return;

        // Perform check, if order has been created at all. If not, stop executing the code.
        if (OrderStats == null) return;

        // Retreive JSON data from API server. Replace URL with your own API request URL.
        string JSONData = GetHTTPResponseInJSON("http://antminer/json_margin.php");
        if (JSONData == null)
        {
            Console.WriteLine("Local margin down!");
        }
        // Serialize returned JSON data.
        LocalStatsResponse Response;
        double m = 0.01;
        double r = 0;
        double s = 25 / 50000;
        string d = "00:00:00";
        try
        {
            Response = JsonConvert.DeserializeObject<LocalStatsResponse>(JSONData);
            m = Response.margin;
            r = Response.estimated_reward;
            s = 25 / (Response.ghashes_ps / 1000);
            d = Response.round_duration;
        }
        catch
        {
            Console.WriteLine("Local margin grabage!");
        }
        
        if (TimeSpan.Parse(d).TotalMinutes < 21)
        {
            try
            {
                double diff = r - OrderStats.BTCPaid;
                Console.WriteLine("Stoping order #" + OrderStats.ID.ToString() + " Paid " + OrderStats.BTCPaid.ToString("F8") + " Reward " + r.ToString("F8") + " Diff " + diff.ToString("F8"));
            }
            catch
            {
            }
            NewLimit = 0;
            return;
        }

        // Following line of code makes the rest of the code to run only once per minute.
        if ((Tick % 242) != 0) return;

        if (r>0 && OrderStats.BTCPaid>0 && ((r - OrderStats.BTCPaid) < m) && OrderStats.SpeedLimit < 4250)
        {
            NewLimit = (OrderStats.BTCPaid + m) / s;
            //Console.WriteLine("Adjusting order #" + OrderStats.ID.ToString() + " speed limit to: " + NewLimit.ToString("F2"));
        }
    }

    /// <summary>
    /// Data structure used for serializing JSON response from CoinWarz. 
    /// It allows us to parse JSON with one line of code and easily access every data contained in JSON message.
    /// </summary>
#pragma warning disable 0649
    class LocalStatsResponse
    {
        public double margin;
        public double estimated_reward;
        public double ghashes_ps;
        public string round_duration;
    }
#pragma warning restore 0649


    /// <summary>
    /// Property used for measuring time.
    /// </summary>
    private static int Tick = -10;


    // Following methods do not need to be altered.
    #region PRIVATE_METHODS

    /// <summary>
    /// Get HTTP JSON response for provided URL.
    /// </summary>
    /// <param name="URL">URL.</param>
    /// <returns>JSON data returned by webserver or null if error occured.</returns>
    private static string GetHTTPResponseInJSON(string URL)
    {
        try
        {
            HttpWebRequest WReq = (HttpWebRequest)WebRequest.Create(URL);
            WReq.Timeout = 60000;
            WebResponse WResp = WReq.GetResponse();
            Stream DataStream = WResp.GetResponseStream();
            DataStream.ReadTimeout = 60000;
            StreamReader SReader = new StreamReader(DataStream);
            string ResponseData = SReader.ReadToEnd();
            if (ResponseData[0] != '{')
                throw new Exception("Not JSON data.");

            return ResponseData;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    #endregion
}

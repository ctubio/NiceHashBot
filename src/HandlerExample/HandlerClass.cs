using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net; // For generating HTTP requests and getting responses.
using NiceHashBotLib; // Include this for Order class, which contains stats for our order.
using Newtonsoft.Json; // For JSON parsing of remote APIs.
using WMPLib;

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
            Console.WriteLine("[" + DateTime.Now.ToString() + "] Local margin down!!");
            try
            {
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();
                wplayer.URL = "http://antminer/c.mp3";
                wplayer.controls.play();
            }
            catch { }
            return;
        }
        // Serialize returned JSON data.
        LocalStatsResponse Response;
        double m = 0.01;
        double r = 0;
        double g = 0;
        double s = 25 / 50000;
        string d = "00:00:00";
        double p = OrderStats.BTCPaid;
        try
        {
            Response = JsonConvert.DeserializeObject<LocalStatsResponse>(JSONData);
            m = Response.margin;
            r = Response.estimated_reward;
            g = (Response.ghashes_ps / 1000);
            p = p + Response.prepaid;
            s = 25 / g;
            g = (g / 1000);
            d = Response.round_duration;
        }
        catch
        {
            Console.WriteLine("[" + DateTime.Now.ToString() + "] Local margin grabage!!");
            try
            {
                WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();
                wplayer.URL = "http://antminer/c.mp3";
                wplayer.controls.play();
            }
            catch { }
            return;
        }

        if (TimeSpan.Parse(d).TotalMinutes < 21)
        {
            try
            {
                double diff = r - p;
                string _diff = "";
                if (diff > 0) _diff = "+";
                double speed = (OrderStats.Speed / 1000);
                string log = "[" + DateTime.Now.ToString() + "] Paid " + p.ToString("F8") + " Reward " + r.ToString("F8") + " " + Math.Round((diff * 100) / p, 2).ToString("F2") + "% Diff " + Math.Round(((r - p) * 100) / r, 2).ToString("F2") + "% "+ _diff + diff.ToString("F8") + " Hash " + speed.ToString("F2") + "/" + OrderStats.SpeedLimit.ToString("F2") + " Slush " + d + "/" + g.ToString("F2");
                Console.WriteLine("[" + DateTime.Now.ToString() + "] Stoping order #" + OrderStats.ID.ToString());
                Console.WriteLine(log);
                string path = "C:\\htdocs\\phpmyminer\\results";
                string contents;
                using (StreamReader sreader = new StreamReader(path)) { contents = sreader.ReadToEnd(); }
                File.Delete(path);
                using (StreamWriter swriter = new StreamWriter(path, false))
                {
                    contents = log + Environment.NewLine + contents;
                    swriter.Write(contents);
                }
            }
            catch
            {
            }
            NewLimit = 0;
            return;
        }

        // Following line of code makes the rest of the code to run only once per minute.
        if ((Tick % 242) != 0) return;

        if (r>0 && p>0 && ((r - p) < m) && OrderStats.SpeedLimit < 9250)
        {
            NewLimit = Math.Round((p + m) / s, 2);
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
        public double prepaid;
        public string round_duration;
        public int auto;
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NiceHashBotLib;
using System.Net; // For generating HTTP requests and getting responses.
using Newtonsoft.Json; // For JSON parsing of remote APIs.
using System.IO;

namespace NiceHashBot
{
    public partial class FormMain : Form
    {
        public static FormPool FormPoolInstance;
        public static FormNewOrder FormNewOrderInstance;

        private Timer TimerRefresh;
        private Timer BalanceRefresh;

        public FormMain()
        {
            InitializeComponent();
            this.Text = Application.ProductName + " [" + Application.ProductVersion + "]";

            FormPoolInstance = null;
            FormNewOrderInstance = null;

            TimerRefresh = new Timer();
            TimerRefresh.Interval = 500;
            TimerRefresh.Tick += new EventHandler(TimerRefresh_Tick);
            TimerRefresh.Start();

            BalanceRefresh = new Timer();
            BalanceRefresh.Interval = 30 * 1000;
            BalanceRefresh.Tick += new EventHandler(BalanceRefresh_Tick);
            BalanceRefresh.Start();
        }

        private void BalanceRefresh_Tick(object sender, EventArgs e)
        {
            if (!APIWrapper.ValidAuthorization) return;

            APIBalance Balance = APIWrapper.GetBalance();
            if (Balance == null)
            {
                toolStripLabel2.Text = "";
            }
            else
            {
                toolStripLabel2.Text = Balance.Confirmed.ToString("F8") + " BTC";
            }

            string JSONData = GetHTTPResponseInJSON("http://antminer/json_margin.php");
            if (JSONData == null)
            {
                Console.WriteLine("[" + DateTime.Now.ToString() + "] Local margin down!");
            }
            LocalStatsResponse Response;
            double m = 0.01;
            double r = 0;
            double g = 0;
            double s = 25 / 50000;
            string d = "00:00:00";
            int a = 0;
            try
            {
                Response = JsonConvert.DeserializeObject<LocalStatsResponse>(JSONData);
                m = Response.margin;
                r = Response.estimated_reward;
                g = (Response.ghashes_ps / 1000);
                s = 25 / g;
                g = (g / 1000);
                d = Response.round_duration;
                a = Response.auto;
                string _m = "!";
                if (a == 0) _m = "";

                OrderContainer[] Orders = OrderContainer.GetAll();
                if (Orders.Length > 0 && Orders[0].OrderStats != null)
                {
                    double p = Orders[0].OrderStats.BTCPaid + Response.prepaid;
                    double diff = r - p;
                    string _diff = "";
                    if (diff > 0) _diff = "+";
                    double speed = (Orders[0].OrderStats.Speed / 1000);
                    toolStripStatusLabel1.Text = m.ToString("F3") + _m + " - Paid " + p.ToString("F8") + " Reward " + r.ToString("F8") + " " + Math.Round((diff * 100) / p, 2).ToString("F2") + "% Diff " + Math.Round(((r - p) * 100) / r, 2).ToString("F2") + "% " + _diff + diff.ToString("F8") + " Hash " + speed.ToString("F2") + "/" + Orders[0].OrderStats.SpeedLimit.ToString("F2") + " Slush " + d + "/" + g.ToString("F2");
                }
                else {
                    toolStripStatusLabel1.Text = m.ToString("F3") + _m + " Slush " + d + "/" + g.ToString("F2");
                }
                statusStrip1.Refresh();
                if (a>0 && TimeSpan.Parse(d).TotalMinutes > a)
                {
                    if (Orders.Length == 0)
                    {
                        if (FormNewOrderInstance == null)
                        {
                            FormNewOrderInstance = new FormNewOrder();
                            FormNewOrderInstance.Show();
                            FormNewOrderInstance.AcceptButton.PerformClick();
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("[" + DateTime.Now.ToString() + "] Local margin grabage!");
            }
        }

        private void TimerRefresh_Tick(object sender, EventArgs e)
        {
            if (!APIWrapper.ValidAuthorization) return;

            OrderContainer[] Orders = OrderContainer.GetAll();
            int Selected = -1;
            if (listView1.SelectedIndices.Count > 0)
                Selected = listView1.SelectedIndices[0];
            listView1.Items.Clear();
            for (int i = 0; i < Orders.Length; i++)
            {
                int Algorithm = Orders[i].Algorithm;
                ListViewItem LVI = new ListViewItem(APIWrapper.SERVICE_NAME[Orders[i].ServiceLocation]);
                LVI.SubItems.Add(APIWrapper.ALGORITHM_NAME[Algorithm]);
                if (Orders[i].OrderStats != null)
                {
                    LVI.SubItems.Add("#" + Orders[i].OrderStats.ID.ToString());
                    string PriceText = Orders[i].OrderStats.Price.ToString("F4") + " (" + Orders[i].MaxPrice.ToString("F4") + ")";
                    PriceText += " BTC/" + APIWrapper.SPEED_TEXT[Algorithm] + "/Day";
                    LVI.SubItems.Add(PriceText);
                    LVI.SubItems.Add(Orders[i].OrderStats.BTCAvailable.ToString("F8"));
                    LVI.SubItems.Add(Orders[i].OrderStats.BTCPaid.ToString("F8"));
                    string SpeedText = (Orders[i].OrderStats.Speed * APIWrapper.ALGORITHM_MULTIPLIER[Algorithm]).ToString("F4") + " (" + Orders[i].Limit.ToString("F2") + ") " + APIWrapper.SPEED_TEXT[Algorithm] + "/s";
                    LVI.SubItems.Add(SpeedText);
                    LVI.SubItems.Add(Orders[i].OrderStats.Workers.ToString());
                    if (!Orders[i].OrderStats.Alive)
                        LVI.BackColor = Color.PaleVioletRed;
                    else
                        LVI.BackColor = Color.LightGreen;
                    //LVI.SubItems.Add("View competing orders");
                }

                if (Selected >= 0 && Selected == i)
                    LVI.Selected = true;

                listView1.Items.Add(LVI);
            }
        }

        private void orderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (FormNewOrderInstance == null)
            {
                FormNewOrderInstance = new FormNewOrder();
                FormNewOrderInstance.Show();
            }
        }

        private void poolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFormPools();
        }


        public static void ShowFormPools()
        {
            if (FormPoolInstance == null)
            {
                FormPoolInstance = new FormPool();
                FormPoolInstance.Show();
            }
        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.nicehash.com/index.jsp?p=wallet");
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            OrderContainer.Remove(listView1.SelectedIndices[0]);
            TimerRefresh_Tick(sender, e);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSettings FS = new FormSettings(SettingsContainer.Settings.APIID, SettingsContainer.Settings.APIKey, SettingsContainer.Settings.TwoFactorSecret);
            if (FS.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SettingsContainer.Settings.APIID = FS.ID;
                SettingsContainer.Settings.APIKey = FS.Key;
                SettingsContainer.Settings.TwoFactorSecret = FS.TwoFASecret;
                SettingsContainer.Commit();

                if (SettingsContainer.Settings.TwoFactorSecret.Length > 0)
                    APIWrapper.TwoFASecret = SettingsContainer.Settings.TwoFactorSecret;
                else
                    APIWrapper.TwoFASecret = null;

                if (APIWrapper.Initialize(SettingsContainer.Settings.APIID.ToString(), SettingsContainer.Settings.APIKey))
                {
                    orderToolStripMenuItem.Enabled = true;
                    BalanceRefresh_Tick(sender, e);
                }
                else
                {
                    orderToolStripMenuItem.Enabled = false;
                }
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            settingsToolStripMenuItem_Click(sender, e);
        }
       
        private void speedLimitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            FormNumberInput FNI = new FormNumberInput("Set new limit", 0, 1000, OrderContainer.GetLimit(listView1.SelectedIndices[0]), 2);
            if (FNI.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OrderContainer.SetLimit(listView1.SelectedIndices[0], FNI.Value);
            }
        }

        private void setMaximalPriceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
                return;

            FormNumberInput FNI = new FormNumberInput("Set new price", 0.0001, 100, OrderContainer.GetMaxPrice(listView1.SelectedIndices[0]), 4);
            if (FNI.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OrderContainer.SetMaxPrice(listView1.SelectedIndices[0], FNI.Value);
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

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MainLauncher
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            // load settings
            LoadSettings();

            WindowState = FormWindowState.Minimized;            
        }

        private void LoadSettings()
        {
            var settingsData = File.ReadAllLines(ConfigurationManager.AppSettings["SettingsFile"], Encoding.Default);

            foreach (var entry in settingsData)
            {
                // menu_type|menu_title|menu_link
                
                // ignore commmented lines in settings.ini file
                if (entry[0].Equals('#'))
                {
                    continue;
                }

                var namevalue = entry.Split('|');

                CustomContextMenu ccm = new CustomContextMenu(namevalue[0], namevalue[2]);
                var tsi = contextMenuStripMain.Items.Add(namevalue[1]);
                tsi.Tag = ccm;
                contextMenuStripMain.Items.Insert(0, tsi);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIconMain.Visible = true;
                notifyIconMain.ShowBalloonTip(500);
                this.ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
            }
        }

        private void notifyIconMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void GetMCBFOREX(string urlForexMCB)
        {
            var content = GetHtmlFromUrl(urlForexMCB);

            var pageContent = content;
            const string patternDiv = "<div.*</div>";

            pageContent = pageContent.Replace("\n", String.Empty);
            pageContent = pageContent.Replace("\r", String.Empty);
            pageContent = pageContent.Replace("\t", String.Empty);

            // get <DIV> tags from the content, one by one
            pageContent = pageContent.Replace("</div>", "</div>" + Environment.NewLine);

            StringBuilder sbForex = new StringBuilder();
            foreach (Match m in Regex.Matches(pageContent, patternDiv, RegexOptions.IgnoreCase))
            {
                var value = m.Value;

                if (value.Contains("class=\"currency\""))
                {
                    // clear <div> and </div>
                    value = value.Replace("<div class=\"row\">", String.Empty)
                        .Replace("<div class=\"row odd\">", String.Empty)
                        .Replace("</div>", String.Empty);

                    value = value.Replace(" ", String.Empty).Replace("<spanclass=\"currency\">", String.Empty)
                        .Replace("<spanclass=\"sell\">", "Buy : ").Replace("<spanclass=\"buy\">", "Sell : ")
                        .Replace("</span>", " * ").Trim();

                    sbForex.AppendLine(value.Substring(0, value.Length - 1));
                }
            }

            MessageBox.Show(sbForex.ToString(), 
                String.Format("FOREX - MCB @ {0}", DateTime.Today.ToString("dd/MM/yyyy")), 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string GetHtmlFromUrl(string url)
        {
            var html = String.Empty;

            if (String.IsNullOrEmpty(url)) return html;

            var request = (HttpWebRequest)WebRequest.Create(url);

            HttpWebResponse response = null;
            try
            {
                // get the response, to later read the stream
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // get the response stream.
            //Stream responseStream = response.GetResponseStream();

            if (response != null)
            {
                // use a stream reader that understands UTF8
                var reader = new StreamReader(response.GetResponseStream(), encoding: Encoding.UTF8);
                html = reader.ReadToEnd();
                // close the reader
                reader.Close();
                response.Close();
            }

            return html; //return html content
        }

        private void contextMenuStripMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            CustomContextMenu ccm = e.ClickedItem.Tag as CustomContextMenu;
            if (null == ccm)
            {
                return;
            }
            
            switch (ccm.MenuType)
            {
                case "web":
                case "app":
                case "dir":
                    try
                    {
                        Process.Start(ccm.MenuLink);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;
                case "mcb":
                    GetMCBFOREX(ccm.MenuLink);
                    break;
                default:
                    break;
            }
        }
    }

    class CustomContextMenu
    {
        public CustomContextMenu(string menuType, string menuLink)
        {
            MenuType = menuType;
            MenuLink = menuLink;
        }

        public string MenuType { set; get; }
        public string MenuLink { set; get; }
    }
}

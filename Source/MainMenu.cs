using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace EFT_Trainer
{
    public partial class MainMenu : Form
    {
        bool isRunning = false;
        string hightlightPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Temp\\Highlights\\Escape From Tarkov";

        int count = 0;
        bool initCount = false;

        // Variables for moving the Application
        private const int WM_NCHITTEST = 0x84;
        private const int HT_CLIENT = 0x1;
        private const int HT_CAPTION = 0x2;

        public MainMenu()
        {
            InitializeComponent();
        }

        // Code to move the Application
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
                m.Result = (IntPtr)(HT_CAPTION);
        }

        //Start/Stop Button
        private void btnStart_Click(object sender, EventArgs e)
        {
            isRunning = !isRunning;
            if (isRunning)
            {
                label1.Text = "Kill Confirmer: ON";
                btnStart.Text = "Stop";
                checkTimer.Start();
                btnStart.FlatAppearance.BorderColor = Color.Red;
            }
            else
            {
                label1.Text = "Kill Confirmer: OFF";
                btnStart.Text = "Start";
                lblCurrentKills.Text = "Current Kills: 0";
                checkTimer.Stop();
                initCount = false;
                btnStart.FlatAppearance.BorderColor = Color.Lime;
            }
        }

        //Check Timer if new highlights found
        private void checkTimer_Tick(object sender, EventArgs e)
        {
            if (!initCount)
            {
                //Set lenght
                count = Directory.GetFiles(hightlightPath, "*").Length;
                initCount = true;
            }

            int nCount = Directory.GetFiles(hightlightPath, "*").Length;

            // if kill than beep
            if (nCount > count)
            {
                Console.Beep();
                lblCurrentKills.Text = "Current Kills: " + nCount;
            }

            count = nCount;

            //Check if still kills found / in round
            if (nCount == 0)
            {
                lblCurrentKills.Text = "Current Kills: " + nCount;
                count = nCount;
            }
        }


        private void MainMenu_Load(object sender, EventArgs e)
        {
            hightlightPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), hightlightPath);
        }

        //Show Crosshair Settings Form Button
        private void btnCrosshairSettings_Click(object sender, EventArgs e)
        {
            var CrosshairSettingsForm = new ECO_MainGUI();
            //this.Hide();
            CrosshairSettingsForm.Show();
        }

        //Close Button
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}

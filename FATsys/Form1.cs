using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FATsys.Utils;

namespace FATsys
{
    public partial class frmMain : Form
    {
        CFATManager fatMng;
        public frmMain()
        {
            InitializeComponent();
            

            CFATLogger.registerForm(this);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {

            ERUN_MODE nRunMode = ERUN_MODE.BACKTEST;

            if ( cmbMode.Text == "MODE_BACKTEST") nRunMode = ERUN_MODE.BACKTEST;
            if (cmbMode.Text == "MODE_OPTIMIZE") nRunMode = ERUN_MODE.OPTIMIZE;
            if (cmbMode.Text == "MODE_REAL") nRunMode = ERUN_MODE.REALTIME;
            if (cmbMode.Text == "MODE_SIMULATION") nRunMode = ERUN_MODE.SIMULATION;

            if (MessageBox.Show(string.Format("Do you want to run {0} ?", cmbMode.Text), "Confirm", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            fatMng = new CFATManager(nRunMode, cmbMode.Text);

            if (!fatMng.init())
                return;

            this.Text = CFATManager.getSystemName();

            btnStart.Enabled = false;
            btnStop.Enabled = true;

            cmbMode.Enabled = false;
            fatMng.doProcess();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            fatMng.stop();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            cmbMode.Enabled = true;
        }

        public  void addLog(string sLog, bool bAppend = true)
        {
            txtLog.Invoke((MethodInvoker)delegate
            {
                if (bAppend)
                {
                    if (txtLog.Text.Length > 1000)
                        txtLog.Text = "";
                    txtLog.Text += sLog;
                    txtLog.Text += "\r\n";
                }
                else
                    txtLog.Text = sLog;

                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            });
        }
        public void addLog_label(string sLog)
        {
            lblLog.Invoke((MethodInvoker)delegate
            {
                lblLog.Text = sLog;
            });
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            cmbMode.SelectedIndex = 0;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            CMQClient.disConnect();
        }

        private void btnSaveReport_Click(object sender, EventArgs e)
        {
            fatMng.makeReport();
        }

        private void btnMatchVirtual2Real_Click(object sender, EventArgs e)
        {
            //fatMng.matchPosVirtual2Real();
        }
    }
}

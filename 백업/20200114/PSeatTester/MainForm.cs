﻿#define PROGRAM_RUNNING

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSeatTester
{    
    public interface MyInterface
    {
        /// <summary>
        /// 공용 함수를 호출한다.
        /// </summary>
        COMMON_FUCTION 공용함수 { get; }
        /// <summary>
        /// 환경 변수 설정을 진해안다.
        /// </summary>
        __Config__ GetConfig { get; set; }
        /// <summary>
        /// LIN 통신 함수를 호출한다.
        /// </summary>
        //LinControl GetLin { get; }
        /// <summary>
        /// CAN 통신 함수를 호출한다.
        /// </summary>
        __CanControl GetCan { get; }
        /// <summary>
        /// 현재 검사를 진행하는지 정보를 읽어 오거나 설정한다.
        /// </summary>
        bool isRunning { get; set; }
        /// <summary>
        /// 프로그램 종료 여부를 같는다.
        /// </summary>
        bool isExit { get; }
        short GetCanChannel { get; }
        //short GetLinChannel { get; }
        CanMap GetCanReWrite { get; }
        PanelMeter GetPMeter { get; }
        /// <summary>
        /// 파워 제어 함수를 호출한다.
        /// </summary>
        PowerControl GetPower { get; }
        IOControl GetIO { get; }
        SoundMeter GetSound { get; }
    }
    public partial class MainForm : Form,MyInterface
    {
        private COMMON_FUCTION ComF = new COMMON_FUCTION();
        private __Config__ Config;
        private IOControl IOPort = null;
        private MES_Control PopCtrol = null;
        //private LinControl LinCtrl = null;
        private __CanControl CanCtrl = null;
        private __CheckItem__ CheckItem = new __CheckItem__();
        private __Spec__ mSpec = new __Spec__();
        private PowerControl PwCtrl = null;
        private PanelMeter pMeter = null;
        private Color NotSelectColor;
        private Color SelectColor; 
        private SoundMeter Sound = null;
        private __TestData__ mData = new __TestData__();
        private KALMAN_FILETER CurrFilter = new KALMAN_FILETER();
        private CanMap CanReWrite = null;
        private __Infor__ Infor = new __Infor__();
        public MainForm()
        {
            InitializeComponent();
        }

        public COMMON_FUCTION 공용함수
        {
            get { return ComF; }
        }

        public __Config__ GetConfig
        {
            get { return Config; }
            set 
            { 
                Config = value;
                ConfigSetting ReadConfig = new ConfigSetting();
                ReadConfig.ReadWriteConfig = Config;
            }
        }
        

        private void MainForm_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            ConfigSetting ReadConfig = new ConfigSetting();
            Config = ReadConfig.ReadWriteConfig;

            NotSelectColor = fpSpread1.ActiveSheet.Cells[0, 0].BackColor;
            SelectColor = Color.FromArgb(172, 227, 175);

            PopCtrol = new MES_Control(ClientIp: Config.Client, ServerIp: Config.Server, mControl: this);
            PopCtrol.Open();
            IOPort = new IOControl(Board: Config.Board, PC: Config.PC, mControl: this);
            IOPort.Open();

            if (IOPort.isOpen == false) MessageBox.Show("I/O Card IP 를 확인해 주십시오.", "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (IOPort.isConnection == false) MessageBox.Show("I/O Card 와 접속되지 않습니다.", "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (PopCtrol.isClientConnection == false)
            {
                //MessageBox.Show("서버와 접속되지 않습니다.", "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            label2.Text = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();


            테스ToolStripMenuItem_Click(테스ToolStripMenuItem, new EventArgs());
#if PROGRAM_RUNNING

            comboBox4.SelectedItem = null;
            comboBox3.SelectedItem = null;
            //comboBox5.SelectedItem = "12 WAY";
            OpenInfor();
            if (SerialOpen() == false)
            {

            }

            CanCtrl = new __CanControl(this);
            //LinCtrl = new LinControl(false, this);

            CanReWrite = new CanMap(this);
            CanLinDefaultSetting();
            if (0 <= Config.Can.Device)
            {
                CanCtrl.OpenCan(0, CanPosition(), (short)Config.Can.Speed, false);
                if (CanCtrl.isOpen(0) == false) { }

            }

            //if (0 <= Config.Lin.Device)
            //{
            //    if (LinCtrl.isOpen(LinChannel) == false)
            //    {
            //        LinCtrl.LinOpen(LinPosition(Config.Lin.Device), LinControl.HW_MODE.SLAVE, Config.Lin.Speed);
            //        if (LinCtrl.isOpen(LinChannel) == false) { }
            //    }
            //}

            ComF.ReadFileListNotExt(Program.SPEC_PATH.ToString(), "*.Spc", COMMON_FUCTION.FileSortMode.FILENAME_ODERBY);
            List<string> FList = ComF.GetFileList;

            if (0 < FList.Count)
            {
                ModelBoxChangeFlag = true;
                comboBox1.Items.Clear();
                foreach (string s in FList) comboBox1.Items.Add(s);

                if (0 < comboBox1.Items.Count)
                {
                    comboBox1.SelectedIndex = 0;
                }

                if (comboBox1.SelectedItem != null)
                {
                    string sName = Program.SPEC_PATH.ToString() + "\\" + comboBox1.SelectedItem.ToString() + ".Spc";
                    if (File.Exists(sName) == true) ComF.OpenSpec(sName, ref mSpec);                    
                    mName = comboBox1.SelectedItem.ToString();
                }
                ModelBoxChangeFlag = false;
            }
#endif
            CheckItem.LhdRhd = LHD_RHD.LHD;
            CheckItem.LhRh = LH_RH.LH;
            CheckItem.ProductTestRunFlag = false;
            CheckItem.Recline = false;
            CheckItem.Height = false;
            CheckItem.Relax = false;
            CheckItem.Legrest = false;
            CheckItem.LegrestExit = false;
            CheckItem.Can = false; 
        
            FormLoadFirstSpecDisplayTime = ComF.timeGetTimems();
            comboBox4.SelectedItem = "IMS";
            timer1.Enabled = true;
            timer2.Enabled = true;
            DisplaySpec();
            return;
        }
        
        private bool FormLoadFirstSpecDisplay = true;
        private long FormLoadFirstSpecDisplayTime = 0;

        private string mName = null;

        private bool SerialOpen()
        {
            bool Flag = true;

            Sound = new SoundMeter(Config.NoiseMeter.Port);
            if ((Config.NoiseMeter.Port != null) && (Config.NoiseMeter.Port != string.Empty))
            {
                if (Sound.Open() == false) Flag = false;
            }

            PwCtrl = new PowerControl(Config.Power);
            if(PwCtrl.IsOpen == false)
            {
                MessageBox.Show("파워 제어용 포트를 열수 없습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Flag = false;
            }

            pMeter = new PanelMeter(this);
            pMeter.Open(Config.Panel);
            if(pMeter.isOpen == false)
            {
                MessageBox.Show("판넬메타 통신 포트를 열수 없습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Flag = false;
            }

            return Flag;
        }
        public SoundMeter GetSound
        {
            get { return Sound; }
        }
        public PowerControl GetPower
        {
            get { return PwCtrl; }
        }

        //public LinControl GetLin
        //{
        //    get { return LinCtrl; }
        //}
        public __CanControl GetCan
        {
            get { return CanCtrl; }
        }

        public CanMap GetCanReWrite
        {
            get { return CanReWrite; }
        }
        //public short GetLinChannel
        //{
        //    get { return LinChannel; }
        //}
        public short GetCanChannel
        {
            get { return CanChannel; }
        }
        private void CanLinDefaultSetting()
        {
            CanReWrite.CanLinDefaultSetting();
            return;
        }

        public PanelMeter GetPMeter
        {
            get
            {
                return pMeter;
            }
        }

        private bool ExitFlag { get; set; }
        private bool RunningFlag { get; set; }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (RunningFlag == true)
            {
                e.Cancel = true;
                return;
            }

           
            ExitFlag = true;

            CloseForm xClose = new CloseForm()
            {
                Owner = this,
                WindowState = FormWindowState.Normal,
                StartPosition = FormStartPosition.CenterScreen,
                //Location = new Point(1, 1),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MinimizeBox = false,
                MaximizeBox = false,
                TopMost = true
            };


            xClose.FormClosing += delegate (object sender1, FormClosingEventArgs e1)
            {
                e1.Cancel = false;
                xClose.Dispose();
#if PROGRAM_RUNNING
                if (CanCtrl.isOpen(0) == true) CanCtrl.CanClose(0);
                //if (LinCtrl != null)
                //{
                //    if (LinCtrl.isOpen(LinChannel) == true) LinCtrl.LinClose();
                //}

                if (PwCtrl.IsOpen == true) PwCtrl.Close();
                if (pMeter.isOpen == true) pMeter.Close();
#endif
                System.Diagnostics.Process[] mProcess = System.Diagnostics.Process.GetProcessesByName(Application.ProductName);
                foreach (System.Diagnostics.Process p in mProcess) p.Kill();
            };

            e.Cancel = true;
            xClose.Show();
            return;
        }
        public bool isRunning 
        {
            get { return RunningFlag; }
            set { RunningFlag = value; }
        }
        public bool isExit
        {
            get { return ExitFlag; }
        }


//        private short LinChannel = 0;
//        public short LinPosition(short Pos)
//        {
//#if PROGRAM_RUNNING
//            short ID = 0;

//            string[] Device = LinCtrl.GetDevice;

//            for (short i = 0; i < Device.Length; i++)
//            {
//                string s = Device[i];
//                string s1 = Pos.ToString() + " - ID";

//                if (0 <= s.IndexOf(s1))
//                {
//                    ID = i;
//                    break;
//                }
//            }

//            LinChannel = ID;
//            return ID;
//#else
//            return 0;
//#endif
//        }

        private short CanChannel = 0;
        public short CanPosition()
        {
#if PROGRAM_RUNNING
            //short ID = 0;

            string[] Device = CanCtrl.GetDevice;

            //for (short i = 0; i < Device.Length; i++)
            //{
            //    string s = Device[i];
            //    string s1 = "0x" + Pos.ToString("X2");

            //    if (0 <= s.IndexOf(s1))
            //    {
            //        ID = i;
            //        break;
            //    }
            //}
            short Pos = -1;
            string s1 = "Device=" + Config.Can.Device.ToString();
            string s2 = "Channel=" + Config.Can.Channel.ToString() + "h";

            foreach (string s in Device)
            {

                if (0 <= s.IndexOf(s1))
                {
                    if (0 <= s.IndexOf(s2))
                    {
                        string ss = s.Substring(s.IndexOf("ID=") + "ID=".Length);
                        string[] ss1 = ss.Split(',');
                        if (1 < ss1.Length)
                        {
                            string ss2 = ss1[0].Replace("(", null);

                            ss2 = ss2.Replace(")", null);
                            Pos = (short)ComF.StringToHex(ss2);
                        }
                    }
                }
            }

            if (Pos == -1)
            {
                Pos = Config.Can.ID;
            }
            CanChannel = Pos;
            return Pos;
#else
            return 0;
#endif
        }

        //private void toolStripButton4_Click(object sender, EventArgs e)
        //{
        //    //Master Lin 설정
        //    if (toolStripButton6.Text == "로그인")
        //    {
        //        //MessageBox.Show("로그인을 먼저 진행하십시오.", "경고");
        //        //uMessageBox.Show(promptText: "로그인을 먼저 진행하십시오.", title: "경고");
        //        MessageBox.Show(this, "로그인을 먼저 진행하십시오.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        return;
        //    }
        //    if (RunningFlag == false)
        //    {
        //        if (0 <= Config.Lin.Device)
        //        {
        //            if (LinCtrl.isOpen(LinChannel) == false)
        //            {
        //                LinCtrl.LinOpen(LinPosition(Config.Lin.Device), LinControl.HW_MODE.MASTER, Config.Lin.Speed);
        //                if (LinCtrl.isOpen(LinChannel) == false) { }
        //            }
        //        }
        //        LinCtrl.LinConfigSetting(LinChannel);
        //    }
        //    return;
        //}

        private SelfTest SelfForm = null;

        private void OpenFormClose()
        {
            if (SelfForm != null) SelfForm.Close();
            if (SpecForm != null) SpecForm.Close();
            return;
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            if(IOPort.GetAuto == false)
            {
                if(SelfForm == null)
                {
                    OpenFormClose();
                    panel1.SendToBack();
                    panel1.Visible = false;

                    panel2.Visible = true;
                    if(panel2.Parent != this) panel2.Parent = this;
                    panel2.BringToFront();
                    SelfForm = new SelfTest(this)
                    {
                        MaximizeBox = false,
                        MinimizeBox = false,
                        ControlBox = false,
                        ShowIcon = false,
                        StartPosition = FormStartPosition.CenterScreen,
                        WindowState = FormWindowState.Maximized,
                        TopMost = false,
                        TopLevel = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Location = new Point(0, 0),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
                    };

                    SelfForm.FormClosing += delegate (object sender1, FormClosingEventArgs e1)
                    {
                        e1.Cancel = false;
                        SelfForm.Parent = null;
                        SelfForm.Dispose();
                        SelfForm = null;

                        IOPort.PinConnectionOnOff = false;
                    };

                    SelfForm.Parent = panel2;
                    SelfForm.Show();
                }
            }
            return;
        }

        private void 테스ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButton1.Visible = true; //테스트
            toolStripButton4.Visible = IOPort.GetAuto == false ? true : false;
            //설정
            toolStripButton2.Visible = false;
            toolStripButton3.Visible = false;
            //toolStripButton4.Visible = false;
            //toolStripButton5.Visible = false;            
            toolStripSeparator1.Visible = false;
            //toolStripSeparator2.Visible = false;
            //toolStripSeparator3.Visible = false;            
            //로그인
            toolStripButton6.Visible = false;
            toolStripButton10.Visible = false;
            
            //보기
            toolStripButton7.Visible = false;
            //종료
            toolStripButton8.Visible = false;
            toolStripButton9.Visible = false;
            toolStripSeparator4.Visible = false;
            return;
        }

        private void 설정ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButton1.Visible = false; //테스트
            toolStripButton4.Visible = false;
            //설정
            toolStripButton2.Visible = true;
            toolStripButton3.Visible = true;
            //toolStripButton4.Visible = true;
            //toolStripButton5.Visible = true;
            toolStripSeparator1.Visible = true;
            //toolStripSeparator2.Visible = true;
            //toolStripSeparator3.Visible = true;
            //로그인
            toolStripButton6.Visible = false;
            toolStripButton10.Visible = false;
            //toolStripSeparator4.Visible = false;
            //보기
            toolStripButton7.Visible = false;
            //종료
            toolStripButton8.Visible = false;
            toolStripButton9.Visible = false;
            toolStripSeparator4.Visible = false;
            return;
        }

        private void 로그인ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButton1.Visible = false; //테스트
            toolStripButton4.Visible = false;
            //설정
            toolStripButton2.Visible = false;
            toolStripButton3.Visible = false;
            //toolStripButton4.Visible = false;
            //toolStripButton5.Visible = false;
            toolStripSeparator1.Visible = false;
            //toolStripSeparator2.Visible = false;
            //toolStripSeparator3.Visible = false;
            //로그인
            toolStripButton6.Visible = true;
            toolStripButton10.Visible = true;
            toolStripSeparator4.Visible = true;
            //보기
            toolStripButton7.Visible = false;
            //종료
            toolStripButton8.Visible = false;
            toolStripButton9.Visible = false;
            //toolStripSeparator4.Visible = false;
            return;
        }

        private void 보기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButton1.Visible = false; //테스트
            toolStripButton4.Visible = false;
            //설정
            toolStripButton2.Visible = false;
            toolStripButton3.Visible = false;
            //toolStripButton4.Visible = false;
            //toolStripButton5.Visible = false;
            toolStripSeparator1.Visible = false;
            //toolStripSeparator2.Visible = false;
            //toolStripSeparator3.Visible = false;
            //로그인
            toolStripButton6.Visible = false;
            toolStripButton10.Visible = false;
            toolStripSeparator4.Visible = false;
            //보기
            toolStripButton7.Visible = true;
            //종료
            toolStripButton8.Visible = false;
            toolStripButton9.Visible = false;
            //toolStripSeparator4.Visible = false;
            return;
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripButton1.Visible = false; //테스트
            toolStripButton4.Visible = false;
            //설정
            toolStripButton2.Visible = false;
            toolStripButton3.Visible = false;
            //toolStripButton4.Visible = false;
            //toolStripButton5.Visible = false;
            toolStripSeparator1.Visible = false;
            //toolStripSeparator2.Visible = false;
            //toolStripSeparator3.Visible = false;
            //로그인
            toolStripButton6.Visible = false;
            toolStripButton10.Visible = false;
            //toolStripSeparator4.Visible = false;
            //보기
            toolStripButton7.Visible = false;
            //종료
            toolStripButton8.Visible = true;
            toolStripButton9.Visible = true;
            toolStripSeparator4.Visible = true;
            return;
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (toolStripButton6.Text == "로그인")
            {
                PasswordCheckForm pass = new PasswordCheckForm();
                //아래와 같이 해 주면 폼을 닫을때 Dialog로 오픈을 하지 않아도 {} 안을 실행하게 된다. 동시에 해당 폼의 FormClosing이 동시에 실행되므로 Dialog로 오픈한것 같은 효과를 얻는다.
                pass.FormClosing += delegate (object sender1, FormClosingEventArgs e1)
                {
                    if (pass.result == true)
                    {
                        toolStripButton6.Text = "로그아웃";
                        로그인ToolStripMenuItem.Text = "로그아웃";
                        toolStripButton6.Image = Properties.Resources.Pad_Unlock_36Pixel1;

                        toolStripButton2.Enabled = true;
                        toolStripButton3.Enabled = true;
                        //toolStripButton4.Enabled = true;
                        //toolStripButton5.Enabled = true;
                    }
                };
                pass.Show();
            }
            else
            {
                toolStripButton6.Text = "로그인";
                로그인ToolStripMenuItem.Text = "로그인";
                toolStripButton6.Image = Properties.Resources.Pad_Lock3;
                toolStripButton2.Enabled = false;
                toolStripButton3.Enabled = false;
                //toolStripButton4.Enabled = false;
                //toolStripButton5.Enabled = false;
            }
            return;
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            if (toolStripButton6.Text == "로그인")
            {
                //MessageBox.Show("로그인을 먼저 진행하십시오.", "경고");
                //uMessageBox.Show(promptText: "로그인을 먼저 진행하십시오.", title: "경고");
                MessageBox.Show(this, "로그인을 먼저 진행하십시오.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            PasswordSetForm set = new PasswordSetForm();
            set.MinimizeBox = false;
            set.MaximizeBox = false;
            set.FormBorderStyle = FormBorderStyle.FixedSingle;
            set.Show();
            return;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (toolStrip1.Enabled == IOPort.GetAuto) toolStrip1.Enabled = !IOPort.GetAuto;
            label2.Text = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();

            if (led1.Value.AsBoolean != IOPort.isConnection) led1.Value.AsBoolean = IOPort.isConnection;
            if (led7.Value.AsBoolean != PopCtrol.isClientConnection) led7.Value.AsBoolean = PopCtrol.isClientConnection;

            return;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if(panel1.Visible == false)
            {
                OpenFormClose();
                panel2.SendToBack();
                panel2.Visible = false;

                panel1.Visible = true;                
                panel1.BringToFront();
                if (panel1.Parent != this) panel1.Parent = this;
            }
            return;
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (IOPort.GetAuto == false)
            {
                if (RunningFlag == false)
                {
                    this.Close();
                }
            }
            return;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ModelBoxChangeFlag == true) return;
            string s = comboBox1.SelectedItem.ToString();

            string sName = Program.SPEC_PATH.ToString() + "\\" + s + ".Spc";

            if (File.Exists(sName) == true) ComF.OpenSpec(sName, ref mSpec);
            DisplaySpec();
            mName = comboBox1.SelectedItem.ToString();
            return;
        }
        private bool BuzzerRunFlag = false;
        private bool BuzerOnOff = false;
        private long BuzzerLast = 0;
        private long BuzzerFirst = 0;
        private short BuzzerOnCount = 0;
        private bool ProductOutFlag = false;
        private bool JigUpFlag = false;
        private short JigUpCount = 0;
        private void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                timer2.Enabled = true;

                //this.Text = string.Format("{0:X}", IOPort.GetInData[1]);

                if(IOPort.GetJigUp == true)
                {
                    if (JigUpFlag == false) JigUpFlag = true;
                }

                if (led4.Value.AsBoolean != IOPort.GetProductIn) led4.Value.AsBoolean = IOPort.GetProductIn;
                if (led9.Value.AsBoolean != JigUpFlag) led9.Value.AsBoolean = JigUpFlag;
                if (led6.Value.AsBoolean != IOPort.TestOKOnOff) led6.Value.AsBoolean = IOPort.TestOKOnOff;
                if (led5.Value.AsBoolean != IOPort.TestINGOnOff) led5.Value.AsBoolean = IOPort.TestINGOnOff;

                if (BuzzerRunFlag == true)
                {
                    BuzzerLast = ComF.timeGetTimems();
                    if (BuzerOnOff == true)
                    {
                        if (700 <= (BuzzerLast - BuzzerFirst))
                        {
                            BuzerOnOff = false;
                            IOPort.BuzzerOnOff = BuzerOnOff;
                            BuzzerFirst = ComF.timeGetTimems();
                            BuzzerOnCount++;
                        }
                    }
                    else
                    {
                        if (500 <= (BuzzerLast - BuzzerFirst))
                        {
                            if (BuzzerOnCount < 3)
                            {
                                BuzerOnOff = true;
                                IOPort.BuzzerOnOff = BuzerOnOff;
                                BuzzerFirst = ComF.timeGetTimems();
                            }
                            else
                            {
                                BuzerOnOff = false;
                                IOPort.BuzzerOnOff = BuzerOnOff;
                                BuzzerOnCount = 0;
                                BuzzerRunFlag = false;
                            }
                        }
                    }
                }

                if (IOPort.GetAuto == false)
                {
                    if(led2.Indicator.Text != "MANUAL")
                    {
                        led2.Indicator.Text = "MANUAL";
                        if (panel1.Visible == true)
                            toolStripButton4.Visible = true;
                        else toolStripButton4.Visible = false;
                    }
                    if (toolStrip1.Enabled == false) toolStrip1.Enabled = true;

                    if (panel2.Visible == false)
                    {
                        if(IOPort.GetPinConnectSw == true)
                        {
                            if (IOPort.PinConnectionOnOff == false) IOPort.PinConnectionOnOff = true;
                        }
                        else
                        {
                            if (IOPort.PinConnectionOnOff == true) IOPort.PinConnectionOnOff = false;
                        }
                    }
                }
                else
                {
                    if (led2.Indicator.Text != "AUTO")
                    {
                        led2.Indicator.Text = "AUTO";
                        toolStripSeparator4.Visible = false;
                        IOPort.PinConnectionOnOff = false;
                    }
                    if (panel1.Visible == true)
                    {
                        if (toolStrip1.Enabled == true) toolStrip1.Enabled = false;
                    }                   
                }

                if(IOPort.GetProductIn == false)
                {
                    if (JigUpCount < 10)
                        JigUpCount++;
                    else if (JigUpFlag == true) JigUpFlag = false;
                    if(PopReadOkOld == true) PopReadOkOld = false;
                    if(ProductOutFlag == true) ProductOutFlag = false;
                    if (label16.Text != "")
                    {
                        label16.Text = "";
                        ScreenInit();
                    }
                    if (panel2.Visible == false)
                    {
                        if (IOPort.TestOKOnOff == true) IOPort.TestOKOnOff = false;
                        if (IOPort.TestNGOnOff == true) IOPort.TestNGOnOff = false;
                    }
                }
                else
                {
                    if (JigUpCount != 0) JigUpCount = 0;
                }

                if (RunningFlag == false)
                {
                    if (IOPort.GetAuto == true)
                    {
                        bool Flag = false;
                        
                        if(Config.AutoConnection == false)
                        {
                            if ((JigUpFlag == true) && (IOPort.GetProductIn == true) && (PopReadOk == true) && (ProductOutFlag == false) && (label16.Text == ""))  Flag = true;
                        }
                        else
                        {
                            if ((IOPort.GetStartSw == true) && (JigUpFlag == true) && (IOPort.GetProductIn == true) && (PopReadOk == true) && (ProductOutFlag == false) && (label16.Text == "")) Flag = true;
                        }

                        if (Flag == true) StartSetting();
                    }
                    else
                    {
                        if ((IOPort.GetStartSw == true) && (JigUpFlag == true) && (IOPort.GetProductIn == true) && (ProductOutFlag == false) && (label16.Text == "")) StartSetting();
                    }

                    if ((IOPort.GetResetSw == true) && (JigUpFlag == true) && (IOPort.GetProductIn == true) && (label16.Text != ""))
                    {
                        if (ProductOutFlag == true) ProductOutFlag = false;
                        PopReadOk = PopReadOkOld;
                        if(label16.Text != "") label16.Text = "";
                    }

                    if ((JigUpFlag == true) && (IOPort.GetProductIn == true) && (ProductOutFlag == true) && (label16.Text == "OK"))
                    {
                        if (IOPort.TestOKOnOff == false) IOPort.TestOKOnOff = true;
                    }
                    else if ((JigUpFlag == true) && (IOPort.GetProductIn == true) && (ProductOutFlag == true) && (label16.Text == "NG"))
                    {
                        if (IOPort.TestNGOnOff == false) IOPort.TestNGOnOff = true;
                    }
                }
                else
                {
                    if(IOPort.GetResetSw == true)
                    {
                        StopSetting();
                    }
                }

                if (IOPort.GetAuto == false)
                {
                    if (RunningFlag == false) IOInCheck();
                }
                if(FormLoadFirstSpecDisplay == true)
                {
                    if (1000 <= (ComF.timeGetTimems() - FormLoadFirstSpecDisplayTime))
                    {
                        FormLoadFirstSpecDisplay = false;
                        DisplaySpec();
                    }
                }
                if (sevenSegmentAnalog2.Value.AsDouble != pMeter.GetBatt) sevenSegmentAnalog2.Value.AsDouble = pMeter.GetBatt;
                if (sevenSegmentAnalog5.Value.AsDouble != pMeter.GetPSeat) sevenSegmentAnalog5.Value.AsDouble = pMeter.GetPSeat;
                if (sevenSegmentAnalog1.Value.AsDouble != Sound.GetSound) sevenSegmentAnalog1.Value.AsDouble = Sound.GetSound;
            }
            catch
            {

            }
            finally 
            {
                timer2.Enabled = !ExitFlag;
            }
        }

        private void StartSetting()
        {
            RunningFlag = true;
            Step = 0;
            SpecOutputFlag = false;
            PowerOnOff = true;
            label16.Text = "TESTING";
            label16.ForeColor = Color.Yellow;
            IOPort.YellowLampOnOff = true;
            IOPort.GreenLampOnOff = false;
            IOPort.RedLampOnOff = false;
            IOPort.TestINGOnOff = true;

            plot1.Channels[0].Clear();
            plot2.Channels[0].Clear();

            IOPort.FunctionIOInit();
            ScreenInit();
            ThreadSetting();
            ProductOutFlag = true;
            return;
        }

        private void ScreenInit()
        {
            for(int i = 2;i < fpSpread1.ActiveSheet.RowCount;i++)
            {
                fpSpread1.ActiveSheet.Cells[i, 6].Text = "";
                fpSpread1.ActiveSheet.Cells[i, 7].Text = "";
                fpSpread1.ActiveSheet.Cells[i, 7].ForeColor = Color.Black;
            }

            mData.Height.Data1 = 0;
            mData.Height.Result1 = RESULT.CLEAR;
            mData.Height.Data2 = 0;
            mData.Height.Result2 = RESULT.CLEAR;
            mData.Height.Test = false;

            mData.Recline.Data1 = 0;
            mData.Recline.Result1 = RESULT.CLEAR;
            mData.Recline.Data2 = 0;
            mData.Recline.Result2 = RESULT.CLEAR;
            mData.Recline.Test = false;
                                 
            mData.Relax.Data1 = 0;
            mData.Relax.Result1 = RESULT.CLEAR;
            mData.Relax.Data2 = 0;
            mData.Relax.Result2 = RESULT.CLEAR;
            mData.Relax.Test = false;

            mData.Legrest.Data1 = 0;
            mData.Legrest.Result1 = RESULT.CLEAR;
            mData.Legrest.Data2 = 0;
            mData.Legrest.Result2 = RESULT.CLEAR;
            mData.Legrest.Test = false;

            mData.LegrestExt.Data1 = 0;
            mData.LegrestExt.Result1 = RESULT.CLEAR;
            mData.LegrestExt.Data2 = 0;
            mData.LegrestExt.Result2 = RESULT.CLEAR;
            mData.LegrestExt.Test = false;

            mData.RelaxTime.Data = 0;
            mData.RelaxTime.Result = RESULT.CLEAR;
            mData.RelaxTime.Test = false;

            mData.RelaxReturnTime.Data = 0;
            mData.RelaxReturnTime.Result = RESULT.CLEAR;
            mData.RelaxReturnTime.Test = false;

            mData.ReclineFwdTime.Data = 0;
            mData.ReclineFwdTime.Result = RESULT.CLEAR;
            mData.ReclineFwdTime.Test = false;

            mData.ReclineBwdTime.Data = 0;
            mData.ReclineBwdTime.Result = RESULT.CLEAR;
            mData.ReclineBwdTime.Test = false;
            
            mData.LegrestTime.Data = 0;
            mData.LegrestTime.Result = RESULT.CLEAR;
            mData.LegrestTime.Test = false;

            mData.LegrestReturnTime.Data = 0;
            mData.LegrestReturnTime.Result = RESULT.CLEAR;
            mData.LegrestReturnTime.Test = false;

            mData.LegrestExtTime.Data = 0;
            mData.LegrestExtTime.Result = RESULT.CLEAR;
            mData.LegrestExtTime.Test = false;

            mData.LegrestExtReturnTime.Data = 0;
            mData.LegrestExtReturnTime.Result = RESULT.CLEAR;
            mData.LegrestExtReturnTime.Test = false;


            mData.Result = RESULT.CLEAR;
            mData.Time = "";

            mData.ReclineFwdSound.Test = false;
            mData.ReclineFwdSound.ResultStart = RESULT.CLEAR;
            mData.ReclineFwdSound.ResultRun = RESULT.CLEAR;
            mData.ReclineFwdSound.StartData = 0;
            mData.ReclineFwdSound.RunData = 0;

            mData.ReclineBwdSound.Test = false;
            mData.ReclineBwdSound.ResultStart = RESULT.CLEAR;
            mData.ReclineBwdSound.ResultRun = RESULT.CLEAR;
            mData.ReclineBwdSound.StartData = 0;
            mData.ReclineBwdSound.RunData = 0;


            mData.HeightUpSound.Test = false;
            mData.HeightUpSound.ResultStart = RESULT.CLEAR;
            mData.HeightUpSound.ResultRun = RESULT.CLEAR;
            mData.HeightUpSound.StartData = 0;
            mData.HeightUpSound.RunData = 0;

            mData.HeightDnSound.Test = false;
            mData.HeightDnSound.ResultStart = RESULT.CLEAR;
            mData.HeightDnSound.ResultRun = RESULT.CLEAR;
            mData.HeightDnSound.StartData = 0;
            mData.HeightDnSound.RunData = 0;

            mData.RelaxSound.Test = false;
            mData.RelaxSound.ResultStart = RESULT.CLEAR;
            mData.RelaxSound.ResultRun = RESULT.CLEAR;
            mData.RelaxSound.StartData = 0;
            mData.RelaxSound.RunData = 0;

            mData.RelaxReturnSound.Test = false;
            mData.RelaxReturnSound.ResultStart = RESULT.CLEAR;
            mData.RelaxReturnSound.ResultRun = RESULT.CLEAR;
            mData.RelaxReturnSound.StartData = 0;
            mData.RelaxReturnSound.RunData = 0;

            mData.LegrestSound.Test = false;
            mData.LegrestSound.ResultStart = RESULT.CLEAR;
            mData.LegrestSound.ResultRun = RESULT.CLEAR;
            mData.LegrestSound.StartData = 0;
            mData.LegrestSound.RunData = 0;

            mData.LegrestReturnSound.Test = false;
            mData.LegrestReturnSound.ResultStart = RESULT.CLEAR;
            mData.LegrestReturnSound.ResultRun = RESULT.CLEAR;
            mData.LegrestReturnSound.StartData = 0;
            mData.LegrestReturnSound.RunData = 0;

            mData.LegrestExtSound.Test = false;
            mData.LegrestExtSound.ResultStart = RESULT.CLEAR;
            mData.LegrestExtSound.ResultRun = RESULT.CLEAR;
            mData.LegrestExtSound.StartData = 0;
            mData.LegrestExtSound.RunData = 0;

            mData.LegrestExtReturnSound.Test = false;
            mData.LegrestExtReturnSound.ResultStart = RESULT.CLEAR;
            mData.LegrestExtReturnSound.ResultRun = RESULT.CLEAR;
            mData.LegrestExtReturnSound.StartData = 0;
            mData.LegrestExtReturnSound.RunData = 0;

            return;
        }

        private void StopSetting()
        {
            if (Config.AutoConnection == true)
            {
                IOPort.PinConnectionOnOff = false;
                ComF.timedelay(1000);
            }
            RunningFlag = false;
            PowerOnOff = false;
            label16.Text = "N.G";
            label16.ForeColor = Color.Red;
            IOPort.YellowLampOnOff = false;
            IOPort.GreenLampOnOff = false;
            IOPort.RedLampOnOff = true;
            IOPort.TestINGOnOff = false;
            return;
        }

        private bool PowerOnOff
        {
            set
            {
                if(value == true)
                {
                    PwCtrl.POWER_PWSetting(mSpec.TestVolt);
                    PwCtrl.POWER_PWON();
                }
                else
                {
                    PwCtrl.POWER_PWSetting(0);
                    PwCtrl.POWER_PWOFF();
                }
            }
        }

        private bool BattOnOff
        {
            set
            {
                IOPort.SetPSeatBatt = value;
                IOPort.SetIgn1 = value;
                IOPort.SetIgn2 = value;
            }
        }

        private bool PopReadOk { get; set; }
        private bool PopReadOkOld { get; set; }


        private void IMSSetButton(short Pos)
        {
            if (Pos == 0)
                CanReWrite.CanDataOutput(OUT_CAN_LIST.C_AVNIMSButtonCmd, (byte)C_AVNIMSButtonCmd.Data.Memory_P1_CMD);
            else CanReWrite.CanDataOutput(OUT_CAN_LIST.C_AVNIMSButtonCmd, (byte)C_AVNIMSButtonCmd.Data.Memory_P2_CMD);
            ComF.timedelay(600);
            CanReWrite.CanDataOutput(OUT_CAN_LIST.C_AVNIMSButtonCmd, (byte)C_AVNIMSButtonCmd.Data.Default);

            return;
        }

        private void IMSM1Button()
        {
            CanReWrite.CanDataOutput(OUT_CAN_LIST.C_AVNIMSButtonCmd, (byte)C_AVNIMSButtonCmd.Data.PBack_P1_CMD);
            ComF.timedelay(600);
            CanReWrite.CanDataOutput(OUT_CAN_LIST.C_AVNIMSButtonCmd, (byte)C_AVNIMSButtonCmd.Data.Default);
            return;
        }
        private void IMSM2Button()
        {
            CanReWrite.CanDataOutput(OUT_CAN_LIST.C_AVNIMSButtonCmd, (byte)C_AVNIMSButtonCmd.Data.PBack_P2_CMD);
            ComF.timedelay(600);
            CanReWrite.CanDataOutput(OUT_CAN_LIST.C_AVNIMSButtonCmd, (byte)C_AVNIMSButtonCmd.Data.Default);

            return;
        }

        //private void IMS1Set()
        //{
        //    if (comboBox4.SelectedItem.ToString() == "IMS")
        //    {
        //        IMSSetButton(0);
        //    }
        //    return;
        //}

        //private void IMS2Set()
        //{
        //    if (comboBox4.SelectedItem.ToString() == "IMS")
        //    {
        //        IMSSetButton(1);
        //    }
        //    return;
        //}


        private bool RelaxOnOff
        {
            set
            {
                IOPort.SetRelax = value;
            }
            get
            {
                return IOPort.SetRelax;
            }
        }


        private bool RelaxReturnOnOff
        {
            set
            {
                IOPort.SetRelaxReturn = value;
            }
            get
            {
                return IOPort.SetRelaxReturn;
            }
        }
        private bool ReclineFwdOnOff
        {
            set
            {
                IOPort.SetReclineFwd = value;
            }
            get
            {
                return IOPort.SetReclineFwd;
            }
        }
        private bool ReclineBwdOnOff
        {
            set
            {
                IOPort.SetReclineBwd = value;
            }
            get
            {
                return IOPort.SetReclineBwd;
            }
        }
        

        private bool LegrestOnOff
        {
            set
            {
                IOPort.SetLegrest = value;
            }
            get
            {
                return IOPort.SetLegrest;
            }
        }

        private bool LegrestReturnOnOff
        {
            set
            {
                IOPort.SetLegrestReturn = value;
            }
            get
            {
                return IOPort.SetLegrestReturn;
            }
        }

        private bool HeightUpOnOff
        {
            set
            {
                IOPort.SetHeightUp = value;
            }
            get
            {
                return IOPort.SetHeightUp;
            }
        }

        private bool HeightDnOnOff
        {
            set
            {
                IOPort.SetHeightDown = value;
            }
            get
            {
                return IOPort.SetHeightDown;
            }
        }

        private bool LegrestExtOnOff
        {
            set
            {
                IOPort.SetLegrestExt = value;
            }
            get
            {
                return IOPort.SetLegrestExt;
            }
        }
        private bool LegrestExtReturnOnOff
        {
            set
            {
                IOPort.SetLegrestExtReturn = value;
            }
            get
            {
                return IOPort.SetLegrestExtReturn;
            }
        }
        

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (toolStripButton6.Text == "로그인")
            {
                MessageBox.Show(this, "로그인을 먼저 진행하십시오.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (RunningFlag == false)
            {
                SystemSet SysSet = new SystemSet(this)
                {
                    Owner = this,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ControlBox = false,
                    FormBorderStyle = FormBorderStyle.FixedSingle,
                    WindowState = FormWindowState.Normal,
                    StartPosition = FormStartPosition.CenterParent
                };

                SysSet.FormClosing += delegate (object sender1, FormClosingEventArgs e1)
                {
                    e1.Cancel = false;
                    SysSet.Dispose();
                    SysSet = null;
                };

                SysSet.Show();
            }
            return;
        }
        private SpecSetting SpecForm = null;
        private bool ModelBoxChangeFlag = false;
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (toolStripButton6.Text == "로그인")
            {
                //MessageBox.Show("로그인을 먼저 진행하십시오.", "경고");
                //uMessageBox.Show(promptText: "로그인을 먼저 진행하십시오.", title: "경고");
                MessageBox.Show(this, "로그인을 먼저 진행하십시오.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (IOPort.GetAuto == false)
            {
                if (SpecForm == null)
                {
                    OpenFormClose();
                    panel1.SendToBack();
                    panel1.Visible = false;

                    panel2.Visible = true;
                    if (panel2.Parent != this) panel2.Parent = this;
                    panel2.BringToFront();
                    SpecForm = new SpecSetting(this, mSpec.ModelName)
                    {
                        MaximizeBox = false,
                        MinimizeBox = false,
                        ControlBox = false,
                        ShowIcon = false,
                        StartPosition = FormStartPosition.CenterScreen,
                        WindowState = FormWindowState.Maximized,
                        TopMost = false,
                        TopLevel = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Location = new Point(0, 0),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
                    };

                    SpecForm.FormClosing += delegate (object sender1, FormClosingEventArgs e1)
                    {
                        e1.Cancel = false;
                        SpecForm.Parent = null;
                        SpecForm.Dispose();
                        SpecForm = null;

                        ComF.ReadFileListNotExt(Program.SPEC_PATH.ToString(), "*.Spc", COMMON_FUCTION.FileSortMode.FILENAME_ODERBY);
                        List<string> FList = ComF.GetFileList;

                        if (0 < FList.Count)
                        {
                            ModelBoxChangeFlag = true;
                            comboBox1.Items.Clear();
                            foreach (string s in FList) comboBox1.Items.Add(s);

                            if (0 < comboBox1.Items.Count)
                            {
                                if ((mSpec.ModelName != null) && (mSpec.ModelName != "") && (mSpec.ModelName != string.Empty))
                                {
                                    if (comboBox1.Items.Contains(mSpec.ModelName) == true) comboBox1.SelectedItem = mSpec.ModelName;
                                }
                            }
                            if (mSpec.ModelName != null)
                            {
                                string sName = Program.SPEC_PATH.ToString() + "\\" + mSpec.ModelName + ".Spc";
                                ComF.OpenSpec(sName, ref mSpec);
                                DisplaySpec();
                            }
                            ModelBoxChangeFlag = false;
                        }
                    };

                    SpecForm.Parent = panel2;
                    SpecForm.Show();
                }
            }
            return;
        }

        private void DisplaySpec()
        {
            if ((CheckItem.Recline == true) || (CheckItem.Relax == true) || (CheckItem.Height == true) || (CheckItem.Legrest == true) || (CheckItem.LegrestExit == true))
            {
                fpSpread1.ActiveSheet.Cells[2, 0].BackColor = SelectColor;
                fpSpread1.ActiveSheet.Cells[2, 1].BackColor = SelectColor;

                fpSpread1.ActiveSheet.Cells[7, 0].BackColor = SelectColor;
                fpSpread1.ActiveSheet.Cells[7, 1].BackColor = SelectColor;

                fpSpread1.ActiveSheet.Cells[17, 0].BackColor = SelectColor;
                fpSpread1.ActiveSheet.Cells[17, 1].BackColor = SelectColor;

                if (CheckItem.Recline == true)
                {
                    fpSpread1.ActiveSheet.Cells[2, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[2, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[2, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[7, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[8, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[7, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[7, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[8, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[8, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[17, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[18, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[17, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[18, 4].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[2, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[2, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[2, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[7, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[8, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[7, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[7, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[8, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[8, 4].ForeColor = Color.Silver;

                    fpSpread1.ActiveSheet.Cells[17, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[18, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[17, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[18, 4].ForeColor = Color.Silver;
                }

                if (CheckItem.Relax == true)
                {
                    fpSpread1.ActiveSheet.Cells[3, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[3, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[3, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[9, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[10, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[9, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[9, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[10, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[10, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[19, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[20, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[19, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[20, 4].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[3, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[3, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[3, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[9, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[10, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[9, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[9, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[10, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[10, 4].ForeColor = Color.Silver;

                    fpSpread1.ActiveSheet.Cells[19, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[20, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[19, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[20, 4].ForeColor = Color.Silver;

                }
                if (CheckItem.Height == true)
                {
                    fpSpread1.ActiveSheet.Cells[4, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[4, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[4, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[11, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[12, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[11, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[11, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[12, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[12, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[21, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[22, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[21, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[22, 4].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[4, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[4, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[4, 4].ForeColor = Color.Silver;

                    fpSpread1.ActiveSheet.Cells[11, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[12, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[11, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[11, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[12, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[12, 4].ForeColor = Color.Silver;

                    fpSpread1.ActiveSheet.Cells[21, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[22, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[21, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[22, 4].ForeColor = Color.Silver;
                }
                if (CheckItem.Legrest == true)
                {
                    fpSpread1.ActiveSheet.Cells[5, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[5, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[5, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[13, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[14, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[13, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[13, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[14, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[14, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[23, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[24, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[23, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[24, 4].ForeColor = Color.Black;

                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[5, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[5, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[5, 4].ForeColor = Color.Silver;

                    fpSpread1.ActiveSheet.Cells[13, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[14, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[13, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[13, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[14, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[14, 4].ForeColor = Color.Silver;

                    fpSpread1.ActiveSheet.Cells[23, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[24, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[23, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[24, 4].ForeColor = Color.Silver;
                }
                if (CheckItem.LegrestExit == true)
                {
                    fpSpread1.ActiveSheet.Cells[6, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[6, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[6, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[15, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[16, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[15, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[15, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[16, 3].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[16, 4].ForeColor = Color.Black;

                    fpSpread1.ActiveSheet.Cells[25, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[26, 2].BackColor = SelectColor;
                    fpSpread1.ActiveSheet.Cells[25, 4].ForeColor = Color.Black;
                    fpSpread1.ActiveSheet.Cells[26, 4].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[6, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[6, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[6, 4].ForeColor = Color.Silver;

                    fpSpread1.ActiveSheet.Cells[15, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[16, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[15, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[15, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[16, 3].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[16, 4].ForeColor = Color.Silver;

                    fpSpread1.ActiveSheet.Cells[25, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[26, 2].BackColor = NotSelectColor;
                    fpSpread1.ActiveSheet.Cells[25, 4].ForeColor = Color.Silver;
                    fpSpread1.ActiveSheet.Cells[26, 4].ForeColor = Color.Silver;
                }
            }
            else
            {
                fpSpread1.ActiveSheet.Cells[2, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[2, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[2, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[3, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[4, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[5, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[6, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[7, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[7, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[7, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[8, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[9, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[10, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[11, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[12, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[13, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[14, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[15, 2].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[16, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[17, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[17, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[17, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[18, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[18, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[18, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[19, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[19, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[19, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[20, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[20, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[20, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[21, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[21, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[21, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[22, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[22, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[22, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[23, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[23, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[23, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[24, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[24, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[24, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[25, 0].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[25, 1].BackColor = NotSelectColor;
                fpSpread1.ActiveSheet.Cells[25, 2].BackColor = NotSelectColor;

                fpSpread1.ActiveSheet.Cells[2, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[2, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[3, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[3, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[4, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[4, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[5, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[5, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[6, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[6, 4].ForeColor = Color.Silver;

                fpSpread1.ActiveSheet.Cells[7, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[7, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[8, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[8, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[9, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[9, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[10, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[10, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[11, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[11, 4].ForeColor = Color.Silver;

                fpSpread1.ActiveSheet.Cells[12, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[12, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[13, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[13, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[14, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[14, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[15, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[15, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[16, 3].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[16, 4].ForeColor = Color.Silver;

                fpSpread1.ActiveSheet.Cells[17, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[18, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[19, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[20, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[21, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[22, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[23, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[24, 4].ForeColor = Color.Silver;
                fpSpread1.ActiveSheet.Cells[25, 4].ForeColor = Color.Silver;
            }

            fpSpread1.ActiveSheet.Cells[2, 3].Text = mSpec.Current.Recliner.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[2, 4].Text = mSpec.Current.Recliner.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[7, 3].Text = mSpec.MovingSpeed.ReclinerFwd.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[7, 4].Text = mSpec.MovingSpeed.ReclinerFwd.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[8, 3].Text = mSpec.MovingSpeed.ReclinerBwd.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[8, 4].Text = mSpec.MovingSpeed.ReclinerBwd.Max.ToString("0.00");


            fpSpread1.ActiveSheet.Cells[3, 3].Text = mSpec.Current.Relax.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[3, 4].Text = mSpec.Current.Relax.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[9, 3].Text = mSpec.MovingSpeed.Relax.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[9, 4].Text = mSpec.MovingSpeed.Relax.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[10, 3].Text = mSpec.MovingSpeed.RelaxReturn.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[10, 4].Text = mSpec.MovingSpeed.RelaxReturn.Max.ToString("0.00");

            fpSpread1.ActiveSheet.Cells[4, 3].Text = mSpec.Current.Height.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[4, 4].Text = mSpec.Current.Height.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[11, 3].Text = mSpec.MovingSpeed.HeightUp.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[11, 4].Text = mSpec.MovingSpeed.HeightUp.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[12, 3].Text = mSpec.MovingSpeed.HeightDn.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[12, 4].Text = mSpec.MovingSpeed.HeightDn.Max.ToString("0.00");

            fpSpread1.ActiveSheet.Cells[5, 3].Text = mSpec.Current.Legrest.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[5, 4].Text = mSpec.Current.Legrest.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[13, 3].Text = mSpec.MovingSpeed.Legrest.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[13, 4].Text = mSpec.MovingSpeed.Legrest.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[14, 3].Text = mSpec.MovingSpeed.LegrestReturn.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[14, 4].Text = mSpec.MovingSpeed.LegrestReturn.Max.ToString("0.00");

            fpSpread1.ActiveSheet.Cells[6, 3].Text = mSpec.Current.LegrestExt.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[6, 4].Text = mSpec.Current.LegrestExt.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[15, 3].Text = mSpec.MovingSpeed.LegrestExt.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[15, 4].Text = mSpec.MovingSpeed.LegrestExt.Max.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[16, 3].Text = mSpec.MovingSpeed.LegrestExtReturn.Min.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[16, 4].Text = mSpec.MovingSpeed.LegrestExtReturn.Max.ToString("0.00");


            fpSpread1.ActiveSheet.Cells[17, 4].Text = mSpec.Sound.StartMax.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[18, 4].Text = mSpec.Sound.RunMax.ToString("0.00");

            fpSpread1.ActiveSheet.Cells[19, 4].Text = mSpec.Sound.StartMax.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[20, 4].Text = mSpec.Sound.RunMax.ToString("0.00");

            fpSpread1.ActiveSheet.Cells[21, 4].Text = mSpec.Sound.StartMax.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[22, 4].Text = mSpec.Sound.RunMax.ToString("0.00");

            fpSpread1.ActiveSheet.Cells[23, 4].Text = mSpec.Sound.StartMax.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[24, 4].Text = mSpec.Sound.RunMax.ToString("0.00");

            fpSpread1.ActiveSheet.Cells[25, 4].Text = mSpec.Sound.StartMax.ToString("0.00");
            fpSpread1.ActiveSheet.Cells[26, 4].Text = mSpec.Sound.RunMax.ToString("0.00");
            return;
        }
        public IOControl GetIO
        {
            get { return IOPort; }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.Location = new Point(1, 1);
            return;
        }

        //private bool ImsSetButton { get; set; }
        //private bool ImsM1Button { get; set; }
        //private bool ImsM2Button { get; set; }

        //private long ImsSetButtonFirst = 0;
        //private long ImsSetButtonLast = 0;

        private void IOInCheck()
        {
            bool Flag = false;


            if (IOPort.GetRHSelect == true)
            {
                if (comboBox3.SelectedItem == null) comboBox3.SelectedItem = "RH";
                if (comboBox3.SelectedItem.ToString() != "RH")
                {
                    comboBox3.SelectedItem = "RH";
                    CheckItem.LhRh = LH_RH.RH;
                    Flag = true;
                }
            }
            else
            {
                if (comboBox3.SelectedItem == null) comboBox3.SelectedItem = "LH";
                if (comboBox3.SelectedItem.ToString() != "LH")
                {
                    comboBox3.SelectedItem = "LH";
                    CheckItem.LhRh = LH_RH.LH;
                    Flag = true;
                }
            }

            if (IOPort.GetSeatRelax == true)
            {
                if (comboBox4.SelectedItem == null) comboBox4.SelectedItem = "RELAX";
                
                if (comboBox4.SelectedItem.ToString() != "RELAX")
                {
                    comboBox4.SelectedItem = "RELAX";
                    //CheckItem.Can = true;
                    Flag = true;
                }
            }
            else //if (IOPort.GetSeatPower == true) 
            {
                if (comboBox4.SelectedItem == null) comboBox4.SelectedItem = "RELAX";
                if (comboBox4.SelectedItem.ToString() != "POWER")
                {
                    comboBox4.SelectedItem = "POWER";
                    CheckItem.Can = false;
                    Flag = true;
                }
            }

            if (IOPort.GetTestSetMode == true)
            {
                if(IOPort.GetReclinerFwd == true)
                {
                    if (CheckItem.Recline == false) Flag = true;
                    CheckItem.Recline = true;
                }
                else if (IOPort.GetReclinerBwd == true)
                {
                    if (CheckItem.Recline == true) Flag = true;
                    CheckItem.Recline = false;
                }

                if (IOPort.GetRelax_Relax == true)
                {
                    if (CheckItem.Relax == false) Flag = true;
                    CheckItem.Relax = true;
                }
                else if(IOPort.GetRelaxReturn == true)
                {
                    if (CheckItem.Relax == true) Flag = true;
                    CheckItem.Relax = false;
                }
                if (IOPort.GetHeightUp == true)
                {
                    if (CheckItem.Height == false) Flag = true;
                    CheckItem.Height = true;
                }
                else if (IOPort.GetHeightDn == true)
                {
                    if (CheckItem.Height == true) Flag = true;
                    CheckItem.Height = false;
                }

                if (IOPort.GetLegrest_Rest == true) 
                {
                    if (CheckItem.Legrest == false) Flag = true;
                    CheckItem.Legrest = true;
                }
                else if (IOPort.GetLegrest_Return == true)
                {
                    if (CheckItem.Legrest == true) Flag = true;
                    CheckItem.Legrest = false;
                }

                if (IOPort.GetLegrestExt == true)
                {
                    if (CheckItem.LegrestExit == false) Flag = true;
                    CheckItem.LegrestExit = true;
                }
                else if (IOPort.GetLegrestExtReturn == true)
                {
                    if (CheckItem.LegrestExit == true) Flag = true;
                    CheckItem.LegrestExit = false;
                }
            }
            else
            {
                if (ledArrow4.Value.AsBoolean != IOPort.GetReclinerFwd) ledArrow4.Value.AsBoolean = IOPort.GetReclinerFwd;
                if (ledArrow3.Value.AsBoolean != IOPort.GetReclinerBwd) ledArrow3.Value.AsBoolean = IOPort.GetReclinerBwd;

                if (ledArrow7.Value.AsBoolean != IOPort.GetRelax_Relax) ledArrow7.Value.AsBoolean = IOPort.GetRelax_Relax;
                if (ledArrow8.Value.AsBoolean != IOPort.GetRelaxReturn) ledArrow8.Value.AsBoolean = IOPort.GetRelaxReturn;

                if (ledArrow5.Value.AsBoolean != IOPort.GetHeightUp) ledArrow5.Value.AsBoolean = IOPort.GetHeightUp;
                if (ledArrow6.Value.AsBoolean != IOPort.GetHeightDn) ledArrow6.Value.AsBoolean = IOPort.GetHeightDn;

                if (ledArrow11.Value.AsBoolean != IOPort.GetLegrest_Rest) ledArrow11.Value.AsBoolean = IOPort.GetLegrest_Rest;
                if (ledArrow12.Value.AsBoolean != IOPort.GetLegrest_Return) ledArrow12.Value.AsBoolean = IOPort.GetLegrest_Return;

                if (ledArrow9.Value.AsBoolean != IOPort.GetLegrestExt) ledArrow9.Value.AsBoolean = IOPort.GetLegrestExt;
                if (ledArrow10.Value.AsBoolean != IOPort.GetLegrestExtReturn) ledArrow10.Value.AsBoolean = IOPort.GetLegrestExtReturn;
            }
            //----------------------------------------------------------------------
            //사양에 따라 모델 선택을 진행한다.

            if(Flag == true)
            {
                string LHType = comboBox3.SelectedItem.ToString();
                string PSeat = comboBox4.SelectedItem.ToString();
                string s = comboBox1.SelectedItem.ToString();
                //string LHDType;
                string RELAXType;
                string ModelType;

                //if (CheckItem.LhdRhd == LHD_RHD.LHD)
                //    LHDType = "LHD";
                //else LHDType = "RHD";

                RELAXType = comboBox4.SelectedItem.ToString();

                if (comboBox3.SelectedItem != null)
                    LHType = comboBox3.SelectedItem.ToString();
                else LHType = "LH";

                ModelType = RELAXType + "_" + LHType + "_" + PSeat;

                if (s.IndexOf(ModelType) < 0)
                {
                    foreach (string Item in comboBox1.Items)
                    {
                        if (0 <= Item.IndexOf(ModelType))
                        {
                            comboBox1.SelectedItem = Item;
                            Flag = false;
                            break;
                        }
                    }
                }
            }
            
            //----------------------------------------------------------------------
            if (Flag == true) DisplaySpec();
            return;
        }

        private BackgroundWorker backgroundWorker1 = null;
        private void ThreadSetting()
        {
            backgroundWorker1 = new BackgroundWorker();

            //ReportProgress메소드를 호출하기 위해서 반드시 true로 설정, false일 경우 ReportProgress메소드를 호출하면 exception 발생
            backgroundWorker1.WorkerReportsProgress = true;
            //스레드에서 취소 지원 여부
            backgroundWorker1.WorkerSupportsCancellation = true;
            //스레드가 run시에 호출되는 핸들러 등록
            backgroundWorker1.DoWork += new DoWorkEventHandler(BackgroundWorker1_DoWork);
            // ReportProgress메소드 호출시 호출되는 핸들러 등록
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorker1_ProgressChanged);
            // 스레드 완료(종료)시 호출되는 핸들러 동록
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker1_RunWorkerCompleted);


            // 스레드가 Busy(즉, run)가 아니라면
            if (backgroundWorker1.IsBusy != true)
            {
                // 스레드 작동!! 아래 함수 호출 시 위에서 bw.DoWork += new DoWorkEventHandler(bw_DoWork); 에 등록한 핸들러가
                // 호출 됩니다.

                backgroundWorker1.RunWorkerAsync();
            }
            return;
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //바로 위에서 worker.ReportProgress((i * 10));호출 시 
            // bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged); 등록한 핸들러가 호출 된다고
            // 하였는데요.. 이 부분에서는 기존 Thread에서 처럼 Dispatcher를 이용하지 않아도 됩니다. 
            // 즉 아래처럼!!사용이 가능합니다.
            //this.tbProgress.Text = (e.ProgressPercentage.ToString() + "%");

            // 기존의 Thread클래스에서 아래와 같이 UI 엘리먼트를 갱신하려면
            // Dispatcher.BeginInvoke(delegate() 
            // {
            //        this.tbProgress.Text = (e.ProgressPercentage.ToString() + "%");
            // )};
            //처럼 처리해야 할 것입니다. 그러나 바로 UI 엘리먼트를 업데이트 하고 있죠??
        }


        //스레드의 run함수가 종료될 경우 해당 핸들러가 호출됩니다.
        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            //스레드가 종료한 이유(사용자 취소, 완료, 에러)에 맞쳐 처리하면 됩니다.
            if ((e.Cancelled == true))
            {
            }
            else if (!(e.Error == null))
            {

            }
            else
            {

            }
        }


        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            do
            {
                //CancellationPending 속성이 true로 set되었다면(위에서 CancelAsync 메소드 호출 시 true로 set된다고 하였죠?
                if ((worker.CancellationPending == true))
                {
                    //루프를 break한다.(즉 스레드 run 핸들러를 벗어나겠죠)
                    e.Cancel = true;
                    break;
                }
                else
                {
                    // 이곳에는 스레드에서 처리할 연산을 넣으시면 됩니다.

                    Processing();

                    Thread.Sleep(1);
                    // 스레드 진행상태 보고 - 이 메소드를 호출 시 위에서 
                    // bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged); 등록한 핸들러가 호출 됩니다.
                    worker.ReportProgress(10);
                }
                if ((ExitFlag == true) || (RunningFlag == false))
                {
                    worker.CancelAsync();
                }
            } while (true);
            //while (ExitFlag == false);
        }
        private bool SpecOutputFlag { get; set; }
        private short Step { get; set; }
        private short SubStep { get; set; }

        private double NormalSound { get; set; }
        private long StepTimeFirst = 0;
        private long StepTimeLast = 0;
        private void Processing()
        {
            switch (Step)
            {
                case 0:
                    if (SpecOutputFlag == false)
                    {
                        if (Config.AutoConnection == true) IOPort.PinConnectionOnOff = true;
                        mData.Time = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();

                        SubStep = 0;

                        SpecOutputFlag = true;
                        CurrFilter.InitAll();
                        StepTimeFirst = ComF.timeGetTimems();
                        StepTimeLast = ComF.timeGetTimems();

                        NormalSound = 0;
                    }
                    else
                    {
                        if (Config.AutoConnection == true)
                        {
                            StepTimeLast = ComF.timeGetTimems();
                            if ((long)(Config.PinConnectionDelay * 1000) <= (StepTimeLast - StepTimeFirst))
                            {
                                SpecOutputFlag = false;
                                Step++;
                            }
                            else
                            {
                                if(IOPort.GetPinConnectFwd == true)
                                {
                                    SpecOutputFlag = false;
                                    Step++;
                                }
                            }
                        }
                        else
                        {
                            SpecOutputFlag = false;
                            Step++;
                        }
                    }
                    //Jig On
                    break;
                case 1:
                    //Batt
                    if (SpecOutputFlag == false)
                    {
                        BattOnOff = true;
                        StepTimeFirst = ComF.timeGetTimems();
                        StepTimeLast = ComF.timeGetTimems();
                        SpecOutputFlag = true;
                    }
                    else
                    {
                        StepTimeLast = ComF.timeGetTimems();
                        if (500 <= (StepTimeLast - StepTimeFirst))
                        {
                            Step++;
                            SpecOutputFlag = false;
                        }
                    }
                    break;                                
                case 2:
                    if (CheckItem.Recline == true)
                    {
                        ReclineCheck(0);
                    }
                    else
                    {
                        Step++;
                        SpecOutputFlag = false;
                    }
                    break;
                case 3:
                    //if (CheckItem.Recline == true)
                    //{
                    //    ReclineCheck(1);
                    //}
                    //else
                    //{
                    //    Step++;
                    //    SpecOutputFlag = false;
                    //}
                    Step++;
                    SpecOutputFlag = false;
                    break;
                case 4:
                    if (CheckItem.Relax == true)
                    {
                        RelaxCheck(0);
                    }
                    else
                    {
                        Step++;
                        SpecOutputFlag = false;
                    }
                    break;
                case 5:
                    //if (CheckItem.Relax == true)
                    //{
                    //    RelaxCheck(1);
                    //}
                    //else
                    //{
                    //    Step++;
                    //    SpecOutputFlag = false;
                    //}
                    Step++;
                    SpecOutputFlag = false;
                    break;
                case 6:
                    if (CheckItem.Height == true)
                    {
                        HeightCheck(0);
                    }
                    else
                    {
                        Step++;
                        SpecOutputFlag = false;
                    }
                    break;
                case 7:
                    //if (CheckItem.Height == true)
                    //{
                    //    HeightCheck(1);
                    //}
                    //else
                    //{
                    //    Step++;
                    //    SpecOutputFlag = false;
                    //}
                    Step++;
                    SpecOutputFlag = false;
                    break;
                case 8:
                    if (CheckItem.Legrest == true)
                    {
                        LegrestCheck(0);
                    }
                    else
                    {
                        Step++;
                        SpecOutputFlag = false;
                    }
                    break;
                case 9:
                    //if (CheckItem.Legrest == true)
                    //{
                    //    LegrestCheck(1);
                    //}
                    //else
                    //{
                    //    Step++;
                    //    SpecOutputFlag = false;
                    //}
                    Step++;
                    SpecOutputFlag = false;
                    break;
                case 10:
                    if (CheckItem.LegrestExit == true)
                    {
                        LegrestExxtCheck(0);
                    }
                    else
                    {
                        Step++;
                        SpecOutputFlag = false;
                    }
                    break;
                case 11:
                    //if (CheckItem.LegrestExit == true)
                    //{
                    //    LegrestExxtCheck(1);
                    //}
                    //else
                    //{
                    //    Step++;
                    //    SpecOutputFlag = false;
                    //}
                    Step++;
                    SpecOutputFlag = false;
                    break;
                case 12:
                    SoundCheck();
                    break;
                case 13:
                    DeliveryPosMoveing();
                    break;                
                case 14:
                    ComF.timedelay(300);
                    ResultCheck();
                    if (Config.AutoConnection == true) IOPort.PinConnectionOnOff = false;
                    StepTimeFirst = ComF.timeGetTimems();
                    StepTimeLast = ComF.timeGetTimems();
                    SpecOutputFlag = false;
                    Step++;
                    break;
                default:
                    if (Config.AutoConnection == true)
                    {
                        StepTimeLast = ComF.timeGetTimems();
                        if ((long)(Config.PinConnectionDelay * 1000) <= (StepTimeLast - StepTimeFirst))
                        {
                            IOPort.TestINGOnOff = false;
                            if (mData.Result != RESULT.REJECT)
                            {
                                IOPort.TestNGOnOff = true;
                                SendTestData();
                            }
                            else
                            {
                                IOPort.TestOKOnOff = true;
                                TestEndResultToReject = true;
                                SendTestData();
                            }

                            RunningFlag = false;
                            SaveData();
                        }
                        else
                        {
                            if(IOPort.GetPinConnectBwd == true)
                            {
                                IOPort.TestINGOnOff = false;
                                if (mData.Result != RESULT.REJECT)
                                {
                                    IOPort.TestNGOnOff = true;
                                    SendTestData();
                                }
                                else
                                {
                                    IOPort.TestOKOnOff = true;
                                    TestEndResultToReject = true;
                                    SendTestData();
                                }

                                RunningFlag = false;
                                SaveData();
                            }
                        }
                    }
                    else
                    {
                        IOPort.TestINGOnOff = false;
                        if (mData.Result != RESULT.REJECT)
                        {
                            IOPort.TestNGOnOff = true;
                            SendTestData();
                        }
                        else
                        {
                            IOPort.TestOKOnOff = true;
                            TestEndResultToReject = true;
                            SendTestData();
                        }

                        RunningFlag = false;
                        SaveData();
                    }
                    break;
            }
            return;
        }

        private bool TestEndResultToReject { get; set; }
        private long RelaxRunTime { get; set; }
        private bool RelaxRunTimeFlag { get; set; }
        private long RunTime;
        private long RunTimeOld;

        private void RelaxCheck(short Pos)
        {
            if (SpecOutputFlag == false)
            {
                if(Pos == 0)
                    label5.Text = "RELAX 전류 측정 중 입니다.";
                else label5.Text = "RELAX RETURN 전류 측정 중 입니다.";
                StepTimeFirst = ComF.timeGetTimems();
                StepTimeLast = ComF.timeGetTimems();
                SpecOutputFlag = true;

                //Relax
                if (Pos == 0)
                    RelaxOnOff = true;
                else RelaxReturnOnOff = true;
                plot2.Channels[0].Clear();
                RelaxRunTimeFlag = false;
                RunTime = 0;
                RunTimeOld = 0;
                CurrFilter.InitAll();
            }
            else
            {
                float AdData;

                if ((mSpec.RelaxSwOnTime * 1000) <= (StepTimeLast - StepTimeFirst))
                {
                    if (Pos == 0)
                    {
                        if (RelaxOnOff == true) RelaxOnOff = false;
                    }
                    else
                    {
                        if (RelaxReturnOnOff == true) RelaxReturnOnOff = false;
                    }
                }

                StepTimeLast = ComF.timeGetTimems();
                AdData = pMeter.GetPSeat;

                if (0.5 < AdData)
                {
                    if(RelaxRunTimeFlag == false)
                    {
                        RelaxRunTimeFlag = true;
                        RelaxRunTime = ComF.timeGetTimems();
                    }
                    RelaxRunTime = ComF.timeGetTimems() - RelaxRunTime;
                    RunTime = RelaxRunTime;
                    if (RunTime != RunTimeOld)
                    {
                        float sData = CurrFilter.CheckData(0, (float)Math.Abs(Sound.GetSound - NormalSound));

                        if (mSpec.Sound.RMSMode == true)
                            plot2.Channels[0].AddXY((float)RunTime, sData);
                        else plot2.Channels[0].AddXY((float)RunTime, (float)Math.Abs(Sound.GetSound - NormalSound));
                        RunTimeOld = RunTime;
                        plot2.XAxes[0].Tracking.ZoomToFitAll();
                        plot2.YAxes[0].Tracking.ZoomToFitAll();
                    }
                }
                plot1.Channels[0].AddXY(plot1.Channels[0].Count, AdData);
                plot1.YAxes[0].Tracking.ZoomToFitAll();
                plot1.XAxes[0].Tracking.ZoomToFitAll();
                
                if ((mSpec.RelaxTestTime * 1000) <= (StepTimeLast - StepTimeFirst))
                {
                    //Slide Fwd Sw Off
                    fpSpread1.ActiveSheet.Cells[3, 6].Text = AdData.ToString("0.0");

                    float CheckData;

                    if (float.TryParse(fpSpread1.ActiveSheet.Cells[3, 6].Text, out CheckData) == false) CheckData = 0;

                    if (RelaxOnOff == true) RelaxOnOff = false;
                    if (RelaxReturnOnOff == true) RelaxReturnOnOff = false;

                    if(Pos == 0)
                        mData.Relax.Data1 = CheckData;
                    else mData.Relax.Data2 = CheckData;
                    mData.Relax.Test = true;

                    if (Pos == 0)
                    {
                        if ((mSpec.Current.Relax.Min <= CheckData) && (CheckData <= mSpec.Current.Relax.Max))
                            mData.Relax.Result1 = RESULT.PASS;
                        else mData.Relax.Result1 = RESULT.REJECT;

                        if (mData.Relax.Result1 == RESULT.REJECT) mData.Result = RESULT.REJECT;
                    }
                    else
                    {
                        if ((mSpec.Current.Relax.Min <= CheckData) && (CheckData <= mSpec.Current.Relax.Max))
                            mData.Relax.Result2 = RESULT.PASS;
                        else mData.Relax.Result2 = RESULT.REJECT;
                        if (mData.Relax.Result2 == RESULT.REJECT) mData.Result = RESULT.REJECT;
                    }

                    if (Pos == 0)
                    {
                        fpSpread1.ActiveSheet.Cells[3, 7].Text = mData.Relax.Result1 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[3, 7].ForeColor = mData.Relax.Result1 == RESULT.PASS ? Color.Green : Color.Red;
                        SoundLavelCheck(2);
                    }
                    else
                    {
                        fpSpread1.ActiveSheet.Cells[3, 7].Text = mData.Relax.Result1 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[3, 7].ForeColor = mData.Relax.Result1 == RESULT.PASS ? Color.Green : Color.Red;
                        SoundLavelCheck(3);
                    }
                    Step++;
                    SpecOutputFlag = false;
                    ComF.timedelay(500);
                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[3, 6].Text = AdData.ToString("0.0");
                }
            }
            return;
        }

        private void ReclineCheck(short Pos)
        {
            if (SpecOutputFlag == false)
            {
                label5.Text = "RECLINE 전류 측정 중 입니다.";
                StepTimeFirst = ComF.timeGetTimems();
                StepTimeLast = ComF.timeGetTimems();
                SpecOutputFlag = true;

                //Recline 
                if(Pos == 0)
                    ReclineBwdOnOff = true;
                else ReclineFwdOnOff = true;

                RunTime = 0;
                RunTimeOld = 0;
                plot2.Channels[0].Clear();
                CurrFilter.InitAll();
            }
            else
            {
                float AdData;

                StepTimeLast = ComF.timeGetTimems();
                AdData = pMeter.GetPSeat;

                RunTime = (StepTimeLast - StepTimeFirst);

                if (RunTime != RunTimeOld)
                {
                    float sData = CurrFilter.CheckData(0, (float)Math.Abs(Sound.GetSound - NormalSound));

                    if(mSpec.Sound.RMSMode == true) 
                        plot2.Channels[0].AddXY((float)RunTime, sData);
                    else plot2.Channels[0].AddXY((float)RunTime, (float)Math.Abs(Sound.GetSound - NormalSound));
                    plot2.XAxes[0].Tracking.ZoomToFitAll();
                    plot2.YAxes[0].Tracking.ZoomToFitAll();
                    RunTimeOld = RunTime;
                }
                plot1.Channels[0].AddXY(plot1.Channels[0].Count, AdData);
                plot1.YAxes[0].Tracking.ZoomToFitAll();
                plot1.XAxes[0].Tracking.ZoomToFitAll();


                if ((mSpec.ReclinerTestTime * 1000) <= (StepTimeLast - StepTimeFirst))
                {
                    //Recline Fwd Sw Off
                    fpSpread1.ActiveSheet.Cells[2, 6].Text = AdData.ToString("0.0");

                    float CheckData;

                    if (float.TryParse(fpSpread1.ActiveSheet.Cells[2, 6].Text, out CheckData) == false) CheckData = 0;

                    if (Pos == 0)
                        ReclineBwdOnOff = false;
                    else ReclineFwdOnOff = false;

                    if (Pos == 0)
                    {
                        mData.Recline.Data1 = CheckData;
                        if ((mSpec.Current.Recliner.Min <= CheckData) && (CheckData <= mSpec.Current.Recliner.Max))
                            mData.Recline.Result1 = RESULT.PASS;
                        else mData.Recline.Result1 = RESULT.REJECT;
                        if (mData.Recline.Result1 == RESULT.REJECT) mData.Result = RESULT.REJECT;
                        fpSpread1.ActiveSheet.Cells[2, 7].Text = mData.Recline.Result1 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[2, 7].ForeColor = mData.Recline.Result1 == RESULT.PASS ? Color.Green : Color.Red;
                        SoundLavelCheck(0);
                    }
                    else
                    {
                        mData.Recline.Data2 = CheckData;
                        if ((mSpec.Current.Recliner.Min <= CheckData) && (CheckData <= mSpec.Current.Recliner.Max))
                            mData.Recline.Result2 = RESULT.PASS;
                        else mData.Recline.Result2 = RESULT.REJECT;
                        if (mData.Recline.Result2 == RESULT.REJECT) mData.Result = RESULT.REJECT;
                        fpSpread1.ActiveSheet.Cells[2, 7].Text = mData.Recline.Result2 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[2, 7].ForeColor = mData.Recline.Result2 == RESULT.PASS ? Color.Green : Color.Red;
                        SoundLavelCheck(1);
                    }
                        
                    mData.Recline.Test = true;
                    
                    
                    Step++;
                    SpecOutputFlag = false;
                    ComF.timedelay(500);
                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[2, 6].Text = AdData.ToString("0.0");
                }
            }
            return;
        }

        private void LegrestCheck(short Pos)
        {
            if (SpecOutputFlag == false)
            {
                if (Pos == 0)
                    label5.Text = "LEGREST 전류 측정 중 입니다.";
                else label5.Text = "LEGREST RETURN 전류 측정 중 입니다.";
                StepTimeFirst = ComF.timeGetTimems();
                StepTimeLast = ComF.timeGetTimems();
                SpecOutputFlag = true;

                if(Pos == 0)
                    LegrestOnOff = true;
                else LegrestReturnOnOff = true;

                plot2.Channels[0].Clear();
                RunTimeOld = 0;
                RunTime = 0;
                CurrFilter.InitAll();
            }
            else
            {
                float AdData;

                StepTimeLast = ComF.timeGetTimems();

                RunTime = (StepTimeLast - StepTimeFirst);
                AdData = pMeter.GetPSeat;

                if (RunTime != RunTimeOld)
                {
                    RunTimeOld = RunTime;
                    float sData = CurrFilter.CheckData(0, (float)Math.Abs(Sound.GetSound - NormalSound));

                    if (mSpec.Sound.RMSMode == true)
                        plot2.Channels[0].AddXY((float)RunTime, sData);
                    else plot2.Channels[0].AddXY((float)RunTime, (float)Math.Abs(Sound.GetSound - NormalSound));
                    plot2.XAxes[0].Tracking.ZoomToFitAll();
                    plot2.YAxes[0].Tracking.ZoomToFitAll();
                }
                plot1.Channels[0].AddXY(plot1.Channels[0].Count, AdData);
                plot1.YAxes[0].Tracking.ZoomToFitAll();
                plot1.XAxes[0].Tracking.ZoomToFitAll();

                if ((mSpec.LegrestTestTime * 1000) <= (StepTimeLast - StepTimeFirst))
                {
                    //Slide Fwd Sw Off
                    fpSpread1.ActiveSheet.Cells[5, 6].Text = AdData.ToString("0.0");

                    float CheckData;

                    if (float.TryParse(fpSpread1.ActiveSheet.Cells[5, 6].Text, out CheckData) == false) CheckData = 0;

                    if (Pos == 0)
                        LegrestOnOff = false;
                    else LegrestReturnOnOff = false;

                    mData.Legrest.Test = true;

                    if (Pos == 0)
                    {
                        mData.Legrest.Data1 = CheckData;
                        
                        if ((mSpec.Current.Legrest.Min <= CheckData) && (CheckData <= mSpec.Current.Legrest.Max))
                            mData.Legrest.Result1 = RESULT.PASS;
                        else mData.Legrest.Result1 = RESULT.REJECT;

                        if (mData.Legrest.Result1 == RESULT.REJECT) mData.Result = RESULT.REJECT;
                        fpSpread1.ActiveSheet.Cells[5, 7].Text = mData.Legrest.Result1 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[5, 7].ForeColor = mData.Legrest.Result1 == RESULT.PASS ? Color.Green : Color.Red;
                        SoundLavelCheck(6);
                    }
                    else
                    {
                        mData.Legrest.Data2 = CheckData;

                        if ((mSpec.Current.Legrest.Min <= CheckData) && (CheckData <= mSpec.Current.Legrest.Max))
                            mData.Legrest.Result2 = RESULT.PASS;
                        else mData.Legrest.Result2 = RESULT.REJECT;

                        if (mData.Legrest.Result2 == RESULT.REJECT) mData.Result = RESULT.REJECT;
                        fpSpread1.ActiveSheet.Cells[5, 7].Text = mData.Legrest.Result2 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[5, 7].ForeColor = mData.Legrest.Result2 == RESULT.PASS ? Color.Green : Color.Red;
                        SoundLavelCheck(7);
                    }
                        
                        
                    Step++;
                    SpecOutputFlag = false;
                    ComF.timedelay(500);
                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[5, 6].Text = AdData.ToString("0.0");
                }
            }
            return;
        }

        private void HeightCheck(short Pos)
        {
            if (SpecOutputFlag == false)
            {
                label5.Text = "HEIGHT 전류 측정 중 입니다.";
                StepTimeFirst = ComF.timeGetTimems();
                StepTimeLast = ComF.timeGetTimems();
                SpecOutputFlag = true;

                if(Pos == 0)
                    HeightDnOnOff = true;
                else HeightUpOnOff = true;

                plot2.Channels[0].Clear();
                RunTimeOld = 0;
                RunTime = 0;
                CurrFilter.InitAll();
            }
            else
            {
                float AdData;

                StepTimeLast = ComF.timeGetTimems();

                RunTime = (StepTimeLast - StepTimeFirst);

                AdData = pMeter.GetPSeat;

                if (RunTime != RunTimeOld)
                {
                    RunTimeOld = RunTime;
                    float sData = CurrFilter.CheckData(0, (float)Math.Abs(Sound.GetSound - NormalSound));

                    if (mSpec.Sound.RMSMode == true)
                        plot2.Channels[0].AddXY((float)RunTime, sData);
                    else plot2.Channels[0].AddXY((float)RunTime, (float)Math.Abs(Sound.GetSound - NormalSound));
                    plot2.XAxes[0].Tracking.ZoomToFitAll();
                    plot2.YAxes[0].Tracking.ZoomToFitAll();
                }
                plot1.Channels[0].AddXY(plot1.Channels[0].Count, AdData);
                plot1.YAxes[0].Tracking.ZoomToFitAll();
                plot1.XAxes[0].Tracking.ZoomToFitAll();
                

                if ((mSpec.HeightTestTime * 1000) <= (StepTimeLast - StepTimeFirst))
                {
                    //Height
                    fpSpread1.ActiveSheet.Cells[4, 6].Text = AdData.ToString("0.0");

                    float CheckData;

                    if (float.TryParse(fpSpread1.ActiveSheet.Cells[4, 6].Text, out CheckData) == false) CheckData = 0;

                    if(Pos == 0)
                        HeightDnOnOff = false;
                    else HeightUpOnOff = false;

                    mData.Height.Test = true;

                    if (Pos == 0)
                    {
                        mData.Height.Data1 = CheckData;
                        
                        if ((mSpec.Current.Height.Min <= CheckData) && (CheckData <= mSpec.Current.Height.Max))
                            mData.Height.Result1 = RESULT.PASS;
                        else mData.Height.Result1 = RESULT.REJECT;

                        if (mData.Height.Result1 == RESULT.REJECT) mData.Result = RESULT.REJECT;

                        fpSpread1.ActiveSheet.Cells[4, 7].Text = mData.Height.Result1 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[4, 7].ForeColor = mData.Height.Result1 == RESULT.PASS ? Color.Green : Color.Red;

                        SoundLavelCheck(4);
                    }
                    else
                    {
                        mData.Height.Data2 = CheckData;
                        if ((mSpec.Current.Height.Min <= CheckData) && (CheckData <= mSpec.Current.Height.Max))
                            mData.Height.Result2 = RESULT.PASS;
                        else mData.Height.Result2 = RESULT.REJECT;

                        if (mData.Height.Result2 == RESULT.REJECT) mData.Result = RESULT.REJECT;

                        fpSpread1.ActiveSheet.Cells[4, 7].Text = mData.Height.Result2 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[4, 7].ForeColor = mData.Height.Result2 == RESULT.PASS ? Color.Green : Color.Red;

                        SoundLavelCheck(5);
                    }
                    Step++;
                    SpecOutputFlag = false;
                    ComF.timedelay(500);
                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[4, 6].Text = AdData.ToString("0.0");
                }
            }
            return;
        }

        private void LegrestExxtCheck(short Pos)
        {
            if (SpecOutputFlag == false)
            {
                label5.Text = "LEGREST EXT 전류 측정 중 입니다.";
                StepTimeFirst = ComF.timeGetTimems();
                StepTimeLast = ComF.timeGetTimems();
                SpecOutputFlag = true;

                if (Pos == 0)
                    LegrestExtOnOff = true;
                else LegrestExtReturnOnOff = true;

                plot2.Channels[0].Clear();
                RunTimeOld = 0;
                RunTime = 0;
                CurrFilter.InitAll();
            }
            else
            {
                float AdData;

                StepTimeLast = ComF.timeGetTimems();
                AdData = pMeter.GetPSeat;

                RunTime = (StepTimeLast - StepTimeFirst);
                if (RunTime != RunTimeOld)
                {
                    RunTimeOld = RunTime;
                    float sData = CurrFilter.CheckData(0, (float)Math.Abs(Sound.GetSound - NormalSound));

                    if (mSpec.Sound.RMSMode == true)
                        plot2.Channels[0].AddXY((float)RunTime, sData);
                    else plot2.Channels[0].AddXY((float)RunTime, (float)Math.Abs(Sound.GetSound - NormalSound));
                    plot2.XAxes[0].Tracking.ZoomToFitAll();
                    plot2.YAxes[0].Tracking.ZoomToFitAll();
                }
                plot1.Channels[0].AddXY(plot1.Channels[0].Count, AdData);
                plot1.YAxes[0].Tracking.ZoomToFitAll();
                plot1.XAxes[0].Tracking.ZoomToFitAll();
                

                if ((mSpec.LegrestExtTestTime * 1000) <= (StepTimeLast - StepTimeFirst))
                {
                    //Slide Fwd Sw Off
                    fpSpread1.ActiveSheet.Cells[6, 6].Text = AdData.ToString("0.0");

                    float CheckData;

                    if (float.TryParse(fpSpread1.ActiveSheet.Cells[6, 6].Text, out CheckData) == false) CheckData = 0;

                    if (Pos == 0)
                        LegrestExtOnOff = false;
                    else LegrestExtReturnOnOff = false;

                    if(Pos ==0)
                    {
                        mData.LegrestExt.Data1 = CheckData;
                        if ((mSpec.Current.LegrestExt.Min <= CheckData) && (CheckData <= mSpec.Current.LegrestExt.Max))
                            mData.LegrestExt.Result1 = RESULT.PASS;
                        else mData.LegrestExt.Result1 = RESULT.REJECT;
                        if (mData.LegrestExt.Result1 == RESULT.REJECT) mData.Result = RESULT.REJECT;
                        fpSpread1.ActiveSheet.Cells[6, 7].Text = mData.LegrestExt.Result1 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[6, 7].ForeColor = mData.LegrestExt.Result1 == RESULT.PASS ? Color.Green : Color.Red;
                        SoundLavelCheck(8);
                    }
                    else
                    {
                        mData.LegrestExt.Data2 = CheckData;
                        if ((mSpec.Current.LegrestExt.Min <= CheckData) && (CheckData <= mSpec.Current.LegrestExt.Max))
                            mData.LegrestExt.Result2 = RESULT.PASS;
                        else mData.LegrestExt.Result2 = RESULT.REJECT;
                        if (mData.LegrestExt.Result2 == RESULT.REJECT) mData.Result = RESULT.REJECT;
                        fpSpread1.ActiveSheet.Cells[6, 7].Text = mData.LegrestExt.Result2 == RESULT.PASS ? "O.K" : "N.G";
                        fpSpread1.ActiveSheet.Cells[6, 7].ForeColor = mData.LegrestExt.Result2 == RESULT.PASS ? Color.Green : Color.Red;
                        SoundLavelCheck(9);
                    }
                    mData.LegrestExt.Test = true;
                                       
                    
                    Step++;
                    SpecOutputFlag = false;
                    ComF.timedelay(500);
                }
                else
                {
                    fpSpread1.ActiveSheet.Cells[6, 6].Text = AdData.ToString("0.0");
                }
            }
            return;
        }
        

        private void SoundLavelCheck(short Pos)
        {
            long StartTime = 0;
            long xRunTime = 0;
            float Time = 0;
            float Sound = 0;
            double StartSound = 0;
            int StartCount = 0;
            double RunSound = 0;
            int RunCount = 0;

            StartTime = (long)(mSpec.Sound.StartTime * 1000F);

            switch(Pos)
            {
                case 0:
                case 1:
                    xRunTime = (long)(mSpec.ReclinerSoundCheckTime * 1000F);
                    break;
                case 2:
                case 3:
                    xRunTime = (long)(mSpec.RelaxSoundCheckTime * 1000F);
                    break;
                case 4:
                case 5:
                    xRunTime = (long)(mSpec.HeightSoundCheckTime * 1000F);
                    break;
                case 6:
                case 7:
                    xRunTime = (long)(mSpec.LegrestSoundCheckTime * 1000F);
                    break;
                default:
                    xRunTime = (long)(mSpec.LegrestExtSoundCheckTime * 1000F);
                    break;
            }

            for (int i = 0;i < plot2.Channels[0].Count;i++)
            {
                Time = (float)plot2.Channels[0].GetX(i);
                Sound = (float)plot2.Channels[0].GetY(i);

                if(Time < StartTime)
                {
                    if (StartCount == 0)
                    {
                        StartSound = Sound;
                    }
                    else
                    {
                        //if (mSpec.Sound.RMSMode == true)
                        //    StartSound += Sound;
                        //else StartSound = Math.Max(StartSound, Sound);
                        StartSound = Math.Max(StartSound, Sound);
                    }
                    StartCount++;
                }
                else if (Time <= xRunTime)
                {
                    if (RunCount == 0)
                    {
                        RunSound = Sound;
                    }
                    else
                    {
                        //if (mSpec.Sound.RMSMode == true)
                        //    RunSound += Sound;
                        //else RunSound = Math.Max(RunSound, Sound);
                        RunSound = Math.Max(RunSound, Sound);
                    }
                    RunCount++;
                }
            }
            //if (mSpec.Sound.RMSMode == true) RunSound = RunSound / (float)RunCount;
            //if (mSpec.Sound.RMSMode == true) StartSound = StartSound / (float)StartCount;

            switch (Pos)
            {
                case 0:
                    mData.ReclineBwdSound.StartData = (float)StartSound;
                    mData.ReclineBwdSound.RunData = (float)RunSound;
                    mData.ReclineBwdSound.Test = true;
                    break;
                case 1:
                    mData.ReclineFwdSound.StartData = (float)StartSound;
                    mData.ReclineFwdSound.RunData = (float)RunSound;
                    mData.ReclineFwdSound.Test = true;
                    break;
                case 2:
                    mData.RelaxSound.StartData = (float)StartSound;
                    mData.RelaxSound.RunData = (float)RunSound;
                    mData.RelaxSound.Test = true;
                    break;
                case 3:
                    mData.RelaxReturnSound.StartData = (float)StartSound;
                    mData.RelaxReturnSound.RunData = (float)RunSound;
                    mData.RelaxReturnSound.Test = true;
                    break;
                case 4:
                    mData.HeightUpSound.StartData = (float)StartSound;
                    mData.HeightUpSound.RunData = (float)RunSound;
                    mData.HeightUpSound.Test = true;
                    break;
                case 5:
                    mData.HeightDnSound.StartData = (float)StartSound;
                    mData.HeightDnSound.RunData = (float)RunSound;
                    mData.HeightDnSound.Test = true;
                    break;
                case 6:
                    mData.LegrestSound.StartData = (float)StartSound;
                    mData.LegrestSound.RunData = (float)RunSound;
                    mData.LegrestSound.Test = true;
                    break;
                case 7:
                    mData.LegrestReturnSound.StartData = (float)StartSound;
                    mData.LegrestReturnSound.RunData = (float)RunSound;
                    mData.LegrestReturnSound.Test = true;
                    break;
                case 8:
                    mData.LegrestExtSound.StartData = (float)StartSound;
                    mData.LegrestExtSound.RunData = (float)RunSound;
                    mData.LegrestExtSound.Test = true;
                    break;
                default:
                    mData.LegrestExtReturnSound.StartData = (float)StartSound;
                    mData.LegrestExtReturnSound.RunData = (float)RunSound;
                    mData.LegrestExtReturnSound.Test = true;
                    break;
            }
            return;
        }

        //private bool SlideMotorMoveEndFlag { get; set; }
        //private bool ReclineMotorMoveEndFlag { get; set; }
        //private bool TiltMotorMoveEndFlag { get; set; }
        //private bool HeightMotorMoveEndFlag { get; set; }
        //void SoundCheckInitPositionMove()
        //{
        //    if (SpecOutputFlag == false)
        //    {
        //        SlideFwdOnOff = true;
        //        ReclineFwdOnOff = true;
        //        TiltUpOnOff = true;
        //        HeightUpOnOff = true;
        //        StepTimeFirst = ComF.timeGetTimems();
        //        StepTimeLast = ComF.timeGetTimems();
        //        SpecOutputFlag = true;
        //        SlideMotorMoveEndFlag = false;
        //        ReclineMotorMoveEndFlag = false;
        //        TiltMotorMoveEndFlag = false;
        //        HeightMotorMoveEndFlag = false;
        //        label5.Text = "속도 측정을 하기 위해 측정 위치로 이동 중 입니다.";
        //    }
        //    else
        //    {
        //        if(30 < pMeter.GetPSeat)
        //        {
        //            SlideFwdOnOff = false;
        //            ReclineFwdOnOff = false;
        //            TiltUpOnOff = false;
        //            HeightUpOnOff = false;
        //            SpecOutputFlag = false;
        //            SlideMotorMoveEndFlag = true;
        //            ReclineMotorMoveEndFlag = true;
        //            TiltMotorMoveEndFlag = true;
        //            HeightMotorMoveEndFlag = true;
        //        }

        //        StepTimeLast = ComF.timeGetTimems();

        //        if (2000 <= (StepTimeLast - StepTimeFirst))
        //        {
        //            if (SlideMotorMoveEndFlag == false)
        //            {
        //                float AdData;

        //                bool Flag = false;
        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                    AdData = pMeter.GetPSeat;
        //                else AdData = IOPort.ADRead[mSpec.PinMap.SlideFWD.PinNo];
        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                {
        //                    if (AdData < mSpec.SlideLimitCurr) Flag = true;
        //                }
        //                else
        //                {
        //                    if (mSpec.SlideLimitCurr < AdData) Flag = true;
        //                }
        //                if (Flag == true)
        //                {
        //                    SlideFwdOnOff = false;
        //                    SlideMotorMoveEndFlag = true;
        //                }
        //                else if ((mSpec.SlideLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                {
        //                    SlideFwdOnOff = false;
        //                    SlideMotorMoveEndFlag = true;
        //                }
        //            }
        //            if (ReclineMotorMoveEndFlag == false)
        //            {
        //                float AdData;

        //                bool Flag = false;
        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                    AdData = pMeter.GetPSeat;
        //                else AdData = IOPort.ADRead[mSpec.PinMap.SlideFWD.PinNo];
        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                {
        //                    if (AdData < mSpec.ReclinerLimitCurr) Flag = true;
        //                }
        //                else
        //                {
        //                    if (mSpec.ReclinerLimitCurr < AdData) Flag = true;
        //                }
        //                if (Flag == true)
        //                {
        //                    ReclineFwdOnOff = false;
        //                    ReclineMotorMoveEndFlag = true;
        //                }
        //                else if ((mSpec.ReclinerLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                {
        //                    ReclineFwdOnOff = false;
        //                    ReclineMotorMoveEndFlag = true;
        //                }
        //            }
        //            if (TiltMotorMoveEndFlag == false)
        //            {
        //                float AdData;
        //                bool Flag = false;

        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                    AdData = pMeter.GetPSeat;
        //                else AdData = IOPort.ADRead[mSpec.PinMap.TiltUp.PinNo];

        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                {
        //                    if (AdData < mSpec.TiltLimitCurr) Flag = true;
        //                }
        //                else
        //                {
        //                    if (mSpec.TiltLimitCurr < AdData) Flag = true;
        //                }
        //                if (Flag == true)
        //                {
        //                    TiltUpOnOff = false;
        //                    TiltMotorMoveEndFlag = true;
        //                }
        //                else if ((mSpec.TiltLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                {
        //                    TiltUpOnOff = false;
        //                    TiltMotorMoveEndFlag = true;
        //                }
        //            }
        //            if (HeightMotorMoveEndFlag == false)
        //            {
        //                float AdData;
        //                bool Flag = false;

        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                    AdData = pMeter.GetPSeat;
        //                else AdData = IOPort.ADRead[mSpec.PinMap.HeightUp.PinNo];
        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                {
        //                    if (AdData < mSpec.HeightLimitCurr) Flag = true;
        //                }
        //                else
        //                {
        //                    if (mSpec.HeightLimitCurr < AdData) Flag = true;
        //                }
        //                if (Flag == true)
        //                {
        //                    HeightUpOnOff = false;
        //                    HeightMotorMoveEndFlag = true;
        //                }
        //                else if ((mSpec.HeightLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                {
        //                    HeightUpOnOff = false;
        //                    HeightMotorMoveEndFlag = true;
        //                }
        //            }
        //        }

        //        if ((SlideMotorMoveEndFlag == true) && (TiltMotorMoveEndFlag == true) && (HeightMotorMoveEndFlag == true))
        //        {
        //            SpecOutputFlag = false;
        //            Step++;
        //            ComF.timedelay(500);
        //        }
        //    }
        //    return;
        //}
        //private bool[] SpeedCheckEnd = { false, false, false, false, false, false, false, false };
        //private void CheckSpeedToIMS(short SpeedCheckStep)
        //{
        //    if (SpecOutputFlag == false)
        //    {
        //        StepTimeFirst = ComF.timeGetTimems();
        //        StepTimeLast = ComF.timeGetTimems();
        //        SpecOutputFlag = true;

        //        //Slide Fwd Sw On

        //        switch (SpeedCheckStep)
        //        {
        //            case 0:
        //                //FirstPosCheckEnd[SpeedCheckStep] = false;
        //                plot2.Channels[0].Clear();
        //                SoundCheckTimeToStart[SpeedCheckStep] = plot2.Channels[0].Count;
        //                SlideBwdOnOff = true;
        //                label5.Text = "SLIDE BACKWORD 속도 측정 중 입니다.";
        //                break;
        //            case 1:
        //                //FirstPosCheckEnd[SpeedCheckStep] = false;
        //                plot2.Channels[0].Clear();
        //                SoundCheckTimeToStart[SpeedCheckStep] = plot2.Channels[0].Count;
        //                SlideFwdOnOff = true;
        //                label5.Text = "SLIDE FORWORD 속도 측정 중 입니다.";
        //                break;
        //            case 2:
        //                //FirstPosCheckEnd[SpeedCheckStep] = false;
        //                plot2.Channels[0].Clear();
        //                SoundCheckTimeToStart[SpeedCheckStep] = plot2.Channels[0].Count;
        //                ReclineBwdOnOff = true;
        //                label5.Text = "RECLINE BACKWORD 속도 측정 중 입니다.";
        //                break;
        //            case 3:
        //                //FirstPosCheckEnd[SpeedCheckStep] = false;
        //                plot2.Channels[0].Clear();
        //                SoundCheckTimeToStart[SpeedCheckStep] = plot2.Channels[0].Count;
        //                ReclineFwdOnOff = true;
        //                label5.Text = "RECLINE FORWORD 속도 측정 중 입니다.";
        //                break;
        //            case 4:
        //                //FirstPosCheckEnd[SpeedCheckStep] = false;
        //                plot2.Channels[0].Clear();
        //                SoundCheckTimeToStart[SpeedCheckStep] = plot2.Channels[0].Count;
        //                TiltDnOnOff = true;
        //                label5.Text = "TULT UP 속도 측정 중 입니다.";
        //                break;
        //            case 5:
        //                //FirstPosCheckEnd[SpeedCheckStep] = false;
        //                plot2.Channels[0].Clear();
        //                SoundCheckTimeToStart[SpeedCheckStep] = plot2.Channels[0].Count;
        //                TiltUpOnOff = true;
        //                label5.Text = "TULT DOWN 속도 측정 중 입니다.";
        //                break;
        //            case 6:
        //                //FirstPosCheckEnd[SpeedCheckStep] = false;
        //                plot2.Channels[0].Clear();
        //                SoundCheckTimeToStart[SpeedCheckStep] = plot2.Channels[0].Count;
        //                HeightDnOnOff = true;
        //                label5.Text = "HEIGHT UP 속도 측정 중 입니다.";
        //                break;
        //            case 7:
        //                //FirstPosCheckEnd[SpeedCheckStep] = false;
        //                plot2.Channels[0].Clear();
        //                SoundCheckTimeToStart[SpeedCheckStep] = plot2.Channels[0].Count;
        //                HeightUpOnOff = true;
        //                label5.Text = "HEIGHT DOWN 속도 측정 중 입니다.";
        //                break;
        //        }
        //        SpeedCheckStartTime[SpeedCheckStep] = ComF.timeGetTimems();
        //    }
        //    else
        //    {
        //        StepTimeLast = ComF.timeGetTimems();

        //        float AdData = pMeter.GetPSeat;

        //        if (500 <= (StepTimeLast - StepTimeFirst))
        //        {
        //            plot2.Channels[0].AddXY(plot2.Channels[0].Count, Math.Abs(Sound.GetSound - NormalSound));
        //            plot2.XAxes[0].Tracking.ZoomToFitAll();
        //            plot2.YAxes[0].Tracking.ZoomToFitAll();
        //            if (SpeedCheckStep == 0)
        //            {
        //                if (/*(FirstPosCheckEnd[SpeedCheckStep] == false) && (*/AdData < mSpec.SlideLimitCurr)//)
        //                {
        //                    SpeedCheckEnd[SpeedCheckStep] = true;
        //                    SlideBwdOnOff = false;
        //                    SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;

        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                    mData.SlideBwdTime.Data = (mSpec.MovingStroke.Slide / EndTime) * 1F;
        //                    mData.SlideBwdTime.Test = true;
        //                    if ((mSpec.MovingSpeed.SlideBwd.Min <= mData.SlideBwdTime.Data) && (mData.SlideBwdTime.Data <= mSpec.MovingSpeed.SlideBwd.Max))
        //                        mData.SlideBwdTime.Result = RESULT.PASS;
        //                    else mData.SlideBwdTime.Result = RESULT.REJECT;

        //                    fpSpread1.ActiveSheet.Cells[4, 6].Text = mData.SlideBwdTime.Data.ToString("0.00");
        //                    fpSpread1.ActiveSheet.Cells[4, 7].Text = mData.SlideBwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                    fpSpread1.ActiveSheet.Cells[4, 7].ForeColor = mData.SlideBwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                }
        //                else
        //                {
        //                    //if (FirstPosCheckEnd[SpeedCheckStep] == false)
        //                    //{
        //                    if ((mSpec.SlideLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                    {
        //                        SlideBwdOnOff = false;
        //                        float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                        mData.SlideBwdTime.Data = (mSpec.MovingStroke.Slide / EndTime) * 1F;
        //                        mData.SlideBwdTime.Test = true;
        //                        if ((mSpec.MovingSpeed.SlideBwd.Min <= mData.SlideBwdTime.Data) && (mData.SlideBwdTime.Data <= mSpec.MovingSpeed.SlideBwd.Max))
        //                            mData.SlideBwdTime.Result = RESULT.PASS;
        //                        else mData.SlideBwdTime.Result = RESULT.REJECT;

        //                        fpSpread1.ActiveSheet.Cells[4, 6].Text = mData.SlideBwdTime.Data.ToString("0.00");
        //                        fpSpread1.ActiveSheet.Cells[4, 7].Text = mData.SlideBwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.ActiveSheet.Cells[4, 7].ForeColor = mData.SlideBwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    //}
        //                }
        //            }
        //            else if (SpeedCheckStep == 1)
        //            {
        //                if (/*(FirstPosCheckEnd[SpeedCheckStep] == false) && (*/AdData < mSpec.SlideLimitCurr)//)
        //                {
        //                    SpeedCheckEnd[SpeedCheckStep] = true;
        //                    SlideFwdOnOff = false;
        //                    SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;

        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                    mData.SlideFwdTime.Data = (mSpec.MovingStroke.Slide / EndTime) * 1F;
        //                    mData.SlideFwdTime.Test = true;
        //                    if ((mSpec.MovingSpeed.SlideFwd.Min <= mData.SlideFwdTime.Data) && (mData.SlideFwdTime.Data <= mSpec.MovingSpeed.SlideFwd.Max))
        //                        mData.SlideFwdTime.Result = RESULT.PASS;
        //                    else mData.SlideFwdTime.Result = RESULT.REJECT;

        //                    fpSpread1.ActiveSheet.Cells[3, 6].Text = mData.SlideFwdTime.Data.ToString("0.00");
        //                    fpSpread1.ActiveSheet.Cells[3, 7].Text = mData.SlideFwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                    fpSpread1.ActiveSheet.Cells[3, 7].ForeColor = mData.SlideFwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                }
        //                else
        //                {
        //                    //if (FirstPosCheckEnd[SpeedCheckStep] == false)
        //                    //{
        //                    if ((mSpec.SlideLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                    {
        //                        SpeedCheckEnd[SpeedCheckStep] = true;
        //                        SlideFwdOnOff = false;
        //                        SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;

        //                        float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                        mData.SlideFwdTime.Data = (mSpec.MovingStroke.Slide / EndTime) * 1F;
        //                        mData.SlideFwdTime.Test = true;
        //                        if ((mSpec.MovingSpeed.SlideFwd.Min <= mData.SlideFwdTime.Data) && (mData.SlideFwdTime.Data <= mSpec.MovingSpeed.SlideFwd.Max))
        //                            mData.SlideFwdTime.Result = RESULT.PASS;
        //                        else mData.SlideFwdTime.Result = RESULT.REJECT;

        //                        fpSpread1.ActiveSheet.Cells[3, 6].Text = mData.SlideFwdTime.Data.ToString("0.00");
        //                        fpSpread1.ActiveSheet.Cells[3, 7].Text = mData.SlideFwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.ActiveSheet.Cells[3, 7].ForeColor = mData.SlideFwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    //}
        //                }
        //            }
        //            else if (SpeedCheckStep == 2)
        //            {
        //                if (/*(FirstPosCheckEnd[SpeedCheckStep] == false) && (*/AdData < mSpec.SlideLimitCurr)//)
        //                {
        //                    SpeedCheckEnd[SpeedCheckStep] = true;
        //                    ReclineBwdOnOff = false;
        //                    SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;

        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                    mData.ReclineBwdTime.Data = (mSpec.MovingStroke.Recliner / EndTime) * 1F;
        //                    mData.ReclineBwdTime.Test = true;
        //                    if ((mSpec.MovingSpeed.ReclinerBwd.Min <= mData.ReclineBwdTime.Data) && (mData.ReclineBwdTime.Data <= mSpec.MovingSpeed.ReclinerBwd.Max))
        //                        mData.ReclineBwdTime.Result = RESULT.PASS;
        //                    else mData.ReclineBwdTime.Result = RESULT.REJECT;

        //                    fpSpread1.ActiveSheet.Cells[6, 6].Text = mData.ReclineBwdTime.Data.ToString("0.00");
        //                    fpSpread1.ActiveSheet.Cells[6, 7].Text = mData.ReclineBwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                    fpSpread1.ActiveSheet.Cells[6, 7].ForeColor = mData.ReclineBwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                }
        //                else
        //                {
        //                    //if (FirstPosCheckEnd[SpeedCheckStep] == false)
        //                    //{
        //                    if ((mSpec.ReclinerLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                    {
        //                        ReclineBwdOnOff = false;
        //                        float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                        mData.ReclineBwdTime.Data = (mSpec.MovingStroke.Recliner / EndTime) * 1F;
        //                        mData.ReclineBwdTime.Test = true;
        //                        if ((mSpec.MovingSpeed.ReclinerBwd.Min <= mData.ReclineBwdTime.Data) && (mData.ReclineBwdTime.Data <= mSpec.MovingSpeed.ReclinerBwd.Max))
        //                            mData.ReclineBwdTime.Result = RESULT.PASS;
        //                        else mData.ReclineBwdTime.Result = RESULT.REJECT;

        //                        fpSpread1.ActiveSheet.Cells[6, 6].Text = mData.ReclineBwdTime.Data.ToString("0.00");
        //                        fpSpread1.ActiveSheet.Cells[6, 7].Text = mData.ReclineBwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.ActiveSheet.Cells[6, 7].ForeColor = mData.ReclineBwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    //}
        //                }
        //            }
        //            else if (SpeedCheckStep == 3)
        //            {
        //                if (/*(FirstPosCheckEnd[SpeedCheckStep] == false) && (*/AdData < mSpec.ReclinerLimitCurr)//)
        //                {
        //                    SpeedCheckEnd[SpeedCheckStep] = true;
        //                    ReclineFwdOnOff = false;
        //                    SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;

        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                    mData.ReclineFwdTime.Data = (mSpec.MovingStroke.Recliner / EndTime) * 1F;
        //                    mData.ReclineFwdTime.Test = true;
        //                    if ((mSpec.MovingSpeed.ReclinerFwd.Min <= mData.ReclineFwdTime.Data) && (mData.ReclineFwdTime.Data <= mSpec.MovingSpeed.ReclinerFwd.Max))
        //                        mData.ReclineFwdTime.Result = RESULT.PASS;
        //                    else mData.ReclineFwdTime.Result = RESULT.REJECT;

        //                    fpSpread1.ActiveSheet.Cells[5, 6].Text = mData.ReclineFwdTime.Data.ToString("0.00");
        //                    fpSpread1.ActiveSheet.Cells[5, 7].Text = mData.ReclineFwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                    fpSpread1.ActiveSheet.Cells[5, 7].ForeColor = mData.ReclineFwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                }
        //                else
        //                {
        //                    //if (FirstPosCheckEnd[SpeedCheckStep] == false)
        //                    //{
        //                    if ((mSpec.ReclinerLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                    {
        //                        SpeedCheckEnd[SpeedCheckStep] = true;
        //                        ReclineFwdOnOff = false;
        //                        SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;

        //                        float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                        mData.ReclineFwdTime.Data = (mSpec.MovingStroke.Recliner / EndTime) * 1F;
        //                        mData.ReclineFwdTime.Test = true;
        //                        if ((mSpec.MovingSpeed.ReclinerFwd.Min <= mData.ReclineFwdTime.Data) && (mData.ReclineFwdTime.Data <= mSpec.MovingSpeed.ReclinerFwd.Max))
        //                            mData.ReclineFwdTime.Result = RESULT.PASS;
        //                        else mData.ReclineFwdTime.Result = RESULT.REJECT;

        //                        fpSpread1.ActiveSheet.Cells[5, 6].Text = mData.ReclineFwdTime.Data.ToString("0.00");
        //                        fpSpread1.ActiveSheet.Cells[5, 7].Text = mData.ReclineFwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.ActiveSheet.Cells[5, 7].ForeColor = mData.ReclineFwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    //}
        //                }
        //            }
        //            else if (SpeedCheckStep == 4)
        //            {
        //                if (/*(FirstPosCheckEnd[SpeedCheckStep] == false) && (*/AdData < mSpec.TiltLimitCurr)//)
        //                {
        //                    SpeedCheckEnd[SpeedCheckStep] = true;
        //                    TiltDnOnOff = false;
        //                    SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;
        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                    mData.TiltDnTime.Data = (mSpec.MovingStroke.Tilt / EndTime) * 1F;
        //                    mData.TiltDnTime.Test = true;
        //                    if ((mSpec.MovingSpeed.TiltDn.Min <= mData.TiltDnTime.Data) && (mData.TiltDnTime.Data <= mSpec.MovingSpeed.TiltDn.Max))
        //                        mData.TiltDnTime.Result = RESULT.PASS;
        //                    else mData.TiltDnTime.Result = RESULT.REJECT;

        //                    fpSpread1.ActiveSheet.Cells[8, 6].Text = mData.TiltDnTime.Data.ToString("0.00");
        //                    fpSpread1.ActiveSheet.Cells[8, 7].Text = mData.TiltDnTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                    fpSpread1.ActiveSheet.Cells[8, 7].ForeColor = mData.TiltDnTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                }
        //                else
        //                {
        //                    //if (FirstPosCheckEnd[SpeedCheckStep] == false)
        //                    //{
        //                    if ((mSpec.TiltLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                    {
        //                        SpeedCheckEnd[SpeedCheckStep] = true;
        //                        TiltDnOnOff = false;
        //                        SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;
        //                        float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                        mData.TiltDnTime.Data = (mSpec.MovingStroke.Tilt / EndTime) * 1F;
        //                        mData.TiltDnTime.Test = true;
        //                        if ((mSpec.MovingSpeed.TiltDn.Min <= mData.TiltDnTime.Data) && (mData.TiltDnTime.Data <= mSpec.MovingSpeed.TiltDn.Max))
        //                            mData.TiltDnTime.Result = RESULT.PASS;
        //                        else mData.TiltDnTime.Result = RESULT.REJECT;

        //                        fpSpread1.ActiveSheet.Cells[8, 6].Text = mData.TiltDnTime.Data.ToString("0.00");
        //                        fpSpread1.ActiveSheet.Cells[8, 7].Text = mData.TiltDnTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.ActiveSheet.Cells[8, 7].ForeColor = mData.TiltDnTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    //}
        //                }
        //            }
        //            else if (SpeedCheckStep == 5)
        //            {
        //                if (/*(FirstPosCheckEnd[SpeedCheckStep] == false) && (*/AdData < mSpec.TiltLimitCurr)//)
        //                {
        //                    SpeedCheckEnd[SpeedCheckStep] = true;
        //                    TiltUpOnOff = false;
        //                    SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;
        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                    mData.TiltUpTime.Data = (mSpec.MovingStroke.Tilt / EndTime) * 1F;
        //                    mData.TiltUpTime.Test = true;
        //                    if ((mSpec.MovingSpeed.TiltUp.Min <= mData.TiltUpTime.Data) && (mData.TiltUpTime.Data <= mSpec.MovingSpeed.TiltUp.Max))
        //                        mData.TiltUpTime.Result = RESULT.PASS;
        //                    else mData.TiltUpTime.Result = RESULT.REJECT;

        //                    fpSpread1.ActiveSheet.Cells[7, 6].Text = mData.TiltUpTime.Data.ToString("0.00");
        //                    fpSpread1.ActiveSheet.Cells[7, 7].Text = mData.TiltUpTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                    fpSpread1.ActiveSheet.Cells[7, 7].ForeColor = mData.TiltUpTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                }
        //                else
        //                {
        //                    //if (FirstPosCheckEnd[SpeedCheckStep] == false)
        //                    //{
        //                    if ((mSpec.TiltLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                    {
        //                        SpeedCheckEnd[SpeedCheckStep] = true;
        //                        TiltUpOnOff = false;
        //                        SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;
        //                        float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                        mData.TiltUpTime.Data = (mSpec.MovingStroke.Tilt / EndTime) * 1F;
        //                        mData.TiltUpTime.Test = true;
        //                        if ((mSpec.MovingSpeed.TiltUp.Min <= mData.TiltUpTime.Data) && (mData.TiltUpTime.Data <= mSpec.MovingSpeed.TiltUp.Max))
        //                            mData.TiltUpTime.Result = RESULT.PASS;
        //                        else mData.TiltUpTime.Result = RESULT.REJECT;

        //                        fpSpread1.ActiveSheet.Cells[7, 6].Text = mData.TiltUpTime.Data.ToString("0.00");
        //                        fpSpread1.ActiveSheet.Cells[7, 7].Text = mData.TiltUpTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.ActiveSheet.Cells[7, 7].ForeColor = mData.TiltUpTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    //}
        //                }
        //            }
        //            else if (SpeedCheckStep == 6)
        //            {
        //                if (/*(FirstPosCheckEnd[SpeedCheckStep] == false) && (*/AdData < mSpec.HeightLimitCurr)//)
        //                {
        //                    SpeedCheckEnd[SpeedCheckStep] = true;
        //                    HeightDnOnOff = false;
        //                    SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;
        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                    mData.HeightDnTime.Data = (mSpec.MovingStroke.Height / EndTime) * 1F;
        //                    mData.HeightDnTime.Test = true;
        //                    if ((mSpec.MovingSpeed.HeightDn.Min <= mData.HeightDnTime.Data) && (mData.HeightDnTime.Data <= mSpec.MovingSpeed.HeightDn.Max))
        //                        mData.HeightDnTime.Result = RESULT.PASS;
        //                    else mData.HeightDnTime.Result = RESULT.REJECT;

        //                    fpSpread1.ActiveSheet.Cells[10, 6].Text = mData.HeightDnTime.Data.ToString("0.00");
        //                    fpSpread1.ActiveSheet.Cells[10, 7].Text = mData.HeightDnTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                    fpSpread1.ActiveSheet.Cells[10, 7].ForeColor = mData.HeightDnTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                }
        //                else
        //                {
        //                    //if (FirstPosCheckEnd[SpeedCheckStep] == false)
        //                    //{
        //                    if ((mSpec.HeightLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                    {
        //                        SpeedCheckEnd[SpeedCheckStep] = true;
        //                        HeightDnOnOff = false;
        //                        SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;
        //                        float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                        mData.HeightDnTime.Data = (mSpec.MovingStroke.Height / EndTime) * 1F;
        //                        mData.HeightDnTime.Test = true;
        //                        if ((mSpec.MovingSpeed.HeightDn.Min <= mData.HeightDnTime.Data) && (mData.HeightDnTime.Data <= mSpec.MovingSpeed.HeightDn.Max))
        //                            mData.HeightDnTime.Result = RESULT.PASS;
        //                        else mData.HeightDnTime.Result = RESULT.REJECT;

        //                        fpSpread1.ActiveSheet.Cells[10, 6].Text = mData.HeightDnTime.Data.ToString("0.00");
        //                        fpSpread1.ActiveSheet.Cells[10, 7].Text = mData.HeightDnTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.ActiveSheet.Cells[10, 7].ForeColor = mData.HeightDnTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    //}
        //                }
        //            }
        //            else
        //            {
        //                if (/*(FirstPosCheckEnd[SpeedCheckStep] == false) && (*/AdData < mSpec.HeightLimitCurr)//)
        //                {
        //                    SpeedCheckEnd[SpeedCheckStep] = true;
        //                    HeightUpOnOff = false;
        //                    SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;
        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                    mData.HeightUpTime.Data = (mSpec.MovingStroke.Height / EndTime) * 1F;
        //                    mData.HeightUpTime.Test = true;
        //                    if ((mSpec.MovingSpeed.HeightUp.Min <= mData.HeightUpTime.Data) && (mData.HeightUpTime.Data <= mSpec.MovingSpeed.HeightUp.Max))
        //                        mData.HeightUpTime.Result = RESULT.PASS;
        //                    else mData.HeightUpTime.Result = RESULT.REJECT;

        //                    fpSpread1.ActiveSheet.Cells[9, 6].Text = mData.HeightUpTime.Data.ToString("0.00");
        //                    fpSpread1.ActiveSheet.Cells[9, 7].Text = mData.HeightUpTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                    fpSpread1.ActiveSheet.Cells[9, 7].ForeColor = mData.HeightUpTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                }
        //                else
        //                {
        //                    //if (FirstPosCheckEnd[SpeedCheckStep] == false)
        //                    //{
        //                    if ((mSpec.HeightLimitTime * 1000) <= (StepTimeLast - StepTimeFirst))
        //                    {
        //                        SpeedCheckEnd[SpeedCheckStep] = true;
        //                        HeightDnOnOff = false;
        //                        SoundCheckTimeToEnd[SpeedCheckStep] = plot2.Channels[0].Count;
        //                        float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[SpeedCheckStep]) / 1000F;
        //                        mData.HeightUpTime.Data = (mSpec.MovingStroke.Height / EndTime) * 1F;
        //                        mData.HeightUpTime.Test = true;
        //                        if ((mSpec.MovingSpeed.HeightUp.Min <= mData.HeightUpTime.Data) && (mData.HeightUpTime.Data <= mSpec.MovingSpeed.HeightUp.Max))
        //                            mData.HeightUpTime.Result = RESULT.PASS;
        //                        else mData.HeightUpTime.Result = RESULT.REJECT;

        //                        fpSpread1.ActiveSheet.Cells[9, 6].Text = mData.HeightUpTime.Data.ToString("0.00");
        //                        fpSpread1.ActiveSheet.Cells[9, 7].Text = mData.HeightUpTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.ActiveSheet.Cells[9, 7].ForeColor = mData.HeightUpTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    //}
        //                }
        //            }
        //        }
        //        if (SpeedCheckEnd[SpeedCheckStep] == true)
        //        {
        //            SoundLavelCheck((short)(SpeedCheckStep / 2));
        //            SubStep++;
        //            SpecOutputFlag = false;
        //        }
        //    }
        //    return;
        //}

        //private bool NotImsSlideSpeedCheckFlag { get; set; }
        //private bool NotImsReclineSpeedCheckFlag { get; set; }
        //private bool NotImsTiltSpeedCheckFlag { get; set; }
        //private bool NotImsHeightSpeedCheckFlag { get; set; }

        //private bool NotImsSlideSpeedCheckEnd { get; set; }
        //private bool NotImsReclineSpeedCheckEnd { get; set; }
        //private bool NotImsTiltSpeedCheckEnd { get; set; }
        //private bool NotImsHeightSpeedCheckEnd { get; set; }
        //private long[] SpeedCheckStartTime = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        //private void CheckSpeedNotIMS()
        //{
        //    if (SpecOutputFlag == false)
        //    {
        //        label5.Text = "P/SEAT 속도 측정 중 입니다.";
        //        //label5.Text = "TULT UP 속도 측정 중 입니다.";
        //        //label5.Text = "HEIGHT UP 속도 측정 중 입니다.";
        //        //label5.Text = "RECLINE UP 속도 측정 중 입니다.";
        //        SlideBwdOnOff = true;
        //        ReclineBwdOnOff = true;
        //        TiltDnOnOff = true;
        //        HeightDnOnOff = true;

        //        SpeedCheckStartTime[0] = ComF.timeGetTimems();
        //        SpeedCheckStartTime[1] = ComF.timeGetTimems();
        //        SpeedCheckStartTime[2] = ComF.timeGetTimems();
        //        SpeedCheckStartTime[3] = ComF.timeGetTimems();
        //        NotImsSlideSpeedCheckFlag = false;
        //        NotImsReclineSpeedCheckFlag = false;
        //        NotImsTiltSpeedCheckFlag = false;
        //        NotImsHeightSpeedCheckFlag = false;

        //        NotImsSlideSpeedCheckEnd = false;
        //        NotImsReclineSpeedCheckEnd = false;
        //        NotImsTiltSpeedCheckEnd = false;
        //        NotImsHeightSpeedCheckEnd = false;
        //        SpecOutputFlag = true;
        //    }
        //    else
        //    {
        //        float AdData1;
        //        float AdData2;
        //        float AdData3;
        //        float AdData4;

        //        if (NotImsSlideSpeedCheckEnd == false)
        //        {
        //            if (NotImsSlideSpeedCheckFlag == false)
        //                AdData1 = IOPort.ADRead[mSpec.PinMap.SlideBWD.PinNo];
        //            else AdData1 = IOPort.ADRead[mSpec.PinMap.SlideFWD.PinNo];

        //            if (1000 <= (ComF.timeGetTimems() - SpeedCheckStartTime[0]))
        //            {
        //                bool Flag = false;

        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                {
        //                    if ((AdData1 <= mSpec.SlideLimitCurr) || ((mSpec.SlideLimitTime * 1000) <= (ComF.timeGetTimems() - SpeedCheckStartTime[0]))) Flag = true;
        //                }
        //                else
        //                {
        //                    if ((mSpec.SlideLimitCurr <= AdData1) || ((mSpec.SlideLimitTime * 1000) <= (ComF.timeGetTimems() - SpeedCheckStartTime[0]))) Flag = true;
        //                }
        //                if (Flag == true)
        //                {
        //                    if (NotImsSlideSpeedCheckFlag == false)
        //                        SlideBwdOnOff = false;
        //                    else SlideFwdOnOff = false;

        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[0]) / 1000F;

        //                    if (NotImsSlideSpeedCheckFlag == false)
        //                    {
        //                        mData.SlideBwdTime.Test = true;
        //                        mData.SlideBwdTime.Data = (mSpec.MovingStroke.Slide / EndTime) * 1F;
        //                        if ((mSpec.MovingSpeed.SlideBwd.Min <= mData.SlideBwdTime.Data) && (mData.SlideBwdTime.Data <= mSpec.MovingSpeed.SlideBwd.Max))
        //                            mData.SlideBwdTime.Result = RESULT.PASS;
        //                        else mData.SlideBwdTime.Result = RESULT.REJECT;

        //                        fpSpread1.Sheets[0].Cells[4, 6].Text = mData.SlideBwdTime.Data.ToString("0.00");
        //                        fpSpread1.Sheets[0].Cells[4, 7].Text = mData.SlideBwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.Sheets[0].Cells[4, 7].ForeColor = mData.SlideBwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    else
        //                    {
        //                        mData.SlideFwdTime.Test = true;
        //                        mData.SlideFwdTime.Data = (mSpec.MovingStroke.Slide / EndTime) * 1F;
        //                        if ((mSpec.MovingSpeed.SlideFwd.Min <= mData.SlideFwdTime.Data) && (mData.SlideFwdTime.Data <= mSpec.MovingSpeed.SlideFwd.Max))
        //                            mData.SlideFwdTime.Result = RESULT.PASS;
        //                        else mData.SlideFwdTime.Result = RESULT.REJECT;

        //                        fpSpread1.Sheets[0].Cells[3, 6].Text = mData.SlideFwdTime.Data.ToString("0.00");
        //                        fpSpread1.Sheets[0].Cells[3, 7].Text = mData.SlideFwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.Sheets[0].Cells[3, 7].ForeColor = mData.SlideFwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }

        //                    if (NotImsSlideSpeedCheckFlag == false)
        //                    {
        //                        ComF.timedelay(200);
        //                        NotImsSlideSpeedCheckFlag = true;
        //                        SlideFwdOnOff = true;
        //                        SpeedCheckStartTime[0] = ComF.timeGetTimems();
        //                    }
        //                    else
        //                    {
        //                        NotImsSlideSpeedCheckEnd = true;
        //                    }
        //                }
        //            }
        //        }


        //        if (NotImsReclineSpeedCheckEnd == false)
        //        {
        //            if (NotImsReclineSpeedCheckFlag == false)
        //                AdData2 = IOPort.ADRead[mSpec.PinMap.ReclineBWD.PinNo];
        //            else AdData2 = IOPort.ADRead[mSpec.PinMap.ReclineFWD.PinNo];

        //            if (1000 <= (ComF.timeGetTimems() - SpeedCheckStartTime[0]))
        //            {
        //                bool Flag = false;

        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                {
        //                    if ((AdData2 <= mSpec.ReclinerLimitCurr) || ((mSpec.ReclinerLimitTime * 1000) <= (ComF.timeGetTimems() - SpeedCheckStartTime[0]))) Flag = true;
        //                }
        //                else
        //                {
        //                    if ((mSpec.ReclinerLimitCurr <= AdData2) || ((mSpec.ReclinerLimitTime * 1000) <= (ComF.timeGetTimems() - SpeedCheckStartTime[0]))) Flag = true;
        //                }
        //                if (Flag == true)
        //                {
        //                    if (NotImsReclineSpeedCheckFlag == false)
        //                        ReclineBwdOnOff = false;
        //                    else ReclineFwdOnOff = false;

        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[0]) / 1000F;

        //                    if (NotImsReclineSpeedCheckFlag == false)
        //                    {
        //                        mData.ReclineBwdTime.Test = true;
        //                        mData.ReclineBwdTime.Data = (mSpec.MovingStroke.Recliner / EndTime) * 1F;
        //                        if ((mSpec.MovingSpeed.ReclinerBwd.Min <= mData.ReclineBwdTime.Data) && (mData.ReclineBwdTime.Data <= mSpec.MovingSpeed.ReclinerBwd.Max))
        //                            mData.ReclineBwdTime.Result = RESULT.PASS;
        //                        else mData.ReclineBwdTime.Result = RESULT.REJECT;

        //                        fpSpread1.Sheets[0].Cells[6, 6].Text = mData.ReclineBwdTime.Data.ToString("0.00");
        //                        fpSpread1.Sheets[0].Cells[6, 7].Text = mData.ReclineBwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.Sheets[0].Cells[6, 7].ForeColor = mData.ReclineBwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    else
        //                    {
        //                        mData.ReclineFwdTime.Test = true;
        //                        mData.ReclineFwdTime.Data = (mSpec.MovingStroke.Recliner / EndTime) * 1F;
        //                        if ((mSpec.MovingSpeed.ReclinerFwd.Min <= mData.ReclineFwdTime.Data) && (mData.ReclineFwdTime.Data <= mSpec.MovingSpeed.ReclinerBwd.Max))
        //                            mData.ReclineFwdTime.Result = RESULT.PASS;
        //                        else mData.ReclineFwdTime.Result = RESULT.REJECT;

        //                        fpSpread1.Sheets[0].Cells[5, 6].Text = mData.ReclineFwdTime.Data.ToString("0.00");
        //                        fpSpread1.Sheets[0].Cells[5, 7].Text = mData.ReclineFwdTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.Sheets[0].Cells[5, 7].ForeColor = mData.ReclineFwdTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }

        //                    if (NotImsReclineSpeedCheckFlag == false)
        //                    {
        //                        ComF.timedelay(200);
        //                        NotImsReclineSpeedCheckFlag = true;
        //                        ReclineFwdOnOff = true;
        //                        SpeedCheckStartTime[1] = ComF.timeGetTimems();
        //                    }
        //                    else
        //                    {
        //                        NotImsReclineSpeedCheckEnd = true;
        //                    }
        //                }
        //            }
        //        }

        //        if (NotImsTiltSpeedCheckEnd == false)
        //        {
        //            if (NotImsTiltSpeedCheckFlag == false)
        //                AdData3 = IOPort.ADRead[mSpec.PinMap.TiltDn.PinNo];
        //            else AdData3 = IOPort.ADRead[mSpec.PinMap.TiltUp.PinNo];

        //            if (1000 <= (ComF.timeGetTimems() - SpeedCheckStartTime[1]))
        //            {
        //                bool Flag = false;

        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                {
        //                    if ((AdData3 <= mSpec.TiltLimitCurr) || ((mSpec.TiltLimitTime * 1000) <= (ComF.timeGetTimems() - SpeedCheckStartTime[1]))) Flag = true;
        //                }
        //                else
        //                {
        //                    if ((mSpec.TiltLimitCurr <= AdData3) || ((mSpec.TiltLimitTime * 1000) <= (ComF.timeGetTimems() - SpeedCheckStartTime[1]))) Flag = true;
        //                }
        //                if (Flag == true)
        //                {
        //                    if (NotImsTiltSpeedCheckFlag == false)
        //                        TiltDnOnOff = false;
        //                    else TiltUpOnOff = false;

        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[1]) / 1000F;

        //                    if (NotImsTiltSpeedCheckFlag == false)
        //                    {
        //                        mData.TiltDnTime.Test = true;
        //                        mData.TiltDnTime.Data = (mSpec.MovingStroke.Tilt / EndTime) * 1F;
        //                        if ((mSpec.MovingSpeed.TiltDn.Min <= mData.TiltDnTime.Data) && (mData.TiltDnTime.Data <= mSpec.MovingSpeed.TiltDn.Max))
        //                            mData.TiltDnTime.Result = RESULT.PASS;
        //                        else mData.TiltDnTime.Result = RESULT.REJECT;

        //                        fpSpread1.Sheets[0].Cells[8, 6].Text = mData.TiltDnTime.Data.ToString("0.00");
        //                        fpSpread1.Sheets[0].Cells[8, 7].Text = mData.TiltDnTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.Sheets[0].Cells[8, 7].ForeColor = mData.TiltDnTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    else
        //                    {
        //                        mData.TiltUpTime.Test = true;
        //                        mData.TiltUpTime.Data = (mSpec.MovingStroke.Tilt / EndTime) * 1F;
        //                        if ((mSpec.MovingSpeed.TiltUp.Min <= mData.TiltUpTime.Data) && (mData.TiltUpTime.Data <= mSpec.MovingSpeed.TiltUp.Max))
        //                            mData.TiltUpTime.Result = RESULT.PASS;
        //                        else mData.TiltUpTime.Result = RESULT.REJECT;

        //                        fpSpread1.Sheets[0].Cells[7, 6].Text = mData.TiltUpTime.Data.ToString("0.00");
        //                        fpSpread1.Sheets[0].Cells[7, 7].Text = mData.TiltUpTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.Sheets[0].Cells[7, 7].ForeColor = mData.TiltUpTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }

        //                    if (NotImsTiltSpeedCheckFlag == false)
        //                    {
        //                        ComF.timedelay(200);
        //                        NotImsTiltSpeedCheckFlag = true;
        //                        TiltUpOnOff = true;
        //                        SpeedCheckStartTime[2] = ComF.timeGetTimems();
        //                    }
        //                    else
        //                    {
        //                        NotImsTiltSpeedCheckEnd = true;
        //                    }
        //                }
        //            }
        //        }

        //        if (NotImsHeightSpeedCheckEnd == false)
        //        {
        //            if (NotImsHeightSpeedCheckFlag == false)
        //                AdData4 = IOPort.ADRead[mSpec.PinMap.HeightDn.PinNo];
        //            else AdData4 = IOPort.ADRead[mSpec.PinMap.HeightUp.PinNo];

        //            if (1000 <= (ComF.timeGetTimems() - SpeedCheckStartTime[2]))
        //            {
        //                bool Flag = false;

        //                if (comboBox4.SelectedItem.ToString() == "IMS")
        //                {
        //                    if ((AdData4 <= mSpec.HeightLimitCurr) || ((mSpec.HeightLimitTime * 1000) <= (ComF.timeGetTimems() - SpeedCheckStartTime[2]))) Flag = true;
        //                }
        //                else
        //                {
        //                    if (NotImsHeightSpeedCheckFlag == true)
        //                    {
        //                        if ((mSpec.HeightLimitCurr <= AdData4) || ((mSpec.HeightLimitTime * 1000) <= (ComF.timeGetTimems() - SpeedCheckStartTime[2]))) Flag = true;
        //                    }
        //                    else
        //                    {
        //                        if ((5 <= AdData4) || ((mSpec.HeightLimitTime * 1000) <= (ComF.timeGetTimems() - SpeedCheckStartTime[2]))) Flag = true;
        //                    }
        //                }
        //                if (Flag == true)
        //                {
        //                    if (NotImsHeightSpeedCheckFlag == false)
        //                        HeightDnOnOff = false;
        //                    else HeightUpOnOff = false;

        //                    float EndTime = (ComF.timeGetTimems() - SpeedCheckStartTime[2]) / 1000F;

        //                    if (NotImsHeightSpeedCheckFlag == false)
        //                    {
        //                        mData.HeightDnTime.Test = true;
        //                        mData.HeightDnTime.Data = (mSpec.MovingStroke.Height / EndTime) * 1F;
        //                        if ((mSpec.MovingSpeed.HeightDn.Min <= mData.HeightDnTime.Data) && (mData.HeightDnTime.Data <= mSpec.MovingSpeed.HeightDn.Max))
        //                            mData.HeightDnTime.Result = RESULT.PASS;
        //                        else mData.HeightDnTime.Result = RESULT.REJECT;

        //                        fpSpread1.Sheets[0].Cells[10, 6].Text = mData.HeightDnTime.Data.ToString("0.00");
        //                        fpSpread1.Sheets[0].Cells[10, 7].Text = mData.HeightDnTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.Sheets[0].Cells[10, 7].ForeColor = mData.HeightDnTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }
        //                    else
        //                    {
        //                        mData.HeightUpTime.Test = true;
        //                        mData.HeightUpTime.Data = (mSpec.MovingStroke.Height / EndTime) * 1F;
        //                        if ((mSpec.MovingSpeed.HeightUp.Min <= mData.HeightUpTime.Data) && (mData.HeightUpTime.Data <= mSpec.MovingSpeed.HeightUp.Max))
        //                            mData.HeightUpTime.Result = RESULT.PASS;
        //                        else mData.HeightUpTime.Result = RESULT.REJECT;

        //                        fpSpread1.Sheets[0].Cells[9, 6].Text = mData.HeightUpTime.Data.ToString("0.00");
        //                        fpSpread1.Sheets[0].Cells[9, 7].Text = mData.HeightUpTime.Result == RESULT.PASS ? "O.K" : "N.G";
        //                        fpSpread1.Sheets[0].Cells[9, 7].ForeColor = mData.HeightUpTime.Result == RESULT.PASS ? Color.Black : Color.Red;
        //                    }

        //                    if (NotImsHeightSpeedCheckFlag == false)
        //                    {
        //                        ComF.timedelay(200);
        //                        NotImsHeightSpeedCheckFlag = true;
        //                        HeightUpOnOff = true;
        //                        SpeedCheckStartTime[3] = ComF.timeGetTimems();
        //                    }
        //                    else
        //                    {
        //                        NotImsHeightSpeedCheckEnd = true;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if ((NotImsSlideSpeedCheckEnd == true) && (NotImsTiltSpeedCheckEnd == true) && (NotImsHeightSpeedCheckEnd == true) && (NotImsReclineSpeedCheckEnd == true))
        //    {
        //        SpecOutputFlag = false;
        //        Step++;
        //    }

        //    return;
        //}
        private void SoundCheck()
        {
            if (mData.ReclineFwdSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.ReclineFwdSound.StartData)
                    mData.ReclineFwdSound.ResultStart = RESULT.REJECT;
                else mData.ReclineFwdSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.ReclineFwdSound.RunData)
                    mData.ReclineFwdSound.RunData = RESULT.REJECT;
                else mData.ReclineFwdSound.RunData = RESULT.PASS;

            }

            if (mData.ReclineBwdSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.ReclineBwdSound.StartData)
                    mData.ReclineBwdSound.ResultStart = RESULT.REJECT;
                else mData.ReclineBwdSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.ReclineBwdSound.RunData)
                    mData.ReclineBwdSound.ResultRun = RESULT.REJECT;
                else mData.ReclineBwdSound.ResultRun = RESULT.PASS;
            }

            if (mData.RelaxSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.RelaxSound.StartData)
                    mData.RelaxSound.ResultStart = RESULT.REJECT;
                else mData.RelaxSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.RelaxSound.RunData)
                    mData.RelaxSound.ResultRun = RESULT.REJECT;
                else mData.RelaxSound.ResultRun = RESULT.PASS;
            }

            if (mData.RelaxReturnSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.RelaxReturnSound.StartData)
                    mData.RelaxReturnSound.ResultStart = RESULT.REJECT;
                else mData.RelaxReturnSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.RelaxReturnSound.RunData)
                    mData.RelaxReturnSound.ResultRun = RESULT.REJECT;
                else mData.RelaxReturnSound.ResultRun = RESULT.PASS;
            }
            if (mData.HeightUpSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.HeightUpSound.StartData)
                    mData.HeightUpSound.ResultStart = RESULT.REJECT;
                else mData.HeightUpSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.HeightUpSound.RunData)
                    mData.HeightUpSound.ResultRun = RESULT.REJECT;
                else mData.HeightUpSound.ResultRun = RESULT.PASS;
            }

            if (mData.HeightDnSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.HeightDnSound.StartData)
                    mData.HeightDnSound.ResultStart = RESULT.REJECT;
                else mData.HeightDnSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.HeightDnSound.RunData)
                    mData.HeightDnSound.ResultRun = RESULT.REJECT;
                else mData.HeightDnSound.ResultRun = RESULT.PASS;
            }

            if (mData.LegrestSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.LegrestSound.StartData)
                    mData.LegrestSound.ResultStart = RESULT.REJECT;
                else mData.LegrestSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.LegrestSound.RunData)
                    mData.LegrestSound.ResultRun = RESULT.REJECT;
                else mData.LegrestSound.ResultRun = RESULT.PASS;
            }

            if (mData.LegrestReturnSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.LegrestReturnSound.StartData)
                    mData.LegrestReturnSound.ResultStart = RESULT.REJECT;
                else mData.LegrestReturnSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.LegrestReturnSound.RunData)
                    mData.LegrestReturnSound.ResultRun = RESULT.REJECT;
                else mData.LegrestReturnSound.ResultRun = RESULT.PASS;
            }

            if (mData.LegrestExtSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.LegrestExtSound.StartData)
                    mData.LegrestExtSound.ResultStart = RESULT.REJECT;
                else mData.LegrestExtSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.LegrestExtSound.RunData)
                    mData.LegrestExtSound.ResultRun = RESULT.REJECT;
                else mData.LegrestExtSound.ResultRun = RESULT.PASS;
            }

            if (mData.LegrestExtReturnSound.Test == true)
            {
                if (mSpec.Sound.StartMax < mData.LegrestExtReturnSound.StartData)
                    mData.LegrestExtReturnSound.ResultStart = RESULT.REJECT;
                else mData.LegrestExtReturnSound.ResultStart = RESULT.PASS;

                if (mSpec.Sound.RunMax < mData.LegrestExtReturnSound.RunData)
                    mData.LegrestExtReturnSound.ResultRun = RESULT.REJECT;
                else mData.LegrestExtReturnSound.ResultRun = RESULT.PASS;
            }

            
            if (mData.ReclineFwdSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[17, 6].Text = mData.ReclineFwdSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[18, 6].Text = mData.ReclineFwdSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[17, 7].Text = (mData.ReclineFwdSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[17, 7].ForeColor = (mData.ReclineFwdSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[18, 7].Text = (mData.ReclineFwdSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[18, 7].ForeColor = (mData.ReclineFwdSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }


            if (mData.ReclineBwdSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[17, 6].Text = mData.ReclineBwdSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[18, 6].Text = mData.ReclineBwdSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[17, 7].Text = (mData.ReclineBwdSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[17, 7].ForeColor = (mData.ReclineBwdSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[18, 7].Text = (mData.ReclineBwdSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[18, 7].ForeColor = (mData.ReclineBwdSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }

            if (mData.RelaxSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[19, 6].Text = mData.RelaxSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[20, 6].Text = mData.RelaxSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[19, 7].Text = (mData.RelaxSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[19, 7].ForeColor = (mData.RelaxSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[20, 7].Text = (mData.RelaxSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[20, 7].ForeColor = (mData.RelaxSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }


            if (mData.RelaxReturnSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[19, 6].Text = mData.RelaxReturnSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[20, 6].Text = mData.RelaxReturnSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[19, 7].Text = (mData.RelaxReturnSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[19, 7].ForeColor = (mData.RelaxReturnSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[20, 7].Text = (mData.RelaxReturnSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[20, 7].ForeColor = (mData.RelaxReturnSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }

            if(mData.HeightUpSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[21, 6].Text = mData.HeightUpSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[22, 6].Text = mData.HeightUpSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[21, 7].Text = (mData.HeightUpSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[21, 7].ForeColor = (mData.HeightUpSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[22, 7].Text = (mData.HeightUpSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[22, 7].ForeColor = (mData.HeightUpSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }


            if (mData.HeightDnSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[21, 6].Text = mData.HeightDnSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[22, 6].Text = mData.HeightDnSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[21, 7].Text = (mData.HeightDnSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[21, 7].ForeColor = (mData.HeightDnSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[22, 7].Text = (mData.HeightDnSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[22, 7].ForeColor = (mData.HeightDnSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }

            if (mData.LegrestSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[23, 6].Text = mData.LegrestSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[24, 6].Text = mData.LegrestSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[23, 7].Text = (mData.LegrestSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[23, 7].ForeColor = (mData.LegrestSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[24, 7].Text = (mData.LegrestSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[24, 7].ForeColor = (mData.LegrestSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }


            if (mData.LegrestReturnSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[23, 6].Text = mData.LegrestReturnSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[24, 6].Text = mData.LegrestReturnSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[23, 7].Text = (mData.LegrestReturnSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[23, 7].ForeColor = (mData.LegrestReturnSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[24, 7].Text = (mData.LegrestReturnSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[24, 7].ForeColor = (mData.LegrestReturnSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }

            if (mData.LegrestExtSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[25, 6].Text = mData.LegrestExtSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[26, 6].Text = mData.LegrestExtSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[25, 7].Text = (mData.LegrestExtSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[25, 7].ForeColor = (mData.LegrestExtSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[26, 7].Text = (mData.LegrestExtSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[26, 7].ForeColor = (mData.LegrestExtSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }


            if (mData.LegrestExtReturnSound.Test == true)
            {
                fpSpread1.ActiveSheet.Cells[25, 6].Text = mData.LegrestExtReturnSound.StartData.ToString("0.00");
                fpSpread1.ActiveSheet.Cells[26, 6].Text = mData.LegrestExtReturnSound.RunData.ToString("0.00");

                fpSpread1.ActiveSheet.Cells[25, 7].Text = (mData.LegrestExtReturnSound.ResultStart == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[25, 7].ForeColor = (mData.LegrestExtReturnSound.ResultStart == RESULT.PASS) ? Color.Green : Color.Red;

                fpSpread1.ActiveSheet.Cells[26, 7].Text = (mData.LegrestExtReturnSound.ResultRun == RESULT.PASS) ? "O.K" : "N.G";
                fpSpread1.ActiveSheet.Cells[26, 7].ForeColor = (mData.LegrestExtReturnSound.ResultRun == RESULT.PASS) ? Color.Green : Color.Red;
            }
            SpecOutputFlag = false;
            Step++;
            return;
        }
        
        private bool ReclineMotorMoveEndFlag { get; set; }
        private bool HeightMotorMoveEndFlag { get; set; }
        private bool RelaxMotorMoveEndFlag { get; set; }
        private bool LegrestMotorMoveEndFlag { get; set; }
        private bool LegrestExtMotorMoveEndFlag { get; set; }
        private void DeliveryPosMoveing()
        {
            if (SpecOutputFlag == false)
            {
                SpecOutputFlag = true;

                if (CheckItem.Recline == true)
                {
                    if (mSpec.DeliveryPos.Recliner == 0)//Fwd
                    {
                        ReclineFwdOnOff = true;
                        ReclineMotorMoveEndFlag = false;
                    }
                    else if (mSpec.DeliveryPos.Recliner == 1)//Mid
                    {
                        ReclineBwdOnOff = true;
                        ReclineMotorMoveEndFlag = false;
                    }
                    else
                    {
                        ReclineBwdOnOff = true;
                        ReclineMotorMoveEndFlag = false;
                    }
                }
                else
                {
                    ReclineMotorMoveEndFlag = true;
                }

                if (CheckItem.Relax == true)
                {
                    if (mSpec.DeliveryPos.Relax == 0)
                    {
                        RelaxOnOff = true;
                        RelaxMotorMoveEndFlag = false;

                    }
                    else
                    {
                        RelaxReturnOnOff = true;
                        RelaxMotorMoveEndFlag = false;
                    }
                }
                else
                {
                    RelaxMotorMoveEndFlag = true;
                }

                if (CheckItem.Legrest == true)
                {
                    if (mSpec.DeliveryPos.Legrest == 0)//Up
                    {
                        LegrestOnOff = true;
                        LegrestMotorMoveEndFlag = false;
                    }
                    else
                    {
                        LegrestReturnOnOff = true;
                        LegrestMotorMoveEndFlag = false;
                    }
                }
                else
                {
                    LegrestMotorMoveEndFlag = true;
                }

                if (CheckItem.LegrestExit == true)
                {
                    if (mSpec.DeliveryPos.LegrestExt == 0)//Up
                    {
                        LegrestExtOnOff = true;
                        LegrestExtMotorMoveEndFlag = false;
                    }
                    else
                    {
                        LegrestExtReturnOnOff = true;
                        LegrestExtMotorMoveEndFlag = false;
                    }
                }
                else
                {
                    LegrestExtMotorMoveEndFlag = true;
                }

                if (CheckItem.Height == true)
                {
                    if (mSpec.DeliveryPos.Height == 0)//Up
                    {
                        HeightUpOnOff = true;
                        HeightMotorMoveEndFlag = false;
                    }
                    else
                    {
                        HeightDnOnOff = true;
                        HeightMotorMoveEndFlag = false;
                    }
                }
                else
                {
                    HeightMotorMoveEndFlag = true;
                }
                StepTimeFirst = ComF.timeGetTimems();
                StepTimeLast = ComF.timeGetTimems();

                label5.Text = "출하 위치로 이동 중 입니다.";
            }
            else
            {
                StepTimeLast = ComF.timeGetTimems();

                if (CheckItem.Relax == true)
                {
                    if ((mSpec.RelaxSwOnTime * 1000) <= (StepTimeLast - StepTimeFirst))
                    {
                        if (RelaxOnOff == true) RelaxOnOff = false;
                        if (RelaxReturnOnOff == true) RelaxReturnOnOff = false;
                    }
                }

                if (1000 <= (StepTimeLast - StepTimeFirst))
                {
                    float AdData;

                    AdData = pMeter.GetPSeat;

                    if(AdData < 0.5)
                    {
                        if (ReclineMotorMoveEndFlag == false)
                        {
                            if(ReclineFwdOnOff == true) ReclineFwdOnOff = false;
                            if(ReclineBwdOnOff == true) ReclineBwdOnOff = false;
                            ReclineMotorMoveEndFlag = true;
                        }

                        if (HeightMotorMoveEndFlag == false)
                        {
                            if (HeightUpOnOff == true) HeightUpOnOff = false;
                            if (HeightDnOnOff == true) HeightDnOnOff = false;
                            HeightMotorMoveEndFlag = true;
                        }

                        if (LegrestMotorMoveEndFlag == false)
                        {
                            if (LegrestOnOff == true) LegrestOnOff = false;
                            if (LegrestReturnOnOff == true) LegrestReturnOnOff = false;
                            LegrestMotorMoveEndFlag = true;
                        }

                        if (LegrestExtMotorMoveEndFlag == false)
                        {
                            if (LegrestExtOnOff == true) LegrestExtOnOff = false;
                            if (LegrestExtReturnOnOff == true) LegrestExtReturnOnOff = false;
                            LegrestExtMotorMoveEndFlag = true;
                        }

                        if (RelaxMotorMoveEndFlag == false)
                        {
                            RelaxMotorMoveEndFlag = true;
                        }
                    }
                    if (ReclineMotorMoveEndFlag == false)
                    {
                        if ((mSpec.ReclinerLimitTime * 1000F) <= (StepTimeLast - StepTimeFirst))
                        {
                            if (ReclineFwdOnOff == true) ReclineFwdOnOff = false;
                            if (ReclineBwdOnOff == true) ReclineBwdOnOff = false;
                            ReclineMotorMoveEndFlag = true;
                        }
                    }

                    if (HeightMotorMoveEndFlag == false)
                    {
                        if ((mSpec.HeightLimitTime * 1000F) <= (StepTimeLast - StepTimeFirst))
                        {
                            if (HeightUpOnOff == true) HeightUpOnOff = false;
                            if (HeightDnOnOff == true) HeightDnOnOff = false;
                            HeightMotorMoveEndFlag = true;
                        }
                    }
                    if (RelaxMotorMoveEndFlag == false)
                    {
                        if ((mSpec.RelaxLimitTime * 1000F) <= (StepTimeLast - StepTimeFirst))
                        {
                            RelaxMotorMoveEndFlag = true;
                        }
                    }

                    if (LegrestMotorMoveEndFlag == false)
                    {
                        if ((mSpec.LegrestLimitTime * 1000F) <= (StepTimeLast - StepTimeFirst))
                        {
                            if (LegrestOnOff == true) LegrestOnOff = false;
                            if (LegrestReturnOnOff == true) LegrestReturnOnOff = false;
                            LegrestMotorMoveEndFlag = true;
                        }
                    }

                    if (LegrestExtMotorMoveEndFlag == false)
                    {
                        if ((mSpec.LegrestExtLimitTime * 1000F) <= (StepTimeLast - StepTimeFirst))
                        {
                            if (LegrestExtOnOff == true) LegrestExtOnOff = false;
                            if (LegrestExtReturnOnOff == true) LegrestExtReturnOnOff = false;
                            LegrestExtMotorMoveEndFlag = true;
                        }
                    }
                }
            }

            if ((ReclineMotorMoveEndFlag == true) && (HeightMotorMoveEndFlag == true) && (RelaxMotorMoveEndFlag == true) && (LegrestMotorMoveEndFlag == true) && (LegrestExtMotorMoveEndFlag == true))
            {
                SpecOutputFlag = false;
                Step++;
            }
            return;
        }

        private void ResultCheck()
        {
            mData.Result = RESULT.PASS;
            if ((mData.Height.Test == true) && (mData.Height.Result1 == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            if ((mData.Relax.Test == true) && (mData.Relax.Result1 == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            if ((mData.Legrest.Test == true) && (mData.Legrest.Result1 == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            if ((mData.LegrestExt.Test == true) && (mData.LegrestExt.Result1 == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            if ((mData.Recline.Test == true) && (mData.Recline.Result1 == RESULT.REJECT)) mData.Result = RESULT.REJECT;

            if ((mData.RelaxTime.Test == true) && (mData.RelaxTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            if ((mData.RelaxReturnTime.Test == true) && (mData.RelaxReturnTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            
            if ((mData.ReclineFwdTime.Test == true) && (mData.ReclineFwdTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            if ((mData.ReclineBwdTime.Test == true) && (mData.ReclineBwdTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            
            if (((mData.LegrestTime.Test == true) && mData.LegrestTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            if ((mData.LegrestReturnTime.Test == true) && (mData.LegrestReturnTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;

            if (((mData.LegrestExtTime.Test == true) && mData.LegrestExtTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            if ((mData.LegrestExtReturnTime.Test == true) && (mData.LegrestExtReturnTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;

            if ((mData.HeightUpTime.Test == true) && (mData.HeightUpTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;
            if ((mData.HeightDnTime.Test == true) && (mData.HeightDnTime.Result == RESULT.REJECT)) mData.Result = RESULT.REJECT;

            if (mData.ReclineFwdSound.Test == true)
            {
                if (mData.ReclineFwdSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.ReclineFwdSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }

            if (mData.ReclineBwdSound.Test == true)
            {
                if (mData.ReclineBwdSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.ReclineBwdSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }

            if (mData.HeightUpSound.Test == true)
            {
                if (mData.HeightUpSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.HeightUpSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }

            if (mData.HeightDnSound.Test == true)
            {
                if (mData.HeightDnSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.HeightDnSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }

            if (mData.RelaxSound.Test == true)
            {
                if (mData.RelaxSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.RelaxSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }

            if (mData.RelaxReturnSound.Test == true)
            {
                if (mData.RelaxReturnSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.RelaxReturnSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }

            if (mData.LegrestSound.Test == true)
            {
                if (mData.LegrestSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.LegrestSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }

            if (mData.LegrestReturnSound.Test == true)
            {
                if (mData.LegrestReturnSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.LegrestReturnSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }
            if (mData.LegrestExtSound.Test == true)
            {
                if (mData.LegrestExtSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.LegrestExtSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }

            if (mData.LegrestExtReturnSound.Test == true)
            {
                if (mData.LegrestExtReturnSound.ResultStart == RESULT.REJECT) mData.Result = RESULT.REJECT;
                if (mData.LegrestExtReturnSound.ResultRun == RESULT.REJECT) mData.Result = RESULT.REJECT;
            }
            IOPort.TestINGOnOff = false;
            if (mData.Result == RESULT.REJECT)
            {
                label16.Text = "NG";
                label16.ForeColor = Color.Red;

                IOPort.YellowLampOnOff = false;
                IOPort.RedLampOnOff = true;
                IOPort.GreenLampOnOff = false;

                IOPort.BuzzerOnOff = true;
                BuzzerRunFlag = true;
                BuzerOnOff = true;
                BuzzerLast = ComF.timeGetTimems();
                BuzzerFirst = ComF.timeGetTimems();
                BuzzerOnCount = 0;
            }
            else
            {
                label16.Text = "OK";
                label16.ForeColor = Color.Lime;

                IOPort.YellowLampOnOff = false;
                IOPort.RedLampOnOff = false;
                IOPort.GreenLampOnOff = true;
                IOPort.BuzzerOnOff = true;
                ComF.timedelay(500);
                IOPort.BuzzerOnOff = false;
            }
            if (mData.Result == RESULT.REJECT)
                label5.Text = "불량 제품 입니다.";
            else label5.Text = "양품 제품 입니다.";

            return;
        }

        private void SendTestData()
        {
            return;
        }
      

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            if(IOPort.GetAuto == false) panel4.Visible = !panel4.Visible;
            if(panel4.Visible == true)
            {
                //panel4.Parent = this;
                panel4.BringToFront();
            }
            else
            {
                IOPort.PinConnectionOnOff = false;
            }
            IOPort.FunctionIOInit();
            return;
        }

        private void imageButton1_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            if(But.ButtonColor == Color.Black)
            {
                PowerOnOff = true;
                BattOnOff = true;
                
                But.ButtonColor = Color.Red;
            }
            else
            {
                PowerOnOff = false;
                BattOnOff = false;
                But.ButtonColor = Color.Black;
            }
            return;
        }

    
        private void ledArrow4_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //Recline Fwd
            ReclineFwdOnOff = e.ValueNew;
            return;
        }

        private void ledArrow3_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //Recline Bwd
            ReclineBwdOnOff = e.ValueNew;
            return;
        }

        private void ledArrow7_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //relax
            RelaxOnOff = e.ValueNew;
            return;
        }

        private void ledArrow8_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //relax return
            RelaxReturnOnOff = e.ValueNew;
            return;
        }

        private void ledArrow5_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //Height Up
            HeightUpOnOff = e.ValueNew;
            return;
        }

        private void ledArrow6_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //Height Dn
            HeightDnOnOff = e.ValueNew;
            return;
        }

        private void ledArrow11_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //Legrest 
            LegrestOnOff = e.ValueNew;            
            return;
        }

        private void ledArrow12_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //regrest return
            LegrestReturnOnOff = e.ValueNew;
            return;
        }

        private void ledArrow9_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //regrest ext
            LegrestExtOnOff = e.ValueNew;
            return;
        }

        private void ledArrow10_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            //legrest ext return
            LegrestExtReturnOnOff = e.ValueNew;
            return;
        }

        

        private void ledArrow4_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow4_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void ledArrow3_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow3_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void ledArrow7_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow7_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void ledArrow8_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow8_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void ledArrow5_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow5_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void ledArrow6_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow6_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void ledArrow11_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow11_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void ledArrow12_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow12_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void ledArrow9_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow9_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void ledArrow10_MouseDown(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = true;
            return;
        }

        private void ledArrow10_MouseUp(object sender, MouseEventArgs e)
        {
            Iocomp.Instrumentation.Professional.LedArrow LBut = sender as Iocomp.Instrumentation.Professional.LedArrow;
            LBut.Value.AsBoolean = false;
            return;
        }

        private void imageButton2_Click(object sender, EventArgs e)
        {
            if (RunningFlag == false)
            {
                if (IOPort.GetAuto == true)
                {
                    if (PopReadOk == true) StartSetting();
                }
                else
                {
                    StartSetting();
                }
            }
            return;
        }

        private int RowCount = 0;

        private void CreateFileName()
        {
            string Path = Program.DATA_PATH.ToString() + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".xls";

            if ((Infor.DataName != "") && (Infor.DataName != null))
            {
                if (File.Exists(Infor.DataName))
                {
                    if (Infor.DataName != Path)
                    {
                        Infor.Date = DateTime.Now.ToString("yyyyMMdd");
                        Infor.DataName = Path;
                        Infor.TotalCount = 0;
                        Infor.OkCount = 0;
                        Infor.NgCount = 0;
                        SaveInfor();
                    }
                }
            }
            else
            {
                Infor.Date = DateTime.Now.ToString("yyyyMMdd");

                Infor.DataName = Path;
                Infor.TotalCount = 0;
                Infor.OkCount = 0;
                Infor.NgCount = 0;
                SaveInfor();
            }

            if (File.Exists(Infor.DataName) == false)
            {
                CreateDataFile();
            }
            else
            {
                fpSpread2.OpenExcel(Infor.DataName);

                RowCount = 6;
                for (int i = RowCount; i < fpSpread2.ActiveSheet.RowCount; i++)
                {
                    if (fpSpread2.ActiveSheet.Cells[RowCount, 0].Text == "") break;
                    if (fpSpread2.ActiveSheet.Cells[RowCount, 0].Text == null) break;
                    RowCount++;
                }
            }
            return;
        }

        private void SaveData()
        {
            string Path = Program.DATA_PATH.ToString() + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".xls";

            if ((Infor.DataName != "") && (Infor.DataName != null))
            {
                if (File.Exists(Infor.DataName))
                {
                    if (Infor.DataName != Path)
                    {
                        Infor.Date = DateTime.Now.ToString("yyyyMMdd");
                        Infor.DataName = Path;
                        Infor.TotalCount = 1;
                        if (mData.Result == RESULT.PASS)
                            Infor.OkCount = 1;
                        else Infor.NgCount = 1;
                        SaveInfor();
                    }
                    else
                    {
                        SaveInfor();
                    }
                }
                else
                {
                    SaveInfor();
                }
            }
            else
            {
                Infor.Date = DateTime.Now.ToString("yyyyMMdd");
                Infor.DataName = Path;
                Infor.TotalCount = 1;
                if (mData.Result == RESULT.PASS)
                    Infor.OkCount = 1;
                else Infor.NgCount = 1;
                SaveInfor();
            }

            if(Infor.DataName != Path)
            {
                Infor.Date = DateTime.Now.ToString("yyyyMMdd");
                Infor.DataName = Path;
                Infor.TotalCount = 1;
                if (mData.Result == RESULT.PASS)
                    Infor.OkCount = 1;
                else Infor.NgCount = 1;
                SaveInfor();
            }
            if (File.Exists(Infor.DataName) == false) CreateDataFile();

            int Col = 0;

            fpSpread2.ActiveSheet.RowCount = RowCount + 1;

            fpSpread2.ActiveSheet.SetRowHeight(RowCount, 21);
            if (fpSpread2.ActiveSheet.ColumnCount != 10) fpSpread2.ActiveSheet.ColumnCount = 10;

            for (int i = 0; i < fpSpread2.ActiveSheet.ColumnCount; i++)
            {
                fpSpread2.ActiveSheet.Cells[RowCount, i].CellType = new FarPoint.Win.Spread.CellType.TextCellType();
                fpSpread2.ActiveSheet.Cells[RowCount, i].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
                fpSpread2.ActiveSheet.Cells[RowCount, i].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
                fpSpread2.ActiveSheet.Cells[RowCount, i].Border = LineBorderToData;
                fpSpread2.ActiveSheet.Cells[RowCount, i].Text = "";
            }
            //No.
            fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
            fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = ((RowCount - 6) + 1).ToString();
            Col++;

            //Time
            fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
            fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = DateTime.Now.ToLongTimeString();
            Col++;

            //Barcode
            fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
            fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = label17.Text;
            Col++;

            if (mData.Result == RESULT.PASS)
            {
                fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
            }
            else if (mData.Result == RESULT.REJECT)
            {
                fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
            }
            if (mData.Result == RESULT.PASS)
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = "O.K";
            else if (mData.Result == RESULT.REJECT)
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = "N.G";
            Col++;

            

            //Slide Curr
            if (mData.Recline.Test == true)
            {
                fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = fpSpread1.ActiveSheet.Cells[2, 3].Text + " ~ " + fpSpread1.ActiveSheet.Cells[2, 4].Text;
                Col++;

                if (mData.Recline.Result1 != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.Recline.Data1);
            }
            else
            {
                Col++;
            }
            Col++;
            //Relax Curr
            if (mData.Recline.Test == true)
            {
                fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = fpSpread1.ActiveSheet.Cells[3, 3].Text + " ~ " + fpSpread1.ActiveSheet.Cells[3, 4].Text;
                Col++;

                if (mData.Relax.Result1 != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.Relax.Data1);
            }
            else
            {
                Col++;
            }
            Col++;


            //Height Curr
            if (mData.Height.Test == true)
            {
                fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = fpSpread1.ActiveSheet.Cells[4, 3].Text + " ~ " + fpSpread1.ActiveSheet.Cells[4, 4].Text;
                Col++;

                if (mData.Height.Result1 != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.Height.Data1);
            }
            else
            {
                Col++;
            }
            Col++;

            //Legrest
            if (mData.Legrest.Test == true)
            {
                fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = fpSpread1.ActiveSheet.Cells[5, 3].Text + " ~ " + fpSpread1.ActiveSheet.Cells[5, 4].Text;
                Col++;

                if (mData.Legrest.Result1 != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.Legrest.Data1);
            }
            else
            {
                Col++;
            }
            Col++;


            //Legrest ext
            if (mData.LegrestExt.Test == true)
            {
                fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = fpSpread1.ActiveSheet.Cells[6, 3].Text + " ~ " + fpSpread1.ActiveSheet.Cells[6, 4].Text;
                Col++;

                if (mData.LegrestExt.Result1 != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }
                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.LegrestExt.Data1);
            }
            else
            {
                Col++;
            }
            Col++;

                       
            if (mData.ReclineFwdSound.Test == true)
            {
                if (mData.ReclineFwdSound.ResultStart != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.ReclineFwdSound.StartData);
                Col++;

                if (mData.ReclineFwdSound.ResultRun != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.ReclineFwdSound.RunData);
            }
            else
            {
                Col++;
            }
            Col++;

                        
            if (mData.RelaxSound.Test == true)
            {
                if (mData.RelaxSound.ResultStart != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.RelaxSound.StartData);
                Col++;

                if (mData.RelaxSound.ResultRun != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.RelaxSound.RunData);
            }
            else
            {
                Col++;
            }
            Col++;
            
            if (mData.HeightDnSound.Test == true)
            {
                if (mData.HeightDnSound.ResultStart != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.HeightDnSound.StartData);
                Col++;

                if (mData.HeightDnSound.ResultRun != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.HeightDnSound.RunData);
            }
            else
            {
                Col++;
            }
            Col++;


            if (mData.LegrestSound.Test == true)
            {
                if (mData.LegrestSound.ResultStart != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.LegrestSound.StartData);
                Col++;

                if (mData.LegrestSound.ResultRun != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.LegrestSound.RunData);
            }
            else
            {
                Col++;
            }
            Col++;

            if (mData.LegrestExtSound.Test == true)
            {
                if (mData.LegrestExtSound.ResultStart != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.LegrestExtSound.StartData);
                Col++;

                if (mData.LegrestExtSound.ResultRun != RESULT.REJECT)
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.White;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.Black;
                }
                else
                {
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].BackColor = Color.Red;
                    fpSpread2.ActiveSheet.Cells[RowCount, Col].ForeColor = Color.White;
                }

                fpSpread2.ActiveSheet.Cells[RowCount, Col].Text = string.Format("{0:0.00}", mData.LegrestExtSound.RunData);
            }
            else
            {
                Col++;
            }
            Col++;




            RowCount++;

            fpSpread2.SaveExcel(Infor.DataName);
            return;
        }


        private FarPoint.Win.LineBorder LineBorderToHeader = new FarPoint.Win.LineBorder(Color.Black, 1/*RowHeight*/, true, true, true, true);//line color,line style,left,top,right,buttom                       
        private FarPoint.Win.LineBorder LineBorderToData = new FarPoint.Win.LineBorder(Color.Black, 1/*RowHeight*/, true, false, true, true);//line color,line style,left,top,right,buttom                       
        private void CreateDataFile()
        {
            fpSpread2.ActiveSheet.Reset();

            fpSpread2.ActiveSheet.RowCount = 9;
            //용지 방향
            fpSpread2.ActiveSheet.PrintInfo.Orientation = FarPoint.Win.Spread.PrintOrientation.Landscape;
            //프린트 할 때 가로,세로 중앙에 프린트 할 수 있도록 설정
            fpSpread2.ActiveSheet.PrintInfo.Centering = FarPoint.Win.Spread.Centering.Horizontal; //좌/우 중앙                        
            //fpSpread2.ActiveSheet.PrintInfo.PrintCenterOnPageV = false; //Top 쪽으로간다. 만약 true로 설정할 경우 상,하 중간에 프린트가 된다.

            //여백
            fpSpread2.ActiveSheet.PrintInfo.Margin.Bottom = 1;
            fpSpread2.ActiveSheet.PrintInfo.Margin.Left = 1;
            fpSpread2.ActiveSheet.PrintInfo.Margin.Right = 1;
            fpSpread2.ActiveSheet.PrintInfo.Margin.Top = 2;

            //프린트에서 컬러 표시
            fpSpread2.ActiveSheet.PrintInfo.ShowColor = true;
            //프린트에서 셀 라인 표시여부 (true일경우 내가 그린 라인 말고 셀에 사각 표시 라인도 같이 프린트가 된다.
            fpSpread2.ActiveSheet.PrintInfo.ShowGrid = false;

            fpSpread2.ActiveSheet.VerticalGridLine = new FarPoint.Win.Spread.GridLine(FarPoint.Win.Spread.GridLineType.None);
            fpSpread2.ActiveSheet.HorizontalGridLine = new FarPoint.Win.Spread.GridLine(FarPoint.Win.Spread.GridLineType.None);
            //용지 넓이에 페이지 맞춤
            fpSpread2.ActiveSheet.PrintInfo.UseSmartPrint = true;

            //그리드를 표시할 경우 저장할 때나 프린트 할 때 화면에 같이 표시,그리드가 프린트 되기 때문에 지저분해 보인다.
            fpSpread2.ActiveSheet.PrintInfo.ShowColumnFooter = FarPoint.Win.Spread.PrintHeader.Hide;
            fpSpread2.ActiveSheet.PrintInfo.ShowColumnFooterEachPage = false;


            //헤더와 밖같 라인이 같이 프린트 되지 않도록 한다.
            fpSpread2.ActiveSheet.PrintInfo.ShowBorder = false;
            fpSpread2.ActiveSheet.PrintInfo.ShowColumnHeader = FarPoint.Win.Spread.PrintHeader.Hide;
            fpSpread2.ActiveSheet.PrintInfo.ShowRowHeader = FarPoint.Win.Spread.PrintHeader.Hide;
            fpSpread2.ActiveSheet.PrintInfo.ShowShadows = false;
            fpSpread2.ActiveSheet.PrintInfo.ShowTitle = FarPoint.Win.Spread.PrintTitle.Hide;
            fpSpread2.ActiveSheet.PrintInfo.ShowSubtitle = FarPoint.Win.Spread.PrintTitle.Hide;

            //시트 보호를 해지 한다.
            //fpSpread2.ActiveSheet.PrintInfo.PrintType = FarPoint.Win.Spread.PrintType.All;
            //fpSpread2.ActiveSheet.PrintInfo.SmartPrintRules.Add(new ReadOnlyAttribute(false));
            //axfpSpread1.Protect = false;


            //for (int i = 0; i < 24; i++) fpSpread2.ActiveSheet.SetColumnWidth(i, 80);

            //틀 고정
            fpSpread2.ActiveSheet.FrozenColumnCount = 3;
            fpSpread2.ActiveSheet.FrozenRowCount = 6;
            fpSpread2.ActiveSheet.Cells[1, 0].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[1, 0].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Right;
            fpSpread2.ActiveSheet.Cells[1, 0].Text = "날짜 :";
            fpSpread2.ActiveSheet.AddSpanCell(1, 1, 1, 22);

            fpSpread2.ActiveSheet.Cells[1, 1].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[1, 1].Text = DateTime.Now.ToLongDateString();
            fpSpread2.ActiveSheet.Cells[1, 1].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[1, 1].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Left;

            int Col;

            Col = 0;
            //No
            fpSpread2.ActiveSheet.SetRowHeight(3, 40);
            fpSpread2.ActiveSheet.SetRowHeight(4, 40);
            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 3, 1);
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[3, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[3, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].Text = "NO.";
            fpSpread2.ActiveSheet.Cells[3, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[3, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[3, Col].Border = LineBorderToHeader;
            Col++;

            //Time
            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 3, 1);
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 300);
            fpSpread2.ActiveSheet.Cells[3, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[3, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].Text = "생산 시간";
            fpSpread2.ActiveSheet.Cells[3, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[3, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[3, Col].Border = LineBorderToHeader;
            Col++;

            //바코드
            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 3, 1);
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 150);
            fpSpread2.ActiveSheet.Cells[3, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[3, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].Text = "바코드";
            fpSpread2.ActiveSheet.Cells[3, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[3, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[3, Col].Border = LineBorderToHeader;
            Col++;

            //판정
            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 1, 1);
            fpSpread2.ActiveSheet.Cells[3, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[3, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].Text = "판정";
            fpSpread2.ActiveSheet.Cells[3, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[3, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[3, Col].Border = LineBorderToHeader;

            Col++;

            
            //전류
            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 1, 8);
            fpSpread2.ActiveSheet.Cells[3, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[3, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].Text = "P/SEAT 전류 [A]";
            fpSpread2.ActiveSheet.Cells[3, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[3, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[3, Col].Border = LineBorderToHeader;

            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "RECLINE";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToHeader;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "스팩";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "데이타";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "RELAX";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToHeader;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "스팩";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "데이타";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "HEIGHT";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToHeader;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "스팩";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "데이타";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;

            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "LEGREST";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToHeader;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "스팩";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "데이타";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "LEGREST EXT";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToHeader;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "스팩";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "데이타";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;

            
            //소음
            fpSpread2.ActiveSheet.AddSpanCell(3, Col, 1, 8);
            fpSpread2.ActiveSheet.Cells[3, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[3, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[3, Col].Text = "소음[dB]";
            fpSpread2.ActiveSheet.Cells[3, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[3, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[3, Col].Border = LineBorderToHeader;

            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.AddSpanCell(4, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "RECLINE";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToData;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "기동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "구동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.AddSpanCell(4, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "RELAX";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToData;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "기동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "구동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.AddSpanCell(4, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "HEIGHT";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToData;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "기동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "구동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.AddSpanCell(4, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "LEGREST";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToData;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "기동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "구동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;

            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.AddSpanCell(4, Col, 1, 2);
            fpSpread2.ActiveSheet.Cells[4, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[4, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[4, Col].Text = "LEGREST EXT";
            fpSpread2.ActiveSheet.Cells[4, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[4, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[4, Col].Border = LineBorderToData;

            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "기동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            fpSpread2.ActiveSheet.SetColumnWidth(Col, 100);
            fpSpread2.ActiveSheet.Cells[5, Col].CellType = new FarPoint.Win.Spread.CellType.EditBaseCellType();
            fpSpread2.ActiveSheet.Cells[5, Col].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[5, Col].Text = "구동음";
            fpSpread2.ActiveSheet.Cells[5, Col].BackColor = Color.WhiteSmoke;
            fpSpread2.ActiveSheet.Cells[5, Col].ForeColor = Color.Black;
            fpSpread2.ActiveSheet.Cells[5, Col].Border = LineBorderToHeader;
            Col++;
            
            //Header
            fpSpread2.ActiveSheet.AddSpanCell(0, 0, 1, Col);
            fpSpread2.ActiveSheet.SetRowHeight(0, 100);
            fpSpread2.ActiveSheet.Cells[0, 0].Font = new Font("맑은 고딕", 26);
            fpSpread2.ActiveSheet.SetText(0, 0, "레포트");
            fpSpread2.ActiveSheet.Cells[0, 0].VerticalAlignment = FarPoint.Win.Spread.CellVerticalAlignment.Center;
            fpSpread2.ActiveSheet.Cells[0, 0].HorizontalAlignment = FarPoint.Win.Spread.CellHorizontalAlignment.Center;
            RowCount = 6;
            //fpSpread2.SaveExcel("d:\\dd.xls");
            return;
        }

        private void SaveInfor()
        {
            string Path = Program.INFOR_PATH.ToString() + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".inf";


            TIniFile Ini = new TIniFile(Path);

            Ini.WriteInteger("COUNT", "TOTAL", Infor.TotalCount);
            Ini.WriteInteger("COUNT", "OK", Infor.TotalCount - Infor.NgCount);
            Ini.WriteInteger("COUNT", "NG", Infor.NgCount);

            Ini.WriteString("NAME", "DATA", Infor.DataName);
            Ini.WriteString("NAME", "DATE", Infor.Date);
            return;
        }

        private void OpenInfor()
        {
            string Path = Program.INFOR_PATH.ToString() + "\\" + DateTime.Now.ToString("yyyyMMdd") + ".inf";

            if (File.Exists(Path) == false)
            {
                CreateFileName();
                return;
            }

            TIniFile Ini = new TIniFile(Path);

            if (Ini.ReadInteger("COUNT", "TOTAL", ref Infor.TotalCount) == false) Infor.TotalCount = 0;
            if (Ini.ReadInteger("COUNT", "OK", ref Infor.OkCount) == false) Infor.OkCount = 0;
            if (Ini.ReadInteger("COUNT", "NG", ref Infor.NgCount) == false) Infor.NgCount = 0;

            if (Ini.ReadString("NAME", "DATA", ref Infor.DataName) == false) Infor.DataName = "";
            if (Ini.ReadString("NAME", "DATE", ref Infor.Date) == false) Infor.Date = "";
            return;
        }
    }
    public class KALMAN_FILETER
    {
        private struct myKalmanFilterType
        {
            public float z_Din;
            public float Q;
            public float R;
            public float A;
            public float B_uk;
            public float H;
            public float x_Predict;
            public float Xk;
            public float p_Predict;
            public float Pk;
            public float K_gain;
        }

        private myKalmanFilterType[] KalmanStruct = new myKalmanFilterType[20];

        public KALMAN_FILETER()
        {

        }

        //public myKalmanFilterType[] myKalmanBuff = new myKalmanFilterType[5];
        //public myKalmanFilterType myKalmanBuff = new myKalmanFilterType();

        ~KALMAN_FILETER()
        {

        }

        //public const double Speed = 0.01;
        public const double Speed = 0.15;
        private void init_kalmanFiltering(short Ch, ref myKalmanFilterType[] kf)
        {
            //kf->Q = pow(0.01,2);


            kf[Ch].Q = (float)Math.Pow(Speed, 2);
            kf[Ch].R = (float)Math.Pow(0.5, 2);
            kf[Ch].A = 1;
            kf[Ch].B_uk = 0;
            kf[Ch].H = (float)1.0;
            kf[Ch].Xk = 0;     //25
            kf[Ch].Pk = (float)1.0;
            kf[Ch].K_gain = (float)1.0;

            return;
        }

        private void init_kalmanFiltering(ref myKalmanFilterType[] kf)
        {
            //kf->Q = pow(0.01,2);

            for (int i = 0; i < kf.Length; i++)
            {
                kf[i].Q = (float)Math.Pow(Speed, 2);
                kf[i].R = (float)Math.Pow(0.5, 2);
                kf[i].A = 1;
                kf[i].B_uk = 0;
                kf[i].H = (float)1.0;
                kf[i].Xk = 0;     //25
                kf[i].Pk = (float)1.0;
                kf[i].K_gain = (float)1.0;
            }
            return;
        }

        private float kalmanFilter(ref myKalmanFilterType kf)
        {

            kf.x_Predict = (kf.A * kf.Xk) + kf.B_uk;

            kf.p_Predict = (kf.A * kf.Pk) + kf.Q;

            kf.K_gain = kf.p_Predict / (kf.H * kf.p_Predict + kf.R);

            kf.Xk = kf.x_Predict + kf.K_gain * (kf.z_Din - kf.x_Predict);

            kf.Pk = (1 - kf.K_gain) * kf.p_Predict;

            return kf.Xk;

        }

        private float kalmanFilter_(short Ch, float getdata, ref myKalmanFilterType myKalmanBuff)
        {
            myKalmanBuff.z_Din = getdata;

            return kalmanFilter(ref myKalmanBuff);
        }

        //초기화 함수
        private void init_kalmanFilter(ref myKalmanFilterType[] myKalmanBuff)
        {
            //init_kalmanFiltering(ref myKalmanBuff[0]);
            //init_kalmanFiltering(ref myKalmanBuff[1]);
            //init_kalmanFiltering(ref myKalmanBuff[2]);
            //init_kalmanFiltering(ref myKalmanBuff[3]);
            //init_kalmanFiltering(ref myKalmanBuff[4]);
            init_kalmanFiltering(ref myKalmanBuff);
            return;
        }
        private void init_kalmanFilter(short Ch, ref myKalmanFilterType[] myKalmanBuff)
        {
            //init_kalmanFiltering(ref myKalmanBuff[0]);
            //init_kalmanFiltering(ref myKalmanBuff[1]);
            //init_kalmanFiltering(ref myKalmanBuff[2]);
            //init_kalmanFiltering(ref myKalmanBuff[3]);
            //init_kalmanFiltering(ref myKalmanBuff[4]);
            init_kalmanFiltering(Ch, ref myKalmanBuff);
            return;
        }
        public void InitAll()
        {
            init_kalmanFilter(ref KalmanStruct);
            return;
        }

        public void Init(short Ch)
        {
            init_kalmanFilter(Ch, ref KalmanStruct);
            return;
        }

        public float CheckData(short Ch, float Data)
        {
            float rData = kalmanFilter_(Ch, Data, ref KalmanStruct[Ch]);
            rData = KalmanStruct[Ch].Xk;
            return rData;
            //return Data;
        }
    }
}

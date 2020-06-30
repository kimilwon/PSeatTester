using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSeatTester
{
    public partial class SelfTest : Form
    {
        private MyInterface mControl = null;
        public SelfTest()
        {
            InitializeComponent();
        }
        public SelfTest(MyInterface mControl)
        {
            InitializeComponent();
            this.mControl = mControl;
        }

        
        private void switchLever1_ValueChanged(object sender, Iocomp.Classes.ValueBooleanEventArgs e)
        {
            ledBulb70.On = e.ValueNew;

            if (e.ValueNew == true)
                mControl.GetPower.POWER_PWON();
            else mControl.GetPower.POWER_PWOFF();

            return;
        }

        private bool TextChange { get; set; }
        private bool PowerChnageFlag { get; set; }
        private bool PowerTextChange { get; set; }
        private long PowerChangeTimeToFirst { get; set; }

        private void knob1_ValueChanged(object sender, Iocomp.Classes.ValueDoubleEventArgs e)
        {
            if (TextChange == true) return;
            string Value = e.ValueNew.ToString("0.00") + " [V]";

            if (textBox1.Text != Value) textBox1.Text = Value;
            if (PowerTextChange == true) return;

            PowerChangeTimeToFirst = mControl.공용함수.timeGetTimems();
            PowerChnageFlag = true;
            return;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = false;

                if (PowerChnageFlag == true)
                {
                    if (100 <= (mControl.공용함수.timeGetTimems() - PowerChangeTimeToFirst))
                    {
                        PowerChnageFlag = false;
                        mControl.GetPower.POWER_PWSetting((float)knob1.Value.AsDouble);
                    }
                }

                DisplayIOIn();
                DisplayAD();
            }
            catch { }
            finally { timer1.Enabled = !mControl.isExit; }
        }

        private void DisplayIOIn()
        {
            if (ledBulb1.On != mControl.GetIO.GetPinConnectSw) ledBulb1.On = mControl.GetIO.GetPinConnectSw;
            if (ledBulb6.On != mControl.GetIO.GetPinConnectFwd) ledBulb6.On = mControl.GetIO.GetPinConnectFwd;
            if (ledBulb7.On != mControl.GetIO.GetPinConnectBwd) ledBulb7.On = mControl.GetIO.GetPinConnectBwd;

            if (ledBulb2.On != mControl.GetIO.GetStartSw) ledBulb2.On = mControl.GetIO.GetStartSw;
            if (ledBulb3.On != mControl.GetIO.GetResetSw) ledBulb3.On = mControl.GetIO.GetResetSw;
            if (ledBulb4.On != mControl.GetIO.GetRHSelect) ledBulb4.On = mControl.GetIO.GetRHSelect;
            if (ledBulb5.On != mControl.GetIO.GetSeatRelax) ledBulb5.On = mControl.GetIO.GetSeatRelax;
            if (ledBulb12.On != mControl.GetIO.GetAuto) ledBulb12.On = mControl.GetIO.GetAuto;
            if (ledBulb16.On != mControl.GetIO.GetProductIn) ledBulb16.On = mControl.GetIO.GetProductIn;
            if (ledBulb17.On != mControl.GetIO.GetJigUp) ledBulb17.On = mControl.GetIO.GetJigUp;

            if (ledBulb18.On != mControl.GetIO.GetRelax_Relax) ledBulb18.On = mControl.GetIO.GetRelax_Relax;
            if (ledBulb19.On != mControl.GetIO.GetRelaxReturn) ledBulb19.On = mControl.GetIO.GetRelaxReturn;
            if (ledBulb20.On != mControl.GetIO.GetReclinerFwd) ledBulb20.On = mControl.GetIO.GetReclinerFwd;
            if (ledBulb21.On != mControl.GetIO.GetReclinerBwd) ledBulb21.On = mControl.GetIO.GetReclinerBwd;
            if (ledBulb22.On != mControl.GetIO.GetLegrest_Rest) ledBulb22.On = mControl.GetIO.GetLegrest_Rest;
            if (ledBulb23.On != mControl.GetIO.GetLegrest_Return) ledBulb23.On = mControl.GetIO.GetLegrest_Return;
            if (ledBulb24.On != mControl.GetIO.GetHeightUp) ledBulb24.On = mControl.GetIO.GetHeightUp;
            if (ledBulb25.On != mControl.GetIO.GetHeightDn) ledBulb25.On = mControl.GetIO.GetHeightDn;
            if (ledBulb26.On != mControl.GetIO.GetLegrestExt) ledBulb26.On = mControl.GetIO.GetLegrestExt;
            if (ledBulb27.On != mControl.GetIO.GetLegrestExtReturn) ledBulb27.On = mControl.GetIO.GetLegrestExtReturn;
            if (ledBulb28.On != mControl.GetIO.GetTestSetMode) ledBulb28.On = mControl.GetIO.GetTestSetMode;
            if (ledBulb8.On != mControl.GetIO.GetRecline납입센서) ledBulb8.On = mControl.GetIO.GetRecline납입센서;

            return;
        }
        private void DisplayAD()
        {
            
            plot1.Channels[0].AddXY(plot1.Channels[0].Count, mControl.GetPMeter.GetPSeat);
            plot2.Channels[0].AddXY(plot1.Channels[0].Count, mControl.GetSound.GetSound);

            if (sevenSegmentAnalog2.Value.AsDouble != mControl.GetPMeter.GetBatt) sevenSegmentAnalog2.Value.AsDouble = mControl.GetPMeter.GetBatt;
            if (sevenSegmentAnalog5.Value.AsDouble != mControl.GetPMeter.GetPSeat) sevenSegmentAnalog5.Value.AsDouble = mControl.GetPMeter.GetPSeat;
            if (sevenSegmentAnalog1.Value.AsDouble != mControl.GetSound.GetSound) sevenSegmentAnalog1.Value.AsDouble = mControl.GetSound.GetSound;
            return;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {            
            if(e.KeyChar == (char)ConsoleKey.Enter)
            {
                TextChange = true;
                string s = textBox1.Text;

                if(0 <= s.IndexOf("["))
                {
                    s = s.Substring(0, s.IndexOf("[") - 1);
                }
                float Volt;

                if (float.TryParse(s, out Volt) == false) Volt = -1;
                if(Volt != -1) mControl.GetPower.POWER_PWSetting(Volt);
                knob1.Value.AsDouble = Volt;
                TextChange = false;
            }
            return;
        }

        private void SelfTest_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            return;
        }

        private void imageButton1_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Red;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.ProductInOut = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton2_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Red;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.RedLampOnOff = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton3_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Yellow;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.YellowLampOnOff = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton4_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Lime;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.GreenLampOnOff = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton5_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Red;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.TestINGOnOff = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton6_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Lime;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.TestOKOnOff = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton7_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Red;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.SetJigDownOnOff = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton8_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Red;
            else But.ButtonColor = Color.Black;

            if (But.ButtonColor == Color.Black)
            {
                knob1.Value.AsDouble = 0;
                switchLever1.Value.AsBoolean = false;

                imageButton9.ButtonColor = Color.Black;
                imageButton10.ButtonColor = Color.Black;
                mControl.공용함수.timedelay(100);
                mControl.GetIO.SetIgn1 = false;
                mControl.GetIO.SetIgn2 = false;
                mControl.공용함수.timedelay(100);
                mControl.GetIO.SetPSeatBatt = false;
            }
            else
            {
                knob1.Value.AsDouble = 13.6;
                switchLever1.Value.AsBoolean = true;

                imageButton9.ButtonColor = Color.Red;
                imageButton10.ButtonColor = Color.Red;
                mControl.공용함수.timedelay(100);
                mControl.GetIO.SetPSeatBatt = true;
                mControl.공용함수.timedelay(100);
                mControl.GetIO.SetIgn1 = true;
                mControl.GetIO.SetIgn2 = true;
            }
            return;
        }

        private void imageButton9_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Red;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.SetIgn1 = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton10_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Red;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.SetIgn1 = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton12_MouseDown(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Red;

            //mControl.GetIO.SetReclineFwd = true;
            mControl.C_ReclineFwdOnOff = true;
            return;
        }

        private void imageButton12_MouseUp(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Black;
            //mControl.GetIO.SetReclineFwd = false;
            mControl.C_ReclineFwdOnOff = false;
            return;
        }

        private void imageButton11_MouseDown(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Red;

            //mControl.GetIO.SetReclineBwd = true;
            mControl.C_ReclineBwdOnOff = true;
            return;
        }

        private void imageButton11_MouseUp(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Black;
            //mControl.GetIO.SetReclineBwd = false;
            mControl.C_ReclineBwdOnOff = false;
            return;
        }

        //private void imageButton16_MouseDown(object sender, MouseEventArgs e)
        //{
        //    UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
        //    But.ButtonColor = Color.Red;

        //    mControl.GetIO.SetHeightUp = true;
        //    return;
        //}

        //private void imageButton16_MouseUp(object sender, MouseEventArgs e)
        //{
        //    UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
        //    But.ButtonColor = Color.Black;
        //    mControl.GetIO.SetHeightUp = false;
        //    return;
        //}

        //private void imageButton15_MouseDown(object sender, MouseEventArgs e)
        //{
        //    UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
        //    But.ButtonColor = Color.Red;

        //    mControl.GetIO.SetHeightDown = true;
        //    return;
        //}

        //private void imageButton15_MouseUp(object sender, MouseEventArgs e)
        //{
        //    UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
        //    But.ButtonColor = Color.Black;
        //    mControl.GetIO.SetHeightDown = false;
        //    return;
        //}

        private void imageButton14_MouseDown(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Red;
            //mControl.GetIO.SetRelax = true;
            mControl.C_RelaxUpOnOff = true;
            return;
        }

        private void imageButton14_MouseUp(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Black;
            //mControl.GetIO.SetRelax = false;
            mControl.C_RelaxUpOnOff = false;
            return;
        }

        private void imageButton13_MouseDown(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Red;
            //mControl.GetIO.SetRelaxReturn = true;
            mControl.C_RelaxDnOnOff = true;
            return;
        }

        private void imageButton13_MouseUp(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Black;
            //mControl.GetIO.SetRelaxReturn = false;
            mControl.C_RelaxDnOnOff = false;
            return;
        }

        private void imageButton18_MouseDown(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Red;
            //mControl.GetIO.SetLegrest = true;
            mControl.C_LegrestUpOnOff = true;
            return;
        }

        private void imageButton18_MouseUp(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Black;
            //mControl.GetIO.SetLegrest = false;
            mControl.C_LegrestUpOnOff = false;
            return;
        }

        private void imageButton17_MouseDown(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Red;
            //mControl.GetIO.SetLegrestReturn = true;
            mControl.C_LegrestDnOnOff = true;
            return;
        }

        private void imageButton17_MouseUp(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Black;
            //mControl.GetIO.SetLegrestReturn = false;
            mControl.C_LegrestDnOnOff = false;
            return;
        }

        private void imageButton20_MouseDown(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Red;
            //mControl.GetIO.SetLegrestExt = true;
            mControl.C_LegrestExtUpOnOff = true;
            return;
        }

        private void imageButton20_MouseUp(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Black;
            //mControl.GetIO.SetLegrestExt = false;
            mControl.C_LegrestExtUpOnOff = false;
            return;
        }

        private void imageButton19_MouseDown(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Red;
            //mControl.GetIO.SetLegrestExtReturn = true;
            mControl.C_LegrestExtDnOnOff = true;
            return;
        }

        private void imageButton19_MouseUp(object sender, MouseEventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;
            But.ButtonColor = Color.Black;
            //mControl.GetIO.SetLegrestExtReturn = false;
            mControl.C_LegrestExtDnOnOff = false;
            return;
        }

        private void imageButton21_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Red;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.BuzzerOnOff = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }

        private void imageButton22_Click(object sender, EventArgs e)
        {
            UserImageButton.ImageButton But = sender as UserImageButton.ImageButton;

            if (But.ButtonColor == Color.Black)
                But.ButtonColor = Color.Red;
            else But.ButtonColor = Color.Black;

            mControl.GetIO.PinConnectionOnOff = (But.ButtonColor == Color.Black) ? false : true;
            return;
        }
    }
}

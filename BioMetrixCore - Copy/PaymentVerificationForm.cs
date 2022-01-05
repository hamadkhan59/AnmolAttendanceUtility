using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BioMetrixCore
{
    public partial class PaymentVerificationForm : Form
    {
        private System.Windows.Forms.Timer tmr;

        public PaymentVerificationForm(string staffId, string staffName, string message)
        {
            InitializeComponent();
            this.lblStaffIdValue.Text = staffId.PadLeft(5, '0');
            this.lblTime.Text = message;
            GetStaffImage(staffId);
            GetDefaultImage();
            string FormInterval = ConfigurationManager.AppSettings["FormInterval"].ToString();
            int formInterval = int.Parse(FormInterval);

            tmr = new System.Windows.Forms.Timer();
            tmr.Tick += delegate {
                this.Close();
            };
            tmr.Interval = (int)TimeSpan.FromSeconds(formInterval).TotalMilliseconds;
            tmr.Start();
        }

        public void GetStaffImage(string staffId)
        {
            string picfilelocation = ConfigurationManager.AppSettings["picfilelocation"].ToString();
            string pngFile = picfilelocation + staffId + ".png";
            string jpgFile = picfilelocation + staffId + ".jpg";
            string jpegFile = picfilelocation + staffId + ".jpeg";
            string defaultFile = picfilelocation + "default.png";
            string FileName = "";

            if (File.Exists(pngFile))
                FileName = pngFile;
            else if (File.Exists(jpgFile))
                FileName = jpgFile;
            else if (File.Exists(jpegFile))
                FileName = jpegFile;
            else if (File.Exists(defaultFile))
                FileName = defaultFile;

            if (FileName.Length > 0)
            {
                Bitmap image = new Bitmap(FileName);
                pictureBox1.Image = image;
            }
        }

        public void GetDefaultImage()
        {
            string picfilelocation = ConfigurationManager.AppSettings["picfilelocation"].ToString();
            string defaultFile = picfilelocation + "default.png";
            string FileName = defaultFile;

            if (FileName.Length > 0)
            {
                Bitmap image = new Bitmap(FileName);
                pictureBox2.Image = image;
            }
        }

    }
}

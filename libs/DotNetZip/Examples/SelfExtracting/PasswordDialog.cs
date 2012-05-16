namespace Ionic.Utils.Zip
{
    using System;
using System.Collections.Generic;
using System.Windows.Forms;

    public partial class PasswordDialog : Form
    {
        public PasswordDialog()
        {
            InitializeComponent();
        }
        public string EntryName
        {
            set
            {
                prompt.Text = "Enter the password for " + value;
            }
        }
        public string Password
        {
            get
            {
                return textBox1.Text;
            }
        }
    }
}

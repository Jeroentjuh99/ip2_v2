using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArtsApp
{
    public partial class LoginWindow : Form
    {
        private Form1 parent;

        

        public LoginWindow(Form1 parent)
        {
            InitializeComponent();
            this.parent = parent;

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        public void setCallback(Action a)
        {

        }


        public String returnUsername()
        {
            return username.Text;
        }

        public String returnPassWord()
        {
            return password.Text;
        }

        private void Login_Click(object sender, EventArgs e)
        {
            parent.setLoginInfo(returnUsername(), returnPassWord());
            this.Close();
        }

        private void password_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

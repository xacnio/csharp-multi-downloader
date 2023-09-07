using System;
using System.Windows.Forms;

namespace MultiDownload
{
    public partial class Form2 : Form
    {
        Database db = new Database(Utils.GetDatabaseDirectory() + "database.db");

        public Form2()
        {
            InitializeComponent();
            db.CreateIfNotExist();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var username = textBox1.Text.Trim();
            var password = textBox2.Text;
            if (username == "" || password == "")
            {
                showMessage("Tüm alanları doldurun!");
                return;
            }
            var passwordHashed = Utils.HashPassword(password);
            if(db.CreateUser(username, passwordHashed))
            {
                Form1 form1 = new Form1(this);
                form1.Show();
                this.Hide();
            }
            else
            {
                showMessage("Başka bir kullanıcı adı girin!");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var username = textBox1.Text.Trim();
            var password = textBox2.Text;
            if (username == "" || password == "")
            {
                showMessage("Tüm alanları doldurun!");
                return;
            }
            var passwordHashed = Utils.HashPassword(password);
            if (db.CheckUser(username, passwordHashed))
            { 
                Form1 form1 = new Form1(this);
                form1.Show();
                this.Hide();
            }
            else
            {
                showMessage("Geçersiz kullanıcı veya şifre!");
            }
        }

        void showMessage(String msg)
        {
            label3.Text = msg;
            label3.Visible = true;
        }
    }
}

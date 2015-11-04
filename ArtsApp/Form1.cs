using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ArtsApp
{
    public partial class Form1 : Form
    {
        private SerialPort port;
        private bool LogInc;
        private delegate void SetTextCallback(TextBox txt,string text);
        private TcpClient connection;
        private string currentRead="";
        private delegate void SetTextDeleg(string data);

        private List<Patient> patients;

        private String USERNAME, PASSWORD;
        private string selected;

        private delegate void addPatient(ComboBox box,String test);

        public Form1()
        {
            InitializeComponent();
         
            listCommands();
            patients = new List<Patient>();
            selected = "";
            Test();
        }

        public void ListPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            foreach (string s in ports)
            {
                allClients.Items.Add(s);
            }
        }

        public void listCommands()
        {
            string[] commands = new string[] { "Tijd", "Afstand", "Power", "KJ", "Reset"   };
            foreach(String s in commands)
            {
                comboBox2.Items.Add(s);
            }
        }


        private void setPort(string portName)
        {
            try
            {
                port = new SerialPort(portName);

                port.BaudRate = 9600;
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.DataBits = 8;
                port.Handshake = Handshake.None;
                port.ReadTimeout = 2000;
                port.WriteTimeout = 500;

                port.DtrEnable = true;
                port.RtsEnable = true;

                port.Open();

                port.DataReceived += DataReceivedHandler;
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (o, args) =>
                {
                    while (true)
                    {
                       // sendMessage("ST");
                        if(connection!=null)
                        {
                           // WriteTextMessage(connection, "01-" + currentRead);
                          
                        }
                        Thread.Sleep(1000);
                    }
                };
                worker.RunWorkerAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine("woops");
            }

        }

        private void sendMessage(string s)
        {
            if (port != null && port.IsOpen)
            {
                port.WriteLine(s);
            }
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            Thread.Sleep(500);
            if (port.IsOpen)
            {
                string indata = sp.ReadExisting();
                string[] search = new string[] { "ACK", "ERROR", "RUN" };

                foreach (string s in search)
                {
                    indata = indata.Replace(s, "");
                }

                string[] data = indata.Split('\n');
                if (data.Length > 2)
                    UpdateStatus(data[1]);
                else
                    UpdateStatus(data[0]);
            }
        }

        public void UpdateStatus(String data)
        {
            data = data.Replace("\r", "");
            currentRead = data;
            String[] values = data.Split('\t');
            try
            {
                updateField(textBox4, values[1]);
                updateField(textBox5, values[2]);
                updateField(textBox2, values[3]);
                updateField(textBox3, values[4]);
                updateField(textBox6, values[5]);
                updateField(textBox1, values[6]);
            }
            catch(Exception e)
            {
                
            }

           // textBox4.Text = values[1];
            //textBox5.Text = values[2];
            //textBox2.Text = values[3];
            //textBox3.Text = values[4];
            //textBox6.Text = values[5];
            //textBox1.Text = values[6];


        }

        private void sendMultipleMessages(string[] s)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, args) =>
            {
                foreach (string str in s)
                {
                    sendMessage(str);
                    Thread.Sleep(500);
                }
            };
            worker.RunWorkerAsync();

        }


        private void updateField(TextBox txt, String data)
        {
            if (txt.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(updateField);
                Invoke(d,txt,data );
            }
            else
            {
                txt.Text = data;
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //"Tijd", "Afstand", "Power", "RPM", "KJ", "Snelheid"
            switch (comboBox2.SelectedItem.ToString())
            {
                case "Tijd":
                    //sendMultipleMessages(new string[] { "CM", "PT " + textBox8.Text });
                    WriteTextMessage(connection, "08" + allClients.SelectedItem.ToString() + ":CM PT " + textBox8.Text);
                    break;
                case "Afstand":
                    //sendMultipleMessages(new string[] { "CM", "PD  " + Convert.ToDouble(textBox8.Text) * 10 });
                    WriteTextMessage(connection, "08" + allClients.SelectedItem.ToString() + ":CM PD " + Convert.ToDouble(textBox8.Text) * 10);
                    break;

                case "Power":
                    //sendMultipleMessages(new string[] { "CM", "PW " + textBox8.Text });
                    WriteTextMessage(connection, "08" + allClients.SelectedItem.ToString() + ":CM PW " + textBox8.Text);
                    break;

                case "KJ":
                    //sendMultipleMessages(new string[] { "CM", "PE  " + textBox8.Text });
                    WriteTextMessage(connection, "08" + allClients.SelectedItem.ToString() + ":CM PE " + textBox8.Text);
                    break;
                case "Reset":
                    WriteTextMessage(connection, "08" + allClients.SelectedItem.ToString() + ":RS");
                    break;

            }

            
        }

        private void ConnectServer_Click(object sender, EventArgs e)
        {
            IPAddress host;
            bool check = IPAddress.TryParse(textBox7.Text, out host);

            if(check)
            {
                connection = new TcpClient(host.ToString(), 1338);
               // WriteTextMessage(connection, "00-" + username.Text); NEEDS TO BE FIXED DUE TO SHOW DIALOG
            }


        }

        private void ConnectBike_Click(object sender, EventArgs e)
        {
            setPort(allClients.Text);
        }

        private void Reset_Click(object sender, EventArgs e)
        {
            sendMessage("RS");
        }

        private void WriteTextMessage(TcpClient client, string message)
        {
            StreamWriter stream = new StreamWriter(client.GetStream(), Encoding.ASCII);
            stream.WriteLine(message);
            stream.Flush();
        }

        private String ReadTextMessage(TcpClient client)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            string[] lines = (string[])formatter.Deserialize(client.GetStream());
            string line = "";
            if (lines.Length == 1)
            {
                line = lines[0];
            }
            else
            {
                foreach(String e in lines)
                {
                    Invoke(new SetTextDeleg(DisplayToUI), new object[] { e + Environment.NewLine });
                }
            }


            return line;
        }

        private void LoadLog_Click(object sender, EventArgs e)
        {
            if(connection!=null)
            {
                LogShow.ResetText();
                WriteTextMessage(connection, "02" + LogName.Text);
                
               
            }
        }

        private void DisplayToUI(string displayData)
        {
            LogShow.AppendText(displayData);
            LogShow.ScrollToCaret();

        }

        private void Disconnect_Click(object sender, EventArgs e)
        {
            WriteTextMessage(connection,"03");
            connection.Close();
            connection = null;
        }

        private void sendmsg_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void Connection()
        {
            while (true)
            {
                HandleMessages(ReadTextMessage(connection));
            }
        }

        private void HandleMessages(string data)
        {
            if (data.Length > 1)
            {
                switch (data.Substring(0, 2))
                {
                    case "01": DataFromClient(data.Substring(2)); break;
                    case "02": ReceiveLogData(data.Substring(2)); break;
                    case "04": break;
                    case "05": NewPatient(data.Substring(2)); break;
                    case "07": CheckLogin(data.Substring(2)); break;
                    default: break;
                }
            }
        }

        private void ReceiveLogData(string data)
        {
            Invoke(new SetTextDeleg(DisplayToUI), new object[] { data + Environment.NewLine });
            LogInc = true;
        }


        private void NewPatient(string data)
        {
            bool isAdded = false;
            foreach(Patient p in patients)
            {
                if (p.username == data)
                {
                    isAdded = true;
                }
            }

            if (!isAdded)
            {
                Patient p = new Patient(data);
                patients.Add(p);
                UpdateAllClients(allClients,p.username);
                UpdateAllClients(raceSel1, p.username);
                UpdateAllClients(raceSel2, p.username);
            }

        }

        private void UpdateAllClients(ComboBox box , string text)
        {
            if (box.InvokeRequired)
            {
                Invoke(new addPatient(UpdateAllClients),box, text);
            } else
            {
                box.Items.Add(text);
            }
            
        }

        private void CheckLogin(string data)
        {
            if (data.Contains("Failed"))
            {
                showLoginDialog();
            }
        }

        private void V(string data)
        {

        }

        private void DataFromClient(string data)
        {
            string[] splitData = data.Split(',');
            foreach(Patient p in patients)
            {
                if (p.username == splitData[0])
                {
                    p.currentBikeData = splitData[1];

                    if (p.username == selected)
                    {
                        UpdateStatus(p.currentBikeData);
                    }
                }
            }
        }

        private void Login_Click(object sender, EventArgs e)
        {
            showLoginDialog();
        }

        private void allClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            selected = allClients.SelectedItem.ToString();

            foreach(Patient p in patients)
            {
                if (p.username == selected)
                {
                    String[] values = p.currentBikeData.Split('\t');
                    try
                    {
                        updateField(textBox4, values[1]);
                        updateField(textBox5, values[2]);
                        updateField(textBox2, values[3]);
                        updateField(textBox3, values[4]);
                        updateField(textBox6, values[5]);
                        updateField(textBox1, values[6]);
                    }
                    catch
                    {

                    }

                }
            }
        }

        public void setLoginInfo(string usr, string pw)
        {
            USERNAME = usr;
            PASSWORD = pw;

        }


        private void Race_Click(object sender, EventArgs e)
        {
            WriteTextMessage(connection, "06" + raceSel1.SelectedItem.ToString() + ":" + raceSel2.SelectedItem.ToString());
        }
        public void makeKeyPair()
        {
            var csp = new RSACryptoServiceProvider(2048);
            var privKey = csp.ExportParameters(true);
            var pubKey = csp.ExportParameters(false);

            string pubKeyString;
            {
                var sw = new System.IO.StringWriter();
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                xs.Serialize(sw, pubKey);
                pubKeyString = sw.ToString();
            }

            string privKeyString;
            {
                var sw = new System.IO.StringWriter();
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                xs.Serialize(sw, privKey);
                privKeyString = sw.ToString();
            }

            using (StreamWriter sw = new StreamWriter("pub.key"))
            {
                sw.WriteLine(pubKeyString);
                sw.Flush();
            }

            using (StreamWriter sw = new StreamWriter("priv.key"))
            {
                sw.WriteLine(privKeyString);
                sw.Flush();
            }
        }

        public string Encrypt(string toEncrypt)
        {
            string retval;

            var csp = new RSACryptoServiceProvider();

            RSAParameters pubKey;
            using (StreamReader sr = new StreamReader("pub.key"))
            {
                string readdata = sr.ReadLine();
                var stringReader = new System.IO.StringReader(readdata);
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                pubKey = (RSAParameters)xs.Deserialize(sr);
            }
            csp.ImportParameters(pubKey);

            var bytesPlainTextData = Encoding.Unicode.GetBytes(toEncrypt);

            var bytesCypherText = csp.Encrypt(bytesPlainTextData, false);

            var cypherText = Convert.ToBase64String(bytesCypherText);
            

            return cypherText;
        }

        public string Decrypt(string toDecrypt)
        {
            var csp = new RSACryptoServiceProvider();

            RSAParameters privKey;
            using (StreamReader sr = new StreamReader("priv.key"))
            {
                string readdata = sr.ReadLine();
                var stringReader = new System.IO.StringReader(readdata);
                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                privKey = (RSAParameters)xs.Deserialize(sr);
            }

            var bytesCypherText = Convert.FromBase64String(toDecrypt);

            csp = new RSACryptoServiceProvider();
            csp.ImportParameters(privKey);

            var bytesPlainTextData = csp.Decrypt(bytesCypherText, false);

            return System.Text.Encoding.Unicode.GetString(bytesPlainTextData);
        }

        public void Test()
        {
            Console.WriteLine("Testing  ");
            makeKeyPair();
            string encrypted = Encrypt("hallo");

            Console.WriteLine(encrypted);
            Console.WriteLine(Decrypt(encrypted));
        }

        private void AstrandButton_Click(object sender, EventArgs e)
        {

        }

        private void showLoginDialog()
        {
            LoginWindow test = new LoginWindow(this);

            test.ShowDialog();
            
            IPAddress host;
            bool check = IPAddress.TryParse(textBox7.Text, out host);
            
            if (check)
            {
                if (connection == null)
                {
                    connection = new TcpClient(host.ToString(), 1338);
                    Thread con = new Thread(new ThreadStart(Connection));
                    con.Start();
                }
                WriteTextMessage(connection, "07" + USERNAME + ":" + PASSWORD);
            }
        }
    }
}

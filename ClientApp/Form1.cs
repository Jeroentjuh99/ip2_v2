using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace ClientApp
{
    public partial class Form1 : Form
    {
        private SerialPort port;
        private delegate void SetTextCallback(TextBox txt,string text);
        private TcpClient connection;
        private string currentRead="";
        private delegate void SetTextDeleg(RichTextBox box,string data);
        private delegate void SetRaceInfo(RichTextBox box, string[] values);
        private int rpm = 0;

        public Form1()
        {
            InitializeComponent();
            ListPorts();
           // listCommands();
        }

        public void ListPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            foreach (string s in ports)
            {
                comboBox1.Items.Add(s);
            }
        }

        //public void listCommands()
        //{
        //    string[] commands = new string[] { "Tijd", "Afstand", "Power", "KJ"   };
        //    foreach(String s in commands)
        //    {
        //        comboBox2.Items.Add(s);
        //    }
        //}


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
                System.Windows.Forms.MessageBox.Show("Succesfull connected to bike on port " + port.PortName);
                port.DataReceived += DataReceivedHandler;
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (o, args) =>
                {
                    while (true)
                    {
                        sendMessage("ST");
                        if(connection!=null)
                        {
                            WriteTextMessage(connection, "01" + currentRead);
                        }
                        Thread.Sleep(1000);
                    }
                };
                worker.RunWorkerAsync();

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Something went wrong, please try again " + System.Environment.NewLine + ex.ToString(), "Whoops! ",MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            //System.Windows.Forms.MessageBox.Show(currentRead);
            String[] values = data.Split('\t');
            try
            {
                updateField(PulseBox, values[0]);
                updateField(textBox4, values[1]);
                updateField(textBox5, ""+ Math.Round((int.Parse(values[2]) / 10 / 3.6), 2)+ " m/s");
                updateField(textBox2, "" + (int.Parse(values[3])/10.0) + " km");
                updateField(textBox3, values[4] + " W");
                updateField(textBox6, values[5]);
                updateField(textBox1, values[6]);
                Int32.TryParse(values[1],out rpm);
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
        private void setLabelStatus(string status, Color color)
        {
            label_status.Visible = true;
            label_status.Text = status;
            label_status.ForeColor = color;
        }

        private void handleChatMessage(string v)
        {
 
            Invoke(new SetTextDeleg(updateChat), chatBox, "Doctor: "+v);           
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

        //private void button3_Click(object sender, EventArgs e)
        //{
        //    //"Tijd", "Afstand", "Power", "RPM", "KJ", "Snelheid"
        //    switch (comboBox2.SelectedItem.ToString())
        //    {
        //        case "Tijd":
        //            sendMultipleMessages(new string[] { "CM", "PT " + textBox8.Text });
        //            break;
        //        case "Afstand":
        //            sendMultipleMessages(new string[] { "CM", "PD  " + Convert.ToDouble(textBox8.Text) * 10 });
        //            break;

        //        case "Power":
        //            sendMultipleMessages(new string[] { "CM", "PW " + textBox8.Text });
        //            break;

        //        case "KJ":
        //            sendMultipleMessages(new string[] { "CM", "PE  " + textBox8.Text });
        //            break;


        //    }
        //}

        private void updateChat(RichTextBox box, String value)
        {
            box.AppendText(value + Environment.NewLine);
            box.ScrollToCaret();
        }

        private void guus(String data)
        {
            //System.Windows.Forms.MessageBox.Show(data);
            data = data.Replace("CM ", "");
            //updateField(textBox6, data.Substring(3));
            switch (data.Substring(0,2))
            {
                case "PT":
                    sendMultipleMessages(new string[] { "CM", "PT " + data.Substring(3) });
                    break;
                case "PD":
                    //sendMessage("RS");
                    sendMultipleMessages(new string[] { "CM", "PD " + data.Substring(3) });
                    break;
                case "PW":
                    //sendMessage("RS");
                    sendMultipleMessages(new string[] { "CM", "PW " + data.Substring(3) });
                    break;
                case "PE":
                    //sendMessage("RS");
                    sendMultipleMessages(new string[] { "CM", "PE " + data.Substring(3) });
                    break;
                case "RS":
                    sendMessage("RS");
                    break;

            }

            
        }

        private void ConnectServer_Click(object sender, EventArgs e)
        {
            IPAddress host;
            bool check = IPAddress.TryParse(textBox7.Text, out host);

            if(check)
            {
                try
                {
                    connection = new TcpClient(host.ToString(), 1338);
                    WriteTextMessage(connection, "00" + username.Text);
                    Thread con = new Thread(new ThreadStart(Client));
                    con.Start();
                }catch(Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("something went wrong when trying to start astrand test. \n Make sure you are connected to the server and selected a client", "Whoops! ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }



            }
        }

        private void Client()
        {
            while (true)
            {
                HandleMessages(ReadTextMessage(connection));
            }
        }

        private Timer timer;
        private int timerstate, teststate, wattage;
        private List<int> pulses;

        private void Astrand()
        {
            //dit werkt nog
            //System.Windows.Forms.MessageBox.Show("Test");
            //08heeftmisschiennogclientnaamnodig:CM PW hoeveelheid

            sendMessage("RS");
            this.pulses = new List<int>();
            timer = new Timer();
            timer.Enabled = true;
            timer.Interval = 1000;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timerstate = 0;
            teststate = 0;
            guus("CM PW 50");
            handleChatMessage("Warming-up is gestart");
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string name = username.Text;
            timerstate++;
            if (teststate == 0 && timerstate >= 30)
            {
                try
                {
                    if (int.Parse(PulseBox.Text) <= 120)
                    {
                        string[] a = textBox3.Text.Split(' ');
                        int power = int.Parse(a[0]);
                        if (name.Contains("(m)"))
                            guus("CM PW " + (power + 50));
                        else
                            guus("CM PW " + (power + 25));
                        timerstate = 0;
                    }
                    else
                    {
                        handleChatMessage("Test is gestart");
                        teststate++;
                        timerstate = 0;
                    }
                }
                catch
                {
                    PulseBox.Text = "125";
                    handleChatMessage("Test is gestart");
                    teststate++;
                    timerstate = 0;

                }

            }
            else if (teststate == 1 && timerstate >= 1*60)
            {
                string[] b = textBox3.Text.Split(' ');
                wattage = int.Parse(b[0]);
                teststate++;
                timerstate = 0;
                guus("CM PW 50");
                handleChatMessage("Cool-down is gestart");
            }
            else if (teststate == 1 && timerstate < 1*60)
            {
                int f = int.Parse(PulseBox.Text);
                if (f > 50)
                    pulses.Add(f);
            }
            else if (teststate == 2 && timerstate >= 1*60)
            {
                teststate = -1;
                timerstate = 0;
                timer.Stop();
                timer.Enabled = false;
                handleChatMessage("Test is afgelopen");
                calc(name);
            }

            if (!(teststate == 2 || teststate == -1))
            {
                if (label_status.InvokeRequired)
                {

                    label_status.Invoke((MethodInvoker) delegate
                    {
                        label_status.Visible = true;
                        if (rpm < 50)
                        {
                            label_status.Text = "cycle faster";
                            label_status.ForeColor = Color.Orange;
                        }
                        else if (rpm >= 50 && rpm < 70)
                        {
                            label_status.Text = "good job";
                            label_status.ForeColor = Color.Green;
                        }
                        else
                        {
                            label_status.Text = "too fast";
                            label_status.ForeColor = Color.Red;
                        }
                    });
                }
                else
                {
                    label_status.Visible = true;
                    if (rpm < 50)
                    {
                        label_status.Text = "too slow";
                        label_status.ForeColor = Color.Orange;
                    }
                    else if (rpm >= 50 && rpm < 70)
                    {
                        label_status.Text = "good job";
                        label_status.ForeColor = Color.Green;
                    }
                    else
                    {
                        label_status.Text = "too fast";
                        label_status.ForeColor = Color.Red;
                    }

                }
            }
            else
            {
                if (label_status.InvokeRequired)
                {

                    label_status.Invoke((MethodInvoker) delegate
                    {
                        label_status.Visible = false;
                    });
                }
                else
                {
                    label_status.Visible = false;
                }
            }
        }

        private void calc(string name)
        {
            long avgPulse = 0;
            foreach (var VARIABLE in pulses)
            {
                avgPulse += VARIABLE;
            }
            avgPulse /= pulses.Count;
            Double VOmax = 0;
            if (name.Contains("(m)"))
            {
                VOmax = (0.00212 * wattage * 6.12 + 0.299) / (0.769 * avgPulse - 48.5) * 1000;
            }
            else
            {
                VOmax = (0.00193 * wattage * 6.12 + 0.326) / (0.769 * avgPulse - 56.1) * 1000;
            }

            WriteTextMessage(connection, "04Astrand test voltooid. VO2Max = " + VOmax);
            handleChatMessage("Astrand test voltooid. VO2Max = " + VOmax);
        }

        private void HandleMessages(string data)
        {
            if(!(data.Equals("")))
            switch (data.Substring(0, 2))
            {
                case "04": handleChatMessage(data.Substring(2)); break;
                case "06": RaceUpdate(data.Substring(2)); break;
                case "08": guus(data.Substring(2)); break;
                case "10": Astrand(); break;
                default: break;
            }
        }

        private void RaceUpdate(string v)
        {
            String[] values = v.Split(',');

            Invoke(new SetRaceInfo(DisplayToRaceInfo), RaceInfo, values);


        }

        private void ConnectBike_Click(object sender, EventArgs e)
        {
            setPort(comboBox1.Text);
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
            string line = "";
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                string[] lines = (string[]) formatter.Deserialize(client.GetStream());
                if (lines.Length == 1)
                {
                    line = lines[0];
                }
            }
            catch
            {
            }

            return line;
        }

        //private void LoadLog_Click(object sender, EventArgs e)
        //{
        //    if(connection!=null)
        //    {
        //        WriteTextMessage(connection, "02-" + LogName.Text);
        //        String current;
        //        while ((current = ReadTextMessage(connection) ) != null)
        //        {
        //            Invoke(new SetTextDeleg(DisplayToUI), new object[] { current + Environment.NewLine });
        //        }
        //    }
        //}

        

        private void DisplayToUI(RichTextBox box, string displayData)
        {
            box.AppendText(displayData);
            box.ScrollToCaret();

        }

        private void DisplayToRaceInfo(RichTextBox box, String[] values)
        {
            box.ResetText();
            box.AppendText("RACING WITH:" + values[0] + Environment.NewLine);

            String[] data = values[1].Split('\t');

            box.AppendText("Afstand: " + (int.Parse(data[3])/10.0) + " km" + Environment.NewLine);
            box.AppendText("Tijd: " + data[6] + Environment.NewLine);
           

            box.ScrollToCaret();

        }

        private void Disconnect_Click(object sender, EventArgs e)
        {
            WriteTextMessage(connection,"03");
            connection.Close();
            connection = null;
        }

        private void sendmsg_Click(object sender, EventArgs e)
        {
            Invoke(new SetTextDeleg(updateChat), chatBox,"You: " + MessageBox.Text);
            WriteTextMessage(connection, "04" + MessageBox.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}

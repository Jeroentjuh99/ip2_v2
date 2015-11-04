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
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;

namespace Test
{
    public partial class Form1 : Form
    {
        private delegate void SetTextDeleg(string data);
        private delegate void SetTextCallback(string text);
        private SerialPort port;
        
        private List<string> toWrite = new List<string>(); 

        public Form1()
        {
            InitializeComponent();
            InitializeFileStreams();
            ListPorts();
        }

        public void InitializeFileStreams()
        {

           

        }

        public void ListPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            foreach(string s in ports)
            {
                comboBox1.Items.Add(s);
            }
        }

        public void setPort(string portName)
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
                        sendMessage("ST");
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

        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
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

        public void UpdateStatus(string data)
        {
            toWrite.Add(data.Replace("\n",""));
            if (label5.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(UpdateStatus);
                Invoke(d, new object[] { data });
            }
            else
            {
                label5.Text = data.Replace("\t", "    ");
            }
            
        }

        private void DisplayToUI(string displayData)
        {
            richTextBox1.AppendText(displayData);
            richTextBox1.ScrollToCaret();

        }

        private void sendMessage(string s)
        {
            if (port != null && port.IsOpen)
            {
                port.WriteLine(s);
            }
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

        private void button1_Click(object sender, EventArgs e)
        {
            sendMessage("RS");
            richTextBox1.Text = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            sendMessage("ST");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            sendMultipleMessages(new string[] { "CM", "PW " + textBox1.Text });
        }

        private void button4_Click(object sender, EventArgs e)
        {
            sendMultipleMessages(new string[] { "CM", "PT  " + textBox2.Text });
        }

        private void button5_Click(object sender, EventArgs e)
        {
            sendMultipleMessages(new string[] { "CM", "PD  " +  Convert.ToDouble( textBox3.Text)*10 });
        }

        private void button6_Click(object sender, EventArgs e)
        {
            sendMultipleMessages(new string[] { "CM", "PE  " + textBox4.Text });
        }

        private void ConnectButton(object sender, EventArgs e)
        {
            setPort(comboBox1.Text);
           
        }

        private void DisconnectButton(object sender, EventArgs e)
        {
            if (port != null)
            {
                port.Close();
            }
            port = null;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = System.Environment.CurrentDirectory;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string source = saveFileDialog.FileName;
                FileStream fifo = File.Open(source, FileMode.Create, FileAccess.ReadWrite);
                using (StreamWriter w = new StreamWriter(fifo))
                {
                    toWrite.ForEach(str =>
                    {
                        w.WriteLine(str);
                    });

                    toWrite.Clear();
                }

            }



            
        }

        private void button10_Click(object sender, EventArgs e)
        {

            OpenFileDialog browseFileDialog = new OpenFileDialog();
            browseFileDialog.InitialDirectory = System.Environment.CurrentDirectory;


            if (browseFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (browseFileDialog.CheckFileExists)
                {



                    string source = browseFileDialog.FileName;
                    using (StreamReader r = File.OpenText(source))
                    {
                        string current;
                        while ((current = r.ReadLine()) != null)
                        {
                            Invoke(new SetTextDeleg(DisplayToUI), new object[] { current + Environment.NewLine });
                        }
                    }


                }
            }

            browseFileDialog.Dispose();



           
        }
    }
}

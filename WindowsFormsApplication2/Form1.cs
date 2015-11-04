using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        private SerialPort port;
        private delegate void SetTextDeleg(string data);
        private int afstand, tijd, vermogen, rpm, uur, min, sec;
        private double snelheid, kJ;
        private Thread sim;
        private Boolean afstandAflopend, tijdAflopend;
        

        public Form1()
        {
            this.afstand = 0;
            this.tijd = 0;
            this.vermogen = 25;
            rpm = 0;
            kJ = 0.0;
            InitializeComponent();
            try
            {
                port = new SerialPort("COM10");

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

            }
            catch (Exception ex)
            {
               


            }

            sim = new Thread(new ThreadStart(this.Simulatie));
            sim.Start();

        }

        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            System.Threading.Thread.Sleep(500);
            string indata = sp.ReadExisting();
            // Console.WriteLine("Received");
            string command = indata.Substring(0, 2);

            //is.BeginInvoke(new SetTextDeleg(Display), new object[] { command });

            if (indata.Contains("PW"))
            {
                String filter = indata.Replace("ST", "");
                filter = filter.Replace("CM", "");
                filter = filter.Replace("ST", "");
                filter = filter.Replace("\n", "");
                vermogen = int.Parse(filter.Replace("PW ", "").Trim());
                setTextLbl06(vermogen + " W");
                port.WriteLine($"\t{rpm}\t{((int)snelheid * 3.6*10)}\t{afstand/100}\t{vermogen:000}\t{(int)kJ}\t{min}:{sec}\t25");
            }

            if (indata.Contains("ST"))
            {
                port.WriteLine($"\t{rpm}\t{((int)snelheid * 3.6 * 10)}\t{afstand / 100}\t{vermogen:000}\t{(int)kJ}\t{min:00}:{sec:00}\t25");
            }

            if (indata.Contains("RS")) {
                ResetData();
            }

            if (indata.Contains("PT"))
            {
                String filter = indata.Replace("ST", "");
                filter = filter.Replace("CM", "");
                filter = filter.Replace("ST", "");
                filter = filter.Replace("\n", "");
                int onberekendeTijd = int.Parse(filter.Replace("PT ", "").Trim());
                ResetData();
                tijd = (onberekendeTijd % 100) + (onberekendeTijd / 100) * 60;
                tijdAflopend = true;
            }

            if (indata.Contains("PD")) {
                String filter = indata.Replace("ST", "");
                filter = filter.Replace("CM", "");
                filter = filter.Replace("ST", "");
                filter = filter.Replace("\n", "");
                ResetData();
                afstand = 100*int.Parse(filter.Replace("PD ", "").Trim());
                afstandAflopend = true;
            }

            if (indata.Contains("PE"))
            {
                String filter = indata.Replace("ST", "");
                filter = filter.Replace("CM", "");
                filter = filter.Replace("ST", "");
                filter = filter.Replace("\n", "");
                ResetData();
                kJ = int.Parse(filter.Replace("PE ", "").Trim());

            }


            /*
            switch (command)
            {
                case "PW":
                    vermogen = int.Parse(indata.Replace("PW ", "").Trim());
                    String verm = "" + vermogen;
                    setTextLbl06(verm + " W ");
                    port.WriteLine($"0 {rpm} {(snelheid * 3.6):00.0} {afstand} {vermogen} {(int)kJ} {uur}:{min} 0 ");
                    break;
                case "ST":
                    port.WriteLine($"0 {rpm} {(snelheid * 3.6):00.0} {afstand} {vermogen} {(int)kJ} {uur}:{min} 0 ");
                    break;
                case "RS":
                    sim.Abort();
                    sim.Join();
                    vermogen = 0;
                    tijd = 0;
                    afstand = 0;
                    rpm = 0;
                    snelheid = 0;
                    kJ = 0;
                    Reset();
                    sim = new Thread(new ThreadStart(Simulatie));
                    sim.Start();
                    break;
                case "PT":
                    int onberekendeTijd = int.Parse(indata.Replace("PT ", "").Trim());
                    tijd = (onberekendeTijd % 100) + (onberekendeTijd / 100) * 60;
                    tijdAflopend = true;
                    break;
                case "PD":
                    afstand = int.Parse(indata.Replace("PD ", "").Trim());
                    afstandAflopend = true;
                    break;
               
                default:
                    port.WriteLine("ERROR");
                    break;
            }
            */
            this.BeginInvoke(new SetTextDeleg(Display), new object[] { indata });
        }

        private void Reset()
        {
            setTextLbl02($" {uur:00}:{min:00}.{tijd%60:00}");
            setTextLbl04("" + afstand  + " m");
            setTextLbl06("" + vermogen + " W");
            setTextLbl08($"{snelheid:0.00} m/s");
            setTextLbl10("" + rpm);
            setTextLbl12("" + (int)kJ);
        }

        public void Simulatie()
        {
            Random random = new Random();
            Boolean con = true;
            while (con)
            {
               

                if (snelheid < (7.5 - (vermogen * 0.015)))
                {
                    snelheid += (random.Next(100, 300) / 100.0); 
                } else if ( snelheid > (10.0 - (vermogen * 0.015))){
                    snelheid += (random.Next(-100, 0) / 100.0);
                } else
                {
                    snelheid += (random.Next(-50, 50) / 100.0);
                }
                rpm = (int)(snelheid / 0.125);
                if (afstandAflopend)
                {
                    afstand -= (int)snelheid;
                } else
                {
                    afstand += (int)snelheid;
                }
                if (tijdAflopend){
                    tijd -= 1;
                }
                else {
                    tijd += 1;
                }
                uur = tijd / 3600;
                min = tijd / 60;
                sec = tijd % 60;
                kJ += (0.5 * 5.0 * (snelheid*snelheid))/1000.0;
                Reset();
                if (tijd < 1 || afstand < 1)
                {
                    con = false;
                }
                Thread.Sleep(1000);
            }

        }

        private void ResetData()
        {
            sim.Abort();
            sim.Join();
            vermogen = 25;
            tijd = 0;
            afstand = 0;
            rpm = 0;
            snelheid = 0;
            kJ = 0;
            afstandAflopend = false;
            tijdAflopend = false;
            Reset();
            sim = new Thread(new ThreadStart(Simulatie));
            sim.Start();
        }
        
        

        private void Display(string displayData)
        {
            richTextBox1.Text += displayData;

        }
    }
    
}

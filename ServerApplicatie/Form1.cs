using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Security.Cryptography;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ServerApplicatie
{
    public partial class Form1 : Form
    {
        private TcpListener server;
        private List<Client> clients;

        public Form1()
        {
            InitializeComponent();

            clients = new List<Client>();
            CreateFolder();
           
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



        public List<Client> GetClients()
        {
            return clients;
        }

        private void StartServer(object sender, EventArgs e)
        {
            if (server == null) {
                IPAddress ip = IPAddress.Parse(GetPublicIp());
                server = new TcpListener(IPAddress.Any, 1338);
                DisplayOnScreen("Server is running...");
                UpdateIP(ip.ToString());
                server.Start();
                Thread thread = new Thread(new ThreadStart(ServerRunning));
                thread.Start();
            }
        }


        //Stopt de Server en slaat alle data op.
        private void StopServer(object sender, EventArgs e)
        {
            for (int i = clients.Count - 1; i >= 0; i--) {
                Client client = clients[i];
                client.Stop();
                clients.RemoveAt(i);
            }

            server.Stop();
            DisplayOnScreen("Server stopped!");
            server = null;
        }



        //Maakt een folder aan, op het moment dat hij nog niet bestaat.
        private void CreateFolder()
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "clientdata");
            bool exists = System.IO.Directory.Exists(path);
            if (!exists)
            {
                System.IO.Directory.CreateDirectory(path);
                DisplayOnScreen("Nieuwe folder aan gemaakt!");
            }
        }


        //Thread waarin de server loopt.
        //Voegt clients toe aan de list.
        private void ServerRunning()
        {
            DisplayOnScreen("Accepting clients!");
            while (true) {
                try {
                    Client client = new Client(server.AcceptTcpClient(), this);
                    this.clients.Add(client);
                    DisplayOnScreen("New client added!");
                } catch (Exception e)
                {

                }
            }
        }


        private delegate void SetTextCallback(string msg);

        //Verandert het IP adres op het scherm.
        public void UpdateIP(String text) {
            if (label3.InvokeRequired)
            {
                Invoke(new SetTextCallback(UpdateIP), new object[] { text });
            }
            else
            { label3.Text = text; }
        }


        //Geeft het public ip adres.
        private String GetPublicIp()
        {
            string ip = "";
            try
            {
                string url = "http://icanhazip.com";
                System.Net.WebRequest req = System.Net.WebRequest.Create(url);
                System.Net.WebResponse resp = req.GetResponse();
                System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                string response = sr.ReadToEnd().Trim();
//                string[] AllData = response.Split(':');
//                string dataString = AllData[1].Substring(1);
//                string[] data = dataString.Split('<');
                ip = response;
            }
            catch (Exception e) {
                ip = "0.0.0.0";
            }
            return ip;
        }



        //Voegt een bericht toe aan de textbox op het scherm.
        //Scrollt daarna naar onder.
        public void DisplayOnScreen(string displayData)
        {
            if (richTextBox1.InvokeRequired)
            {
                Invoke(new SetTextCallback(DisplayOnScreen), new object[] { displayData });
            }
            else {
                richTextBox1.AppendText(displayData + "\n");
                richTextBox1.ScrollToCaret();
            }
        }


        //Stuurt alle clientnamen naar de dokter.
        public void SendClientsToDoctors()
        {
            foreach (Client client in clients)
            {
                if (client.IsDoctor())
                {
                    foreach(Client c in clients)
                    {
                        if (!c.IsDoctor())
                        {
                            client.WriteMessage("05" + c.getClientname());
                        }
                    }
                }
            }
        }


        //Verwijdert een client.
        public void RemoveClient(Client client)
        {
            if (clients.Contains(client))
            {
                clients.Remove(client);
            }
        }



        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void GenerateKeyPair_Click(object sender, EventArgs e)
        {
            makeKeyPair();
        }
    }

    public class Client
    {
        private TcpClient client;
        private String clientname;
        private StreamReader reader;
        private StreamWriter writer;
        private List<String> data;
        private Form1 application;
        private Thread thread;
        private bool isDoctor = false;
        private bool isAlive = true;
        private List<Client> races;

        public Client(TcpClient client, Form1 application)
        {
            this.client = client;
            this.application = application;
            reader = new StreamReader(client.GetStream(), Encoding.ASCII);
            writer = new StreamWriter(client.GetStream(), Encoding.ASCII);
            thread = new Thread(HandleClient);
            thread.Start();
            data = new List<String>();
            races = new List<Client>();
        }


        //Geeft terug of de client een dokter is.
        public bool IsDoctor()
        {
            return isDoctor;
        }

        //Een race wordt toegevoegd in de List van races.
        public void AddRace(Client c)
        {
            races.Add(c);
        }

        //De Thread die ervoor zorgt dat op het moment dat er een Message binnenkomt, dat deze wordt afgehandeld.
        private void HandleClient()
        {
            MessageBox.Show(reader.ReadLine());
            while (true)
            {
                try {
                    HandleData(reader.ReadLine());
                } catch (Exception e)
                {
                  if (isAlive) { 
                      application.DisplayOnScreen("Error on client " + clientname + "! Closing client!");
                      StopConnection();
                  }
                }
            }
        }


        //Voegt de binnengekomen data toe aan de List. 
        //Indien de client een race heeft met een andere client, dan wordt de data ook naar die client toegestuurd.
        //De data wordt ook meteen doorgestuurd naar de dokter.
        private void AddData(String data)
        {
            this.data.Add(data);
            foreach(Client client in races)
            {
                client.WriteMessage("06" + clientname + "," + data);
            }
            foreach(Client client in application.GetClients())
            {
                if (client.IsDoctor())
                {
                    client.WriteMessage("01" + clientname + "," + data);
                }
            }
        }


        //Stuurt een bericht naar de client/
        public void WriteMessage(String message)
        {
            if (message.Length > 1)
            {
                string[] messages = {message};
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(client.GetStream(), messages);
            }
        }


        //Geeft de clientnaam terug.
        public String getClientname()
        {
            return clientname;
        }


        //Handelt de binnenkomende data af.
        private void HandleData(String data)
        {
            application.DisplayOnScreen(data);
            switch (data.Substring(0, 2))
            {
                case "00": NameClient(data); break;
                case "01": AddData(data.Substring(2)); break;
                case "02": StreamData(data.Substring(2));  break;
                case "03": StopConnection();  break;
                case "04": Chat(data.Substring(2)); break;
                case "06": Race(data.Substring(2)); break;
                case "07": DoctorConnecting(data.Substring(2)); break;
                case "08": SendCommando(data.Substring(2)); break;
                case "10": StartAstrand(data.Substring(3)); break;
                default: application.DisplayOnScreen("Incorrect message send!"); break;
            }
        }

        private void StartAstrand(string s)
        {
            var a = new Astrand(s, this);
            Thread t = new Thread(a.RunTest);
            t.IsBackground = true;
            t.Start();
        }

        private void SendCommando(string data)
        {
            string[] splitData = data.Split(':');
            foreach(Client client in application.GetClients())
            {
                if (client.clientname == splitData[0])
                {
                    client.WriteMessage("08" + splitData[1]);
                }
            }
        }


        //Verifieert of de gebruiker daadwerkelijk de dokter is.
        //Stuurt daarna een authenticatie bericht.
        private void DoctorConnecting(String data)
        {
            string[] splitData = data.Split(':');
            clientname = splitData[0];
            if (Decrypt(splitData[1]) == "PaulLindelauf")
            {
                WriteMessage("07Authentication Succesfull!");
                isDoctor = true;
            } else{
               WriteMessage("07Authentication Failed!");
            }
            application.SendClientsToDoctors();
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





        //Op het moment dat de dokter, 2 clients tegen elkaar wil laten racen.
        //Voegt dan bij allebei de clients toe dat ze met elkaar racen.
        private void Race(String data)
        {
            String[] clientnames = data.Split(':');
            Client client1 = null;
            Client client2 = null;
            foreach(Client c in application.GetClients())
            {
                if (c.clientname == clientnames[0])
                {
                    client1 = c;
                }
                if (c.clientname == clientnames[1])
                {
                    client2 = c;
                }
            }
            if (client1 != null && client2 != null && client1 != client2)
            {
                client1.AddRace(client2);
                client2.AddRace(client1);
            }
        }

        //Controleert of het bericht door een dokter wordt verstuurd.
        //Indien het door een dokter verstuurd wordt, dan controleert hij naar welke persoon het verstuurd moet worden.
        //Anders stuurt hij het naar de dokter.
        private void Chat(String data)
        {
            if (IsDoctor())
            {
                String[] splitData = data.Split(':');
                foreach(Client c in application.GetClients())
                {
                    if (c.clientname == splitData[0])
                    {
                        c.WriteMessage("04" + splitData[1]);
                    }
                }
            } else {
                foreach(Client client in application.GetClients())
                {
                    if (client.IsDoctor())
                    {
                        client.WriteMessage("04" + clientname + ":" + data);
                    }
                }
            }
        }


        //Streamt data van de client door naar de dokter, indien hier om gevraagd wordt.
        private void StreamData(String data)
        {
            if (isDoctor)
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "clientdata", data + ".dat");
                if (File.Exists(path))
                {
                    application.DisplayOnScreen("Looking up data from " + data);
                    String[] lines = File.ReadAllLines(path);
                    WriteMessage("02Log van " + data);
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(client.GetStream(), lines);
                }
                else
                {
                    application.DisplayOnScreen("File not found!");
                }
            } else
            {
                application.DisplayOnScreen("A stranger tried to look up the data from " + data);
            }
        }


        //Geeft de client een naam.
        //Stuurt vervolgens naar de dokter dat er een nieuwe client verbonden is.
        private void NameClient(String data)
        {
            clientname = data.Substring(2);
            application.SendClientsToDoctors();
        }


        //Slaat de data van de client op.
        //Indien de patient niet eerder de test heeft uitgevoerd, dan zal er een nieuw bestand worden aangemaakt.
        public void SafeData()
        {
            if (clientname != "")
            {
                try {
                    string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "clientdata", clientname + ".dat");
                    if (File.Exists(path))
                    {
                        String[] lines = System.IO.File.ReadAllLines(path);
                        StreamWriter tw = new StreamWriter(path);
                        foreach (String line in lines)
                        {
                            tw.WriteLine(line.ToString());
                        }
                        tw.WriteLine("----------------------------------------");
                        tw.WriteLine(DateTime.Now.ToString("HH:mm:ss dd-MM-yyyy"));
                        foreach (String line in data)
                        {
                            tw.WriteLine(line.ToString());
                        }
                        tw.Close();
                    }
                    else
                    {
                        File.Create(path).Close();
                        StreamWriter tw = new StreamWriter(path);
                        tw.WriteLine(DateTime.Now.ToString("HH:mm:ss dd-MM-yyyy"));
                        foreach (String line in data)
                        {
                            tw.WriteLine(line.ToString());
                        }
                        tw.Close();
                    }
                } catch(Exception e)
                {
                    application.DisplayOnScreen("Couldn't safe the data of " + clientname);
                }
            }
        }

        //Sluit de connectie met een client af.
        //Hij slaat de data van de client eerst op.
        //Dan sluit hij de thread af.
        public void StopConnection()
        {
            SafeData();
            application.DisplayOnScreen("Connection closed with " + clientname);
            isAlive = false;
            this.thread.Abort();
            application.RemoveClient(this);
        }

        public void Stop()
        {
            SafeData();
            application.DisplayOnScreen("Connection closed with " + clientname);
            isAlive = false;
            this.thread.Abort();
        }

    }

    public class Astrand
    {
        private Timer timer;
        private string user;
        private Client client;

        public Astrand(string user, Client client)
        {
            this.user = user;
            this.client = client;
            timer.Interval = 1000;
            timer.Elapsed += new ElapsedEventHandler(timer_Tick);
            client.WriteMessage("Astrand test is gestart. Blijft u aub rond de 60 omwentelingen per minuut fietsen.");
        }

        public void timer_Tick(object sender, EventArgs e)
        {
            
        }

        public void RunTest()
        {
            timer.Enabled = true;
            timer.Start();

        }
    }
}

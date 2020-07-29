using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;

namespace Log_Manager
{
    public partial class Form1 : Form
    {
        public string local = Properties.Settings.Default.Local;
        public string log1  = Properties.Settings.Default.Log1;
        public string log2  = Properties.Settings.Default.Log2;
        public string user = Properties.Settings.Default.user;
        public string password = Properties.Settings.Default.password;
        public string LogMaster = Properties.Settings.Default.LogMaster;
        public string DataLogFTP = Properties.Settings.Default.FTP;
        public string logname;
        public string Tahun,Tanggal,Bulan;
        public Form1()
        {
            InitializeComponent();
            logname = "TEST";
            CompareLog(local, LogMaster);
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
           
        }
        string[] pemisah;
        string jamlocal;
        private void GetDateTime()
        {
            bool jamserver = false;
            string date;
            try
            {
                using (WebClient client = new WebClient())
                {
                    string ServerTime = client.DownloadString("http://10.201.192.20/AutoParser/SCH/ICT/GetServerDateTime.aspx?txtFFDB=FF2810_SCH");
                    pemisah = ServerTime.Split(';');
                    jamserver = true;
                }
            }
            catch
            {
                jamlocal = DateTime.Now.ToString("M/d/yyyy h:mm:ss tt");
                jamserver = false;
            }
            if (jamserver == true)
            {
               date =  pemisah[1];
            }
            else
            {
               date =  jamlocal;
            }
            string[] date1 = date.Split(' ');
            string[] date2 = date1[0].Split('/');
            Tahun = date2[2];
            Bulan = date2[0];
            Tanggal = date2[1];
        }
        private void LogManager()
        {
            bool Lokal = CekLocal(local);
            if (Lokal == true)
            {
                bool logmaster;
                testplan();
                logmaster = CekFTPtestlog(LogMaster, logname);
                if (logmaster == true)
                {

                }
                else
                {
                    SendtoFTP(local, DataLogFTP);
                }
            }  
        }
        string [] ParsingLog(string arraye)
        {
            
            string removespace = arraye.Replace(" ", "");
            string removeblanksenter = removespace.Replace("\r\n\r\n", "\r\n");
            string removeblanksenter1 = removeblanksenter.Replace("\r\n", "\r");
            return removeblanksenter1.Split('\r');
        }
        private void CompareLog(string localpath, string logmasterpath)
        {
            List<String> Mylocalfolder = Directory.GetFiles(localpath, "*.txt*", SearchOption.AllDirectories).ToList();
            List<string> info = new List<string>();
            List<string> component1 = new List<string>();
            List<string> component2 = new List<string>();
            List<string> component3 = new List<string>();
            List<string> component4 = new List<string>();
            List<string> component5 = new List<string>();
            List<string> infomaster = new List<string>();
            List<string> componentmaster1 = new List<string>();
            List<string> componentmaster2 = new List<string>();
            List<string> componentmaster3 = new List<string>();
            List<string> componentmaster4 = new List<string>();
            List<string> componentmaster5 = new List<string>();
            foreach (string file in Mylocalfolder)
            {
                FileInfo mFile = new FileInfo(file);
                string hasilfile = Convert.ToString(mFile);
                string localfile = File.ReadAllText(hasilfile);
                string [] perbaris = ParsingLog(localfile);
                for (int a = 0; a <= perbaris.Length; a++)
                {
                    try
                    {
                        if(perbaris[a]!="")
                        {
                            string[] oke = perbaris[a].Split(':');
                            if (oke.Length == 2)
                            {
                                info.Add(perbaris[a]);
                            }
                            if (oke.Length == 5)
                            {
                                component1.Add(oke[0]);
                                component2.Add(oke[1]);
                                component3.Add(oke[2]);
                                component4.Add(oke[3]);
                                component5.Add(oke[4]);
                            }
                        }
                        
                    }
                    catch
                    {
                    }
                }
                WebClient request = new WebClient();
                string url = logmasterpath + logname +"/"+"Master.txt";
                request.Credentials = new NetworkCredential(user,password);
                List<string> pass = new List<string>();
                List<string> skip = new List<string>();
                try
                {
                    byte[] newFileData = request.DownloadData(url);
                    string fileString = System.Text.Encoding.UTF8.GetString(newFileData);
                    string[] master1 = ParsingLog(fileString);
                    for (int a = 0; a <= master1.Length; a++)
                    {
                        try
                        {
                            if (master1[a] != "")
                            {
                                string[] oke = master1[a].Split(':');
                                if (oke.Length == 2)
                                {
                                    infomaster.Add(master1[a]);
                                }
                                if (oke.Length == 5)
                                {
                                    componentmaster1.Add(oke[0]);
                                    componentmaster2.Add(oke[1]);
                                    componentmaster3.Add(oke[2]);
                                    componentmaster4.Add(oke[3]);
                                    componentmaster5.Add(oke[4]);
                                }
                            }

                        }
                        catch
                        {
                        }
                    }
                    string firstElem = component1.First();
                    string restOfArray = string.Join("\n\r", component1.Skip(1));
                    foreach(string component in componentmaster1)
                    {
                        if(restOfArray.Contains(component))
                        {
                            pass.Add(component);
                        }
                        else
                        {
                            skip.Add(component);
                             MessageBox.Show("component skip :"+ component );
                        }
                    }
                }
                catch
                {
                }
                
            }
        }
        private void SendtoFTP(string localpath, string ftppath)
        {
            GetDateTime();
            CreateFolder(DataLogFTP, Tahun, Bulan, Tanggal);
            List<String> Mylocalfolder = Directory.GetFiles(localpath, "*.txt*", SearchOption.AllDirectories).ToList();
            foreach (string file in Mylocalfolder)
            {
                FileInfo mFile = new FileInfo(file);
                string hasilfile = Convert.ToString(mFile);
                using (var client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(user, password);
                    client.UploadFile(ftppath + logname + "/" + Tahun + "/" + Bulan + "/" + Tanggal + "/" + mFile.Name, WebRequestMethods.Ftp.UploadFile, file);
                    System.IO.File.Delete(hasilfile);
                    notifyIcon1.BalloonTipText = (mFile.Name + " Sent!");
                    notifyIcon1.ShowBalloonTip(1000);
                }
            }
        }
        private void CreateFolder(string ftpServer, string Tahun, string Bulan, string Tanggal)
        {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(ftpServer + logname + @"/" + Tahun + @"/" + Bulan + @"/" + Tanggal + @"/");
            request.Credentials = new NetworkCredential(user, password);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            try
            {
                using (var resp = (FtpWebResponse)request.GetResponse())
                {
                }
            }
            catch
            {
            }
        }
        private void parsing(string path)
        {
            string lognames = File.ReadAllText(path);
            string project = lognames;
            string projectnow = project.Substring(project.LastIndexOf("\\"));
            string []projectnow1 = projectnow.Split('#');
            string final = projectnow1[0].Replace(@"\", "");
            logname = final;
        }
        private void testplan()
        {
            if (File.Exists(log1))
            {
                parsing(log1);
            }
            if (File.Exists(log2))
            {
                parsing(log2);
            }
        }
        private bool CekFTPtestlog(string path, string logname)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(path + logname + "/"));
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(user, password);

            try
            {
                using (request.GetResponse())
                {
                    return true;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }
        public bool CekLocal (string path)
        {
            if(!File.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            try
            {
                int count = 0;
                List<String> Mylocalfolder = Directory.GetFiles(path, "*.txt*", SearchOption.AllDirectories).ToList();
                foreach (string file in Mylocalfolder)
                {
                    count++;
                }
                if (count >= 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

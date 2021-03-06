﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Diagnostics;
using Guna.UI2.WinForms;

namespace Log_Manager
{
    public partial class Form1 : Form
    {
        public string local = Properties.Settings.Default.Local1;
        public string log1  = Properties.Settings.Default.Log1;
        public string log2  = Properties.Settings.Default.Log2;
        public string user = Properties.Settings.Default.user;
        public string password = Properties.Settings.Default.password;
        public string LogMaster = Properties.Settings.Default.LogMaster;
        public string DataLogFTP = Properties.Settings.Default.FTP;
        public string projectfolder = Properties.Settings.Default.projectfolder;
        public string logname;
        public string Tahun,Tanggal,Bulan;
        
        public Form1()
        {
            InitializeComponent();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
            }
            LogManager();
            guna2TextBox1.Text = DataLogFTP;
            guna2TextBox2.Text = local;
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

        private void LocalMove(string localpath)
        {
            List<String> Mylocalfolder = Directory.GetFiles(localpath, "*", SearchOption.AllDirectories).ToList();
            foreach (string file in Mylocalfolder)
            {
                try
                {
                    FileInfo mFile = new FileInfo(file);
                    Directory.CreateDirectory(projectfolder + "\\" + logname );
                    File.Move(file, projectfolder + "\\" + logname+"\\"+ mFile.Name);
                }
                catch 
                {
                    
                }

            }
        }
        private void SendtoFTP(string localpath, string ftppath)
        {
            GetDateTime();
            List<String> Mylocalfolder = Directory.GetFiles(localpath, "*", SearchOption.AllDirectories).ToList();
            foreach (string file in Mylocalfolder)
            {
                try
                {
                    FileInfo mFile = new FileInfo(file);
                    string folderproject = Path.GetFileName(Path.GetDirectoryName(file));// get latest folder name
                    string hasilfile = Convert.ToString(mFile);
                    CreateFolder(ftppath,folderproject, Tahun, Bulan, Tanggal);
                    using (var client = new WebClient())
                    {
                        client.Credentials = new NetworkCredential(user, password);
                        client.UploadFile(
                            ftppath + folderproject + "/" + Tahun + "/" + Bulan + "/" + Tanggal + "/" + mFile.Name,
                            WebRequestMethods.Ftp.UploadFile, file);
                        System.IO.File.Delete(hasilfile);
                        notifyIcon1.BalloonTipText = (mFile.Name + " Sent From " + folderproject +" !");
                        notifyIcon1.ShowBalloonTip(1000);
                    }
                }
                catch
                {

                }
            }
        }
        private void LogManager()
        {
            bool Lokal = CekLocal(local);
            if (Lokal == true)
            {
                //bool logmaster;
                testplan();
                LocalMove(local);
                //logmaster = CekFTPtestlog(LogMaster, logname);
                //if (logmaster == true)
                //{
                //    CompareLog(local, LogMaster);
                //    SendtoFTP(local, DataLogFTP);
                //}
                //else
                //{
                SendtoFTP(projectfolder, DataLogFTP);
                //}
            }  
        }
        string [] ParsingLog(string arraye)
        {
            string removespace = arraye.Replace(" ", "");
            string removeblanksenter = removespace.Replace("\r\n\r\n", "\r\n");
            string removeblanksenter1 = removeblanksenter.Replace("\r\n", "\r");
            return removeblanksenter1.Split('\r');
        }
        string MasterLOG;
        private void CompareLog(string localpath, string logmasterpath)
        {
            List<String> Mylocalfolder = Directory.GetFiles(localpath, "*.txt", SearchOption.AllDirectories).ToList();
            foreach (string file in Mylocalfolder)
            {
                List<string>info = new List<string>();
                List<string>comp = new List<string>();
                string localfile = File.ReadAllText(file);
                string [] compressed = ParsingLog(localfile);
                foreach (string isi in compressed)
                {
                    if (isi != "")
                    {
                        string [] com = isi.Split(':');
                        if (com.Length == 2)
                        {
                            info.Add(isi);
                        }
                        else if (com.Length == 3)
                        {
                            info.Add(isi);
                        }
                        else
                        {
                            comp.Add(isi);
                        }

                    }
                }
                List<string> infomaster = new List<string>();
                List<string> compmaster = new List<string>();
                WebClient request = new WebClient();
                string url = logmasterpath + logname +"/"+"Master.txt";
                request.Credentials = new NetworkCredential(user,password);
                
                try
                {
                    byte[] newFileData = request.DownloadData(url);
                    MasterLOG = System.Text.Encoding.UTF8.GetString(newFileData);
                    string[] compressedmaster = ParsingLog(MasterLOG);
                    foreach (string isi in compressedmaster)
                    {
                        if (isi != "")
                        {
                            string[] com = isi.Split(':');
                            if (com.Length == 2)
                            {
                                info.Add(isi);
                            }
                            else if (com.Length == 3)
                            {
                                info.Add(isi);
                            }
                            else
                            {
                                compmaster.Add(isi);
                            }

                        }
                    }
                }
                catch
                {
                }
                List<string> skipped = skipcheck(comp, compmaster);
                if (skipped.Count == 0)
                {
                    List<string> overparameter = over(comp, compmaster);
                    if(overparameter.Count != 0)
                    {
                        label1.Text = "Over Parameter";
                        richTextBox1.Lines = overparameter.ToArray();
                        this.Show();
                    }
                    
                }
                else
                {
                    label1.Text = "Skip Component";
                    richTextBox1.Lines = skipped.ToArray();
                    this.Show();
                }
            }    
        }
        public string convert_string_to_no(string val)
        {
            bool kilo = false;
            bool mega =false;
            bool miliV = false;
            if (val.Contains("mV"))
            {
                miliV = true;
            }
            if(val.Contains('K'))
            {
                kilo = true;
            }
            if(val.Contains('M'))
            {
                mega = true;
            }
            if (val == "Pass")
            {
                val = "1";
            }
            if (val == "Fail")
            {
                val = "0";
            }
            if (val == "OPEN")
            {
                val = "99999999";
            }
            string str_val = "";
            int val_len = val.Length;
            for (int i = 0; i < val_len; i++)
            {
                char myChar = Convert.ToChar(val.Substring(i, 1));
                if (char.IsDigit(myChar) || myChar == '.' || myChar == '-')
                {
                    
                    str_val += myChar;
                }
            }
            if (str_val.Contains('-'))
            {
                str_val = str_val.Replace("-","");
                double a = Convert.ToDouble(str_val);
                double ok = 0 - a;
                str_val = Convert.ToString(ok);
            }
            if(kilo==true)
            {
                double test = Convert.ToDouble(str_val);
                double hasil = test * 1000;
                str_val = Convert.ToString(hasil);
            }
            if (mega == true)
            {
                double test = Convert.ToDouble(str_val);
                double hasil = test * 1000000;
                str_val = Convert.ToString(hasil);
            }
            if (miliV == true)
            {
                double test = Convert.ToDouble(str_val);
                double hasil = test / 1000;
                str_val = Convert.ToString(hasil);
            }
            return str_val;
        } 
        private List<string> over (List<string> localcomp, List<string> Master)
        {
            localcomp.Sort();
            Master.Sort();
            List<string> Comp = new List<string>();
            for (int a = 0; a <= localcomp.Count - 1; a++)
            {
                string[] splited = localcomp[a].Split(':');
                Comp.Add(splited[2]);
            }
            List<string> MastLow = new List<string>();
            List<string> MastHigh = new List<string>();
            for (int a = 0; a <= Master.Count - 1; a++)
            {
                string[] splited = Master[a].Split(':');
                MastLow.Add(splited[1]);
                MastHigh.Add(splited[3]);
            }
            List<string> Pass = new List<string>();
            List<string> Fail = new List<string>();
            for (int a = 0; a < Master.Count; a++)
            {
                try
                {
                    double result = Convert.ToDouble(convert_string_to_no(Comp[a]));
                    double low = Convert.ToDouble(convert_string_to_no(MastLow[a]));
                    double high = Convert.ToDouble(convert_string_to_no(MastHigh[a]));
                    if (result >= low && result <= high)
                    {
                        Pass.Add(localcomp[a]);
                    }
                    else
                    {
                        Fail.Add(localcomp[a]);
                    }
                }
                catch
                {
                }
            }
            return Fail;
        }
        private List<string> skipcheck(List<string> localcomp, List<string> Master)
        {
            List<string> Comp = new List<string>();
            for(int a=0 ; a<=localcomp.Count-1;a++)
            {
                string [] splited = localcomp[a].Split(':');
                Comp.Add(splited[0]);
            }
            List<string> Mast = new List<string>();
            for (int a = 0; a <= Master.Count-1; a++)
            {
                string[] splited = Master[a].Split(':');
                Mast.Add(splited[0]);
            }
            IEnumerable<string> difference = Mast.Except(Comp);
            List<string> asList = difference.ToList();
            return asList;
        }
        
        private void CreateFolder(string ftpServer, string Folderproject, string Tahun, string Bulan, string Tanggal)
        {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(ftpServer + Folderproject + @"/" + Tahun + @"/" + Bulan + @"/" + Tanggal + @"/");
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
            gunaLabel5.Text = final;
            logname = final;
        }
        private void testplan()
        {
            try
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
            catch
            {

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
            if(File.Exists(path)==false)
            {
                System.IO.Directory.CreateDirectory(path);
            }
            try
            {
                List<String> Mylocalfolder = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
                if (Mylocalfolder.Count >= 1)
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

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "";
            this.Hide();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            Hide();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
            
        }
    }
}
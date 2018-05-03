using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;
using Npgsql;

namespace HolaDownloader
{
    public partial class HolaDownloader : Form
    {
        static int MaxDownloadThreads = 30;
        static int filesCounter = 1;
        
        class taskItem
        {
            public long taskid;
            public long id;
            public string url;
            public string path;
            public long owner_id;
            public long album_id;
            public long item_id;
        }

        static Object fileCounterLock = new Object();
        static ConcurrentQueue<taskItem> TaskList = new ConcurrentQueue<taskItem>();

        static System.Windows.Forms.Timer ScanDownloadTaskTimer = new System.Windows.Forms.Timer();

        public HolaDownloader()
        {
            InitializeComponent();

            System.Net.ServicePointManager.DefaultConnectionLimit = MaxDownloadThreads+10;

            for (int ThreadId = 0; ThreadId < MaxDownloadThreads; ThreadId++)
            {
                ListViewItem LVI = listView1.Items.Add(ThreadId.ToString());
                LVI.Name = ThreadId.ToString();
                listView1.Items[ThreadId].SubItems.Add("");
                new Thread((object data) => FileLoaderJob(data)).Start(ThreadId.ToString());
            }

            ScanDownloadTaskTimer.Tick += new EventHandler(LoadTasksFromDatabase);
            ScanDownloadTaskTimer.Interval = 5000;
            ScanDownloadTaskTimer.Start();
        }

        static void FilesDec()
        {
            lock (fileCounterLock)
            {
                filesCounter--;
            }
        }
        static void FilesInc()
        {
            lock (fileCounterLock)
            {
                filesCounter++;
            }
        }

        void UpdateListView(string ThreadId, string filename)
        {
            listView1.Items[ThreadId].SubItems[1].Text = filename;
        }

        void FileLoaderJob(object oThreadId)
        {
            taskItem ti;
            string ThreadId = (string)oThreadId;
            string sql;
            for (;!Program.AppWasClosed;)
            {
                if (!TaskList.TryDequeue(out ti))
                {
                    Thread.Sleep(1000);
                    continue;
                }

                sql = DBHelper.ExecuteScalar("select cast( get_id_by_owner_album(" + ti.owner_id + "," + ti.album_id + ") as varchar)");
                if (sql.CompareTo("0") != 0)
                {
                    sql = DBHelper.ExecuteScalar("select  cast( get_full_path("+sql+") as varchar)");
                    ti.path = sql + "\\" + ti.path.Split('\\').Last();
                };

                this.Invoke((MethodInvoker)(() => UpdateListView(ThreadId, ti.path)));

                try
                {
                    ti.path = FileLoader(ti.path, ti.url);
                    using (NpgsqlConnection sqlConnection = new NpgsqlConnection(Program.connectionString))
                    {
                        sqlConnection.Open();
                        sql = "update download_task set state=1, realpath = N'" + ti.path.Replace("'", "''") + "' where taskid=" + ti.taskid + " ;";
                        using (var cmd = new NpgsqlCommand(sql, sqlConnection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        sqlConnection.Close();
                    }
                }
                catch (Exception E) {
                    using (NpgsqlConnection sqlConnection = new NpgsqlConnection(Program.connectionString))
                    {
                        sqlConnection.Open();
                        using (var cmd = new NpgsqlCommand("update download_task set state=2, realpath = N'" + E.Message + "' where taskid=" + ti.taskid, sqlConnection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        sqlConnection.Close();
                    }
                };
                FilesDec();
                this.Invoke((MethodInvoker)(() => UpdateListView(ThreadId, ti.path + "  done.")));
            }
        }

        string FileLoader(string filename, string url)
        {
            string ext = url.Split('.').Last();
            if (filename[filename.Length - 1] != '.') filename += '.';
            filename = filename + "" + ext;
            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            httpWebRequest.Timeout = 5000;

            using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                using (Stream stream = httpWebReponse.GetResponseStream())
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                    FileStream output = File.Create(filename);
                    stream.CopyTo(output);
                    output.Close();
                    stream.Close();
                }
            }
            return filename;
        }

        private static void LoadTasksFromDatabase(Object myObject, EventArgs myEventArgs)
        {
            ScanDownloadTaskTimer.Stop();
            if (filesCounter==0)
            {
                using (NpgsqlConnection sqlConnection = new NpgsqlConnection(Program.connectionString))
                {
                    sqlConnection.Open();
                    using (var cmd = new NpgsqlCommand("select taskid, id, text, url, path, owner_id, album_id, item_id from download_task where state=0; ", sqlConnection))
                    {
                        NpgsqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            TaskList.Enqueue(new taskItem
                            {
                                taskid = reader.GetInt64(0),
                                id = reader.GetInt64(1),
                                url = reader.GetString(3),
                                path = reader.GetString(4),
                                owner_id = reader.GetInt64(5),
                                album_id = reader.GetInt64(6),
                                item_id = reader.GetInt64(7)
                            });
                            FilesInc();
                        }
                        reader.Close();
                    }
                    sqlConnection.Close();
                }
            }
            ScanDownloadTaskTimer.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.AppWasClosed = true;
        }
    }
}

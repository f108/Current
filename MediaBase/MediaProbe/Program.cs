using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Npgsql;
using System.IO;
using System.Xml;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;

namespace MediaProbe
{
    class Program
    {
        const string connectionString = "";
        static string tempPath;
        struct ProbeJob
        {
            public long ID;
            public string filename;
        }
        static ConcurrentQueue<ProbeJob> ExploreJobList = new ConcurrentQueue<ProbeJob>();
        static ConcurrentBag<long> SelectedJobs = new ConcurrentBag<long>();
        static ConcurrentQueue<long> TaskList = new ConcurrentQueue<long>();
        static ConcurrentQueue<KeyValuePair<int, string>> output_messages = new ConcurrentQueue<KeyValuePair<int, string>>();
        static void TaskWorker(object _IaskID)
        {
            long file_id=0;
            int taskState=0;
            int taskType=0;
            long duration=0;
            string param1="";
            string param2 = "";
            string str;
            string sql;
            long TaskID = (int)_IaskID;
            for (;;)
            {
                if (!TaskList.TryDequeue(out TaskID))
                {
                    Thread.Sleep(150);
                    continue;
                }

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    sql = "select n, task_state, task_type, file_id, param1, param2 from tasks where n="+ TaskID;
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        NpgsqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            taskState = reader.GetInt16(1);
                            taskType = reader.GetInt16(2);
                            file_id = reader.GetInt64(3);
                            if (reader.IsDBNull(4)) param1 = ""; else param1 = reader.GetString(4);
                            if (reader.IsDBNull(5)) param2 = ""; else param2 = reader.GetString(5);
                        }
                        reader.Close();
                    }
                }

                try
                {
                    switch (taskType)
                    {
                        case 1: // ffprobe: get video info in xml
                            GetVideoThumb(TaskID, file_id, param1, "");
                            break;
                        case 2: //
                            GetImagePreview(TaskID, file_id, param1);
                            break;
                        case 3: // ffprobe: get video info in xml
                            GetVideoInfo(TaskID, file_id, param1);
                            break;
                        case 4: 
                            GetVideoInfo(TaskID, file_id, param1);
                            GetVideoThumb(TaskID, file_id, param1, param2);
                            break;
                        case 6:
                            BulkGetImagePreview(TaskID, file_id, param1, param2);
                            break;
                        case 7:
                            FFMPEGConvert(TaskID, file_id, param1, param2);
                            break;
                        case 8:
                            GetGrayScaled32(TaskID, file_id, param1, param2);
                            break;
                        case 9:
                            BulkDeleteTask(TaskID, file_id, param1, param2);
                            break;

                    }
                    using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("update tasks set task_state=2  where n=" + TaskID, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        conn.Close();
                    }
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Task: " + TaskID + " ended");

                }
                catch (Exception E)
                {
                    using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("update tasks set task_state=3, status=N'" + E.Source + ": " + E.Message.Replace("'", "''") + "' where n=" + TaskID, conn))
                        {
                            cmd.ExecuteNonQuery();
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Task: " + TaskID + " Exception: " + E.Source + " " + E.Message);
                        }
                        conn.Close();
                    }
                }
                finally
                {

                };

            }
        }

        static void BulkDeleteTask(long task_number, long file_id, string param1, string param2)
        {
            string sql = "";
            using (NpgsqlConnection sqlConnection = new NpgsqlConnection(Program.connectionString))
            {
                sqlConnection.Open();
                using (var cmd = new NpgsqlCommand("select id, get_full_path(id) from files where id in (0" + param2 + ");", sqlConnection))
                {
                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        try
                        {
                            File.Delete(reader.GetString(1));
                        }
                        catch { };
                    }
                    reader.Close();
                }
                sqlConnection.Close();
            }

        }
        static void GetGrayScaled32(long task_number, long file_id, string param1, string param2)
        {
            string sql = "delete from grayscaled_img where up=@id; insert into grayscaled_img(up, gs16x16) values(@id, @img16); ";
            Dictionary<long, string> files = new Dictionary<long, string>();
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                NpgsqlTransaction mTrans = conn.BeginTransaction();
                long id = 0;
                int size;
                byte[] rawData;
                MemoryStream ms;

                using (var cmd = new NpgsqlCommand("SELECT up, length(image), image FROM media_preview WHERE media_preview.up IN (0" + param2 + ")", conn))
                {
                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Image img;
                        id = reader.GetInt64(0);
                        size = reader.GetInt32(1);
                        if (size <= 0) continue;
                        rawData = new byte[size];
                        reader.GetBytes(2, 0, rawData, 0, size);
                        ms = new MemoryStream(rawData);
                        img = new Bitmap(ms);
                        ms.Close();
                        ms.Dispose();

                        byte[] gsa32, gsa16;

                        Bitmap newImage = new Bitmap(32, 32);
                        using (Graphics gr = Graphics.FromImage(newImage))
                        {   gr.SmoothingMode = SmoothingMode.HighQuality;   gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            gr.DrawImage(img, new Rectangle(0, 0, 32, 32)); };
                        gsa32 = grayscaleArray(newImage, 32);

                        Bitmap newImage16 = new Bitmap(16, 16);
                        using (Graphics gr = Graphics.FromImage(newImage16))
                        {
                            gr.SmoothingMode = SmoothingMode.HighQuality; gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            gr.DrawImage(img, new Rectangle(0, 0, 16, 16));
                        };
                        gsa16 = grayscaleArray(newImage16, 16);

                        using (NpgsqlConnection conn2 = new NpgsqlConnection(connectionString))
                        {
                            conn2.Open();
                            using (var cmd2 = new NpgsqlCommand(sql, conn2))
                            {
                                cmd2.Parameters.AddWithValue("@id", id);
                                //cmd2.Parameters.AddWithValue("@img32", gsa32);
                                cmd2.Parameters.AddWithValue("@img16", gsa16);
                                cmd2.ExecuteNonQuery();
                            }
                            conn2.Close();
                        }

                    }
                    reader.Close();

                }
                mTrans.Commit();
                conn.Close();
            }
        }
        public static byte[] grayscaleArray(Bitmap img32, int patternSize)
        {
            int Depth;
            BitmapData data;
            int gsDataLength = patternSize * patternSize;
            byte[] gsData = new byte[gsDataLength];
            Rectangle PatternRect = new Rectangle(0, 0, patternSize, patternSize);
            Bitmap bmp = new Bitmap(img32, patternSize, patternSize);
            Depth = System.Drawing.Bitmap.GetPixelFormatSize(bmp.PixelFormat);
            data = bmp.LockBits(PatternRect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            IntPtr intdata = data.Scan0;
            unsafe
            {
                byte* ptr = (byte*)intdata.ToPointer();
                int addr;
                float res;
                for (int i = 0; i < gsDataLength; i++)
                {
                    addr = i * 4;
                    res = (float)(ptr[addr] * 0.11 + ptr[addr + 1] * 0.59 + ptr[addr + 2] * 0.3);
                    gsData[i] = (byte)res;
                }
            }
            bmp.UnlockBits(data);
            return gsData;
        } 
        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);
            Graphics g = Graphics.FromImage(newBitmap);
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
              {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
              });
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            g.Dispose();
            return newBitmap;
        }

        static void BulkGetImagePreview(long task_number, long file_id, string param1, string param2)
        {
            string sql = "delete from media_preview where up=@id; insert into media_preview(up,image) values(@id, @img); ";
            Dictionary<long, string> files = new Dictionary<long, string>();
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                //NpgsqlTransaction mTrans = conn.BeginTransaction();
                long id=0;
                string path="";

                using (var cmd = new NpgsqlCommand("SELECT files.id, get_full_path(files.id) FROM files WHERE files.id IN (0"+param2+")", conn))
                {
                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        id = reader.GetInt64(0);
                        path = reader.GetString(1);
                        files.Add(id, path);
                    }
                    reader.Close();

                    for (int i = 0; i < files.Count; i++)
                    {
                        try
                        {
                            id = files.ElementAt(i).Key;
                            path = files.ElementAt(i).Value;
                            MemoryStream thumb = GetImage(id, path, 256);
                            byte[] pic_arr = new byte[thumb.Length];
                            thumb.Position = 0;
                            thumb.Read(pic_arr, 0, pic_arr.Length);

                            using (var cmd2 = new NpgsqlCommand(sql, conn))
                            {
                                cmd2.Parameters.AddWithValue("@id", id);
                                cmd2.Parameters.AddWithValue("@img", pic_arr);
                                cmd2.ExecuteNonQuery();
                            }

                        }
                        catch (Exception E)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("ID: " + id + " " + path + " Exception: " + E.Source + " " + E.Message);
                        };
                    }
                }
                //mTrans.Commit();

            }
        }
        static void GetImagePreview(long task_number, long file_id, string param1)
        {
            string sql = "delete from media_preview where up=@id; insert into media_preview(up,image) values(@id, @img); ";

            MemoryStream thumb = GetImage(file_id, param1, 256);
            byte[] pic_arr = new byte[thumb.Length];
            thumb.Position = 0;
            thumb.Read(pic_arr, 0, pic_arr.Length);

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", file_id);
                    cmd.Parameters.AddWithValue("@img", pic_arr);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        static void GetVideoInfo(long task_number, long file_id, string param1)
        {
            string str = GetXML(file_id, param1);
            str = str.Replace("'", "''");
            long duration = GetDuration(str);
            string sql = "delete from files_json where up="+file_id+"; INSERT INTO files_json(up, xml, duration) VALUES(" + file_id + ", N'" + str + "'," + duration + ") ";
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
        }
        static void GetVideoThumb(long task_number, long id, string param1, string param2)
        {
            int offset = 60;
            int duration;
            string lockerID = Environment.TickCount.ToString("X8");

            if (!File.Exists(param1)) return;

            Console.ForegroundColor = ConsoleColor.Gray; Console.WriteLine("Task: " + task_number + "  "+id+": "+ param1);

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand("select files_json.duration from files_json where up=" + id, conn))
                {
                    duration = (int)cmd.ExecuteScalar();
                }

                using (var cmd = new NpgsqlCommand("delete from video_thumbnails where up="+id, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                Console.ForegroundColor = ConsoleColor.White; Console.WriteLine("Task: " + task_number + "  Duration: " + duration);
                long minPerThumb;
                if (duration < 60) offset = 1;
                if (duration < offset) duration = offset;
                if (duration - offset < 3600) minPerThumb = ((duration - offset) / 15) + 1;
                else minPerThumb = 4 * 60;
                string time, outfile;

                for (int t = 0; (t * minPerThumb + offset - 1 <= duration) && t < 500; t++)
                {
                    TimeSpan timespan = TimeSpan.FromSeconds(t * minPerThumb + offset);
                    time = timespan.ToString(@"hh\:mm\:ss");
                    outfile = tempPath + "mf" + id + Environment.TickCount + ".jpeg";
                    try
                    {
                        if (File.Exists(outfile)) File.Delete(outfile);
                    }
                    catch (Exception E)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Exception: " + E.InnerException.Message + " " + E.Message);
                    };

                    Process process = new Process();
                    process.StartInfo.FileName = "ffmpeg";
                    process.StartInfo.Arguments = "  -nostdin -v quiet -hide_banner -ss " + time + " -i \"" + param1 + "\" " + " -vframes:v 1 \"" + outfile + "\"";
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                    process.WaitForExit();
                    process.Close();

                    try
                    {
                        MemoryStream ms = GetImage(0, outfile);
                        SaveThumb(id, t * minPerThumb + offset, ms);
                        File.Delete(outfile);
                        Console.ForegroundColor = ConsoleColor.White; Console.WriteLine("Pos: " + time + "  size:" + ms.Length);
                    }
                    catch (Exception E)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Exception: " + E.Message + " " + param1);
                    };

                }
                conn.Close();
            }
        }
        static void FFMPEGConvert(long task_number, long id, string param1, string param2)
        {
            int offset = 60;
            int duration;
            string lockerID = Environment.TickCount.ToString("X8");

            Console.ForegroundColor = ConsoleColor.Gray; Console.WriteLine("Task: " + task_number + "  " + id + ": " + param1);

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                /*using (var cmd = new NpgsqlCommand("select files_json.duration from files_json where up=" + id, conn))
                {
                    duration = (int)cmd.ExecuteScalar();
                }*/
                conn.Close();
            }
            Process process = new Process();


            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = param2;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("delete from tasks_standard_output where process=" + process.Id +
                    " ; insert into tasks_standard_output(up, process) values(" + task_number + ", " + process.Id + ");", conn)) 
                {
                    cmd.ExecuteNonQuery();
                };
                conn.Close();
            }
            process.WaitForExit();
            process.Close();

        }
        static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            try
            {
                output_messages.Enqueue(new KeyValuePair<int, string>(((Process)sendingProcess).Id, outLine.Data));
            }
            catch { };
        }

        static void output_post()
        {
            KeyValuePair<int, string> data;
            for (;;)
            {
                if (!output_messages.TryDequeue(out data))
                {
                    Thread.Sleep(1000);
                    continue;
                }
                try
                {
                    using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                    {
                        string sql = "update tasks_standard_output set text=N'" + data.Value.Replace("'", "''") + "' where process=" + data.Key + " ;";
                        conn.Open();
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.ExecuteNonQuery();
                        };
                        conn.Close();
                    }
                }
                catch { };
            }
        }

        static ImageCodecInfo myImageCodecInfo;
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
        static void SaveThumb(long id, long position, MemoryStream thumb)
        {
            byte[] pic_arr = new byte[thumb.Length];
            thumb.Position = 0;
            thumb.Read(pic_arr, 0, pic_arr.Length);
            string sql = "insert into video_thumbnails(up,pos,image) values(" + id + ","+ position+", @img);";
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand("delete from video_thumbnails where up=" + id + " and pos="+ position+";", conn))
                {
                    cmd.ExecuteNonQuery();
                }


                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@img", pic_arr);
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }

        }
        static MemoryStream GetGrayscaled(Image img, int __imageSize = 512, bool __notProportional = false)
        {
            string ret = "";
            Image thumb;
            int NewWidth, NewHeight;

            if (__notProportional)
            {
                NewWidth = __imageSize;
                NewHeight = __imageSize;
            }
            else
            {
                NewWidth = img.Width;
                if (img.Width > __imageSize) NewWidth = __imageSize;
                NewHeight = img.Height * NewWidth / img.Width;
                if (NewHeight > __imageSize)
                {
                    NewWidth = img.Width * __imageSize / img.Height;
                    NewHeight = __imageSize;
                }
            }

            Bitmap newImage = new Bitmap(NewWidth, NewHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(img, new Rectangle(0, 0, NewWidth, NewHeight));
            }
            thumb = newImage;

            // thumb = img.GetThumbnailImage(NewWidth, NewHeight, null, IntPtr.Zero);
            MemoryStream memStream = new MemoryStream(); //thumb.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            for (long L = 75; L > 10; L -= 10)
            {
                EncoderParameter myEncoderParameter;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                System.Drawing.Imaging.Encoder myEncoder;
                myEncoder = System.Drawing.Imaging.Encoder.Quality;
                myEncoderParameter = new EncoderParameter(myEncoder, 75L);
                myEncoderParameters.Param[0] = myEncoderParameter;

                thumb.Save(memStream, GetEncoderInfo("image/jpeg"), myEncoderParameters);
                if (memStream.Length < 65500) break;
                memStream.SetLength(0);
            }
            img.Dispose();

            return memStream;
        }
        static MemoryStream GetImage(long id, string name, int __imageSize = 512, bool __notProportional=false)
        {
            string ret = "";
            Image img;
            Image thumb;
            int NewWidth, NewHeight;

            img = Image.FromFile(name);
            if (__notProportional)
            {
                NewWidth = __imageSize;
                NewHeight = __imageSize;
            }
            else
            {
                NewWidth = img.Width;
                if (img.Width > __imageSize) NewWidth = __imageSize;
                NewHeight = img.Height * NewWidth / img.Width;
                if (NewHeight > __imageSize)
                {
                    NewWidth = img.Width * __imageSize / img.Height;
                    NewHeight = __imageSize;
                }
            }

            Bitmap newImage = new Bitmap(NewWidth, NewHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(img, new Rectangle(0, 0, NewWidth, NewHeight));
            }
            thumb = newImage;

            MemoryStream memStream = new MemoryStream();
            for (long L = 75; L > 10; L -= 10)
            {
                EncoderParameter myEncoderParameter;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                System.Drawing.Imaging.Encoder myEncoder;
                myEncoder = System.Drawing.Imaging.Encoder.Quality;
                myEncoderParameter = new EncoderParameter(myEncoder, 75L);
                myEncoderParameters.Param[0] = myEncoderParameter;

                thumb.Save(memStream, GetEncoderInfo("image/jpeg"), myEncoderParameters);
                if (memStream.Length < 65500) break;
                memStream.SetLength(0);
            }
            img.Dispose();

            return memStream;
        }
        static void VideoWorker()
        {
            long task;
            for (;;)
            {
                if (!TaskList.TryDequeue(out task))
                {
                    Thread.Sleep(1000);
                    continue;
                }

                try
                {
                    //GetVideoThumb(task);
                }
                catch { };

            }
        }

        static void UpdateDuration()
        {
            long id = 384;
            string name = "";
            string xml;
            long offset = 60;
            string lockerID = Environment.TickCount.ToString("X8");
            string sql;// = "  select * from (select up, get_full_path(up) as full_path from files_json where xml is null) f where full_path like '%SMTHi7%' limit 100;";

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                sql = "select up, xml from files_json where duration is null";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        id = reader.GetInt64(0);
                        xml = reader.GetString(1);
                        try
                        {
                            XmlDocument xmldoc = new XmlDocument();
                            xmldoc.LoadXml(xml.Replace("\u0008", "").Replace("\u001A", "").Replace("\x00", ""));
                            long duration = long.Parse(xmldoc["ffprobe"]["format"].Attributes["duration"].Value.Split('.')[0]);
                            using (NpgsqlConnection conn2 = new NpgsqlConnection(connectionString))
                            {
                                conn2.Open();
                                using (var cmd2 = new NpgsqlCommand("update files_json set duration=" + duration + " where up=" + id, conn2))
                                    cmd2.ExecuteNonQuery();
                            }
                            Console.WriteLine("ID: " + id + "  Duration:"+duration);
                        }
                        catch { };

                    }
                    reader.Close();
                }
            }
        }

        /**********************************************************************************************************************************************************/
        static void Main(string[] args)
        {
            myImageCodecInfo = GetEncoderInfo("image/jpeg");
            tempPath = Path.GetTempPath();
            Console.WriteLine("Temp directory: " + tempPath);
            string addedTasks;

            new Thread(() => output_post()).Start();

            for (int i = 0; i <6; i++)
                new Thread(new ParameterizedThreadStart(TaskWorker)).Start(i);

            for (;;)
            {
                if (!TaskList.IsEmpty)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
                {
                    int count = 0;
                    conn.Open();
                    addedTasks = "";
                    string sql = "select n, file_id, param1 from "+
                        "(select* from tasks where depend_on is null union select* from tasks ts1 where(select task_state from tasks ts2 where ts1.depend_on = ts2.n) = 2) tsk" +
                        " where  task_type in (1, 2, 3, 4, 6, 7, 8, 9) and task_state = 1 and param1 like '%" + System.Environment.MachineName + "%' limit 10";
                    //"select n, file_id, param1 from tasks where  task_type in (1, 2, 3, 4, 6) and state = 1 and param1 like '%" + System.Environment.MachineName + "%' limit 10";
                    long task_n;
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        NpgsqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            task_n = reader.GetInt64(0);
                            TaskList.Enqueue(task_n);
                            addedTasks += "," + task_n;
                            count++;
                        }
                        reader.Close();
                    }
                    if (!string.IsNullOrEmpty(addedTasks))
                        using (var cmd = new NpgsqlCommand("update tasks set task_state=4 where n in (0"+addedTasks+")", conn))
                        { cmd.ExecuteNonQuery(); };
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Added "+count+" tasks.");
                    if (count == 0) Thread.Sleep(5000);
                }
            }
        }
        static long GetDuration(string xml)
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(xml.Replace("\u0008", "").Replace("\u001A", "").Replace("\x00", ""));
            long duration = long.Parse(xmldoc["ffprobe"]["format"].Attributes["duration"].Value.Split('.')[0]);
            return duration;
        }

        static string GetXML(long id, string name)
        {
            string ret="";
            //string tempFile = tempPath + "" + Environment.TickCount + ".json";
            Process process = new Process();
            process.StartInfo.FileName = "ffprobe";
            process.StartInfo.Arguments = " -v quiet -hide_banner -print_format xml -show_format -show_streams -i \"" + name + "\" ";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            process.WaitForExit(5000);
            StreamReader OutputReader = process.StandardOutput;
            ret = OutputReader.ReadToEnd();
            process.Close();
            return ret;
        }

        static void SaveXML(long id, string name)
        {
            string sql = "update files_json set xml=N'" + name + "' where up=" + id;
                //"insert into files_json(up,data) values("+id+","+"'"+name+"');";
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
                conn.Close();
            }

        }
        static void UpdateJSON()
        {
            long id = 0;
            string name="";
            string json;
            string sql = "  select * from (select up, get_full_path(up) as full_path from files_json where xml is null) f where full_path like '%SMTHi7%' limit 100;";

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    NpgsqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        id = reader.GetInt64(0);
                        if (SelectedJobs.Contains(id)) continue;
                        name = reader.GetString(1);
                        Console.WriteLine(id + "   " + name);
                        try
                        {
                            ExploreJobList.Enqueue(new ProbeJob { ID = id, filename = name });
                            SelectedJobs.Add(id);
                            //json = GetJSON(id, name);
                            //json = json.Replace("'", "''");//.Replace("\r\n", "\n").Replace("\\\'","''");
                            //SaveJSON(id, json);
                            //Console.WriteLine(json);
                        }
                        catch (Exception E) { Console.WriteLine("Exception"); };
                    }
                    reader.Close();
                }
                conn.Close();
            }

        }


    }

}

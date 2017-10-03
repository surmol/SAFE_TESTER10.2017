using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        bool started = false;
        bool clicked = false;
        private DataSet ds = new DataSet();
        private DataTable dt = new DataTable();
        public int last_serial_number;
        public int amount_of_cameras;
        public int time_refresh;
        public Form1()
        {


            InitializeComponent();

            get_camera_list();


        }


        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        public static int RandomNumber(int min, int max)
        {
            lock (syncLock)
            { // synchronize
                return random.Next(min, max);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {


            get_camera_list();

        }

        public void get_camera_list()
        {
            dataGridView1.DataSource = null;
            dataGridView1.Refresh();
            try
            {
                // PostgeSQL-style connection string
                string connstring = String.Format("Server=localhost;Port=5432;User Id=postgres;Password=21040911;Database=SAFE;");
                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();
                // quite complex sql statement
                string sql = "SELECT * FROM public.\"Cameras\" ORDER BY \"Id\" DESC";
                // data adapter making request from our connection
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, conn);
                // i always reset DataSet before i do
                // something with it.... i don't know why :-)
                ds.Reset();
                // filling DataSet with result from NpgsqlDataAdapter
                da.Fill(ds);
                // since it C# DataSet can handle multiple tables, we will select first
                dt = ds.Tables[0];
                // connect grid to DataTable
                dataGridView1.DataSource = dt;
                // since we only showing the result we don't need connection anymore
                conn.Close();
            }
            catch (Exception msg)
            {
                // something went wrong, and you wanna know why
                MessageBox.Show(msg.ToString());
                throw;
            }
        }

        public async void send_camera(string cameraSerialNumber, string timestamp)
        {

            int pose_random = RandomNumber(-1, 11); // creates a number between 1 and 12
            int dangerlevel_random = RandomNumber(-1, 5); // creates a number between 1 and 6            
            var pose = pose_random.ToString();
            var dangerlevel = dangerlevel_random.ToString();
            HttpClient httpClient = new HttpClient();
            MultipartFormDataContent camera = new MultipartFormDataContent();

            camera.Add(new StringContent(cameraSerialNumber), "cameraSerialNumber");
            camera.Add(new StringContent(timestamp), "timestamp");
            camera.Add(new StringContent(pose), "pose");
            camera.Add(new StringContent(dangerlevel), "dangerlevel");
            // form.Add(new ByteArrayContent(imagebytearraystring, 0, imagebytearraystring.Count()), "profile_pic", "hello1.jpg");

            HttpResponseMessage response = await httpClient.PostAsync("http://192.168.1.56:8089/SubmitFrame", camera);

            response.EnsureSuccessStatusCode();
            httpClient.Dispose();
            string sd = response.Content.ReadAsStringAsync().Result;
        }

        ManualResetEvent CameraPoschange = new ManualResetEvent(false);

        public void send_cameras()
        {

            for (int i = 0; ; i++)
            {

                int amount = get_amount_of_cameras();

                DateTime date = DateTime.Now;
                var unix_time = (long)(date - new DateTime(1970, 1, 1)).TotalMilliseconds;
                var timestamp = unix_time.ToString();

                for (int n = 1; n <= amount; n++)
                {
                    if (clicked)
                    {
                        if (IsHandleCreated)
                            Invoke(new EventHandler(delegate
                            {
                                current_camera_label.Text = n.ToString();

                            }));




                        send_camera(n.ToString(), timestamp);
                        
                    }
                    CameraPoschange.WaitOne(time_refresh);
                }





            }


        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (started && clicked)
            {
                clicked = false;
                run_test_button.Text = "RUN TEST";
            }
            else if (!started && !clicked)
            {
                Thread x = new Thread(send_cameras);
                x.Start();
                started = true;
                clicked = true;
                run_test_button.Text = "STOP TEST";
            }
            else if (started && !clicked)
            {
                clicked = true;
                run_test_button.Text = "STOP TEST";
            }

        }
        public int get_amount_of_cameras()
        {

            try
            {
                // PostgeSQL-style connection string
                string connstring = String.Format("Server=localhost;Port=5432;User Id=postgres;Password=21040911;Database=SAFE;");
                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();
                // quite complex sql statement
                string sql = "SELECT count(*) FROM public.\"Cameras\"";

                NpgsqlCommand command = new NpgsqlCommand(sql, conn);
                int val;
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    amount_of_cameras = Int32.Parse(reader[0].ToString());

                }
                //MessageBox.Show(amount_of_cameras.ToString());
                conn.Close();
                return amount_of_cameras;
            }
            catch (Exception msg)
            {
                // something went wrong, and you wanna know why
                MessageBox.Show(msg.ToString());
                throw;
            }

        }
        public int get_last_serial_number()
        {

            try
            {
                // PostgeSQL-style connection string
                string connstring = String.Format("Server=localhost;Port=5432;User Id=postgres;Password=21040911;Database=SAFE;");
                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();
                // quite complex sql statement
                string sql = "SELECT \"Id\",\"SerialNumber\" FROM public.\"Cameras\" ORDER BY \"Id\" DESC LIMIT 1";

                NpgsqlCommand command = new NpgsqlCommand(sql, conn);
                int val;
                NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    last_serial_number = Int32.Parse(reader[1].ToString());

                }
                //MessageBox.Show(last_serial_number.ToString());
                conn.Close();
                return last_serial_number;
            }
            catch (Exception msg)
            {
                // something went wrong, and you wanna know why
                MessageBox.Show(msg.ToString());
                throw;
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            int amount = Convert.ToInt32(numericUpDown1.Value);
            //get_last_serial_number();
            add_cameras(amount);
            get_camera_list();
        }

        public void add_cameras(int amount)
        {
            //int start_serial = get_last_serial_number();
            int start_serial = 0;
            for (int i = start_serial + 1; i <= start_serial + amount; i++)
            {

                try
                {
                    int d = 0;
                    // PostgeSQL-style connection string
                    string connstring = String.Format("Server=localhost;Port=5432;User Id=postgres;Password=21040911;Database=SAFE;");
                    NpgsqlConnection conn = new NpgsqlConnection(connstring);
                    conn.Open();
                    NpgsqlCommand cmd = new NpgsqlCommand("insert into public.\"Cameras\" (\"Description\",\"Name\",\"CameraStatus\",\"SerialNumber\",\"FramesMode\") values(:Description, :Name,:CameraStatus,:SerialNumber,:FramesMode)", conn);
                    cmd.Parameters.Add(new NpgsqlParameter("Description", "Desc" + i.ToString()));
                    cmd.Parameters.Add(new NpgsqlParameter("Name", "Cam" + i.ToString()));
                    cmd.Parameters.Add(new NpgsqlParameter("CameraStatus", 1));
                    cmd.Parameters.Add(new NpgsqlParameter("SerialNumber", i));
                    cmd.Parameters.Add(new NpgsqlParameter("FramesMode", d));
                    cmd.ExecuteNonQuery();
                    //int Id = Convert.ToInt32(cmd.ExecuteScalar());
                    //MessageBox.Show(Id.ToString());
                    conn.Close();
                }
                catch (Exception msg)
                {
                    // something went wrong, and you wanna know why
                    MessageBox.Show(msg.ToString());
                    throw;
                }


            }

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            int x = get_amount_of_cameras();
        }


        private void closeForm(object sender, FormClosedEventArgs e)
        {
            try
            {
                this.Invoke(new MethodInvoker(delegate { this.Close(); }));

            }
            catch { }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (IsHandleCreated)
                Invoke(new EventHandler(delegate
                {
                    time_refresh = Convert.ToInt32(numericUpDown2.Value);

                }));

        }



        public void add_frames(int amount)
        {
            if (IsHandleCreated)
                Invoke(new EventHandler(delegate
                {
                   label1.Text = "TEST STARTED !!!";

                }));
            
            //int start_serial = get_last_serial_number();
            int start_serial = 0;
            for (int i = 0; i <= amount; i++)
            {
                for (int g = 73; g <= 82; g++)
                {
                    try
                    {

                        int pose_random = RandomNumber(-1, 11); // creates a number between 1 and 12
                        int dangerlevel_random = RandomNumber(-1, 5); // creates a number between 1 and 6            
                        var pose = pose_random.ToString();
                        var dangerlevel = dangerlevel_random.ToString();
                        DateTime date = DateTime.Now;
                        var unix_time = (long)(date - new DateTime(1970, 1, 1)).TotalMilliseconds;
                        var timestamp = unix_time.ToString();
                        int d = 0;
                        // PostgeSQL-style connection string
                        string connstring = String.Format("Server=localhost;Port=5432;User Id=postgres;Password=21040911;Database=SAFE;");
                        NpgsqlConnection conn = new NpgsqlConnection(connstring);
                        conn.Open();
                        NpgsqlCommand cmd = new NpgsqlCommand("insert into public.\"Frames\" (\"CameraId\",\"DangerLevel\",\"Pose\",\"Timestamp\") values(:CameraId, :DangerLevel,:Pose,:Timestamp)", conn);
                        cmd.Parameters.Add(new NpgsqlParameter("CameraId", g));
                        cmd.Parameters.Add(new NpgsqlParameter("DangerLevel", dangerlevel_random));
                        cmd.Parameters.Add(new NpgsqlParameter("Pose", pose_random));
                        cmd.Parameters.Add(new NpgsqlParameter("Timestamp",DateTime.Now));

                        cmd.ExecuteNonQuery();
                        //int Id = Convert.ToInt32(cmd.ExecuteScalar());
                        //MessageBox.Show(Id.ToString());
                        conn.Close();
                    }
                    catch (Exception msg)
                    {
                        // something went wrong, and you wanna know why
                        MessageBox.Show(msg.ToString());
                        throw;
                    }
                }



            }

            if (IsHandleCreated)
                Invoke(new EventHandler(delegate
                {
                    label1.Text = "test finished";
                }));
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int ss = Convert.ToInt32(numericUpDown3.Value);
            add_frames(ss);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing.Imaging;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using System.Windows.Media.Imaging;
using System.Drawing;

using AForge;
using AForge.Controls;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using Size = System.Drawing.Size;

namespace OperateCamera
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private VideoFileWriter writer;     //写入到视频
        private bool is_record_video = false;   //是否开始录像
        private bool synchronization = false;   //同步显示
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            try
            {
                // 枚举所有视频输入设备
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                writer = new VideoFileWriter();

                if (videoDevices.Count == 0)
                    throw new ApplicationException();

                foreach (FilterInfo device in videoDevices)
                {
                    tscbxCameras.Items.Add(device.Name);
                }

                tscbxCameras.SelectedIndex = 0;

            }
            catch (ApplicationException)
            {
                tscbxCameras.Items.Add("No local capture devices");
                videoDevices = null;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            CameraConn();
           
        }
        //连接摄像头
        private void CameraConn()
        {
            VideoCaptureDevice videoSource = new VideoCaptureDevice(videoDevices[tscbxCameras.SelectedIndex].MonikerString);
            videoSource.DesiredFrameSize = new System.Drawing.Size(640, 480);//2.2.5 无效
            videoSource.DesiredFrameRate = 1;
            videoSource.NewFrame += new NewFrameEventHandler(show_video);

            videoSourcePlayer.VideoSource = videoSource;
            videoSourcePlayer.Start();
        }

        //关闭摄像头
        private void btnClose_Click(object sender, EventArgs e)
        {
            videoSourcePlayer.SignalToStop();
            videoSourcePlayer.WaitForStop();
        }

        //主窗体关闭
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            btnClose_Click(null, null);
        }

        //拍照
        private void Photograph_Click(object sender, EventArgs e)
        {
            try
            {
                for(int i=1;i<=1;i++)
                {
                    if (videoSourcePlayer.IsRunning)
                    {
                        BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                        videoSourcePlayer.GetCurrentVideoFrame().GetHbitmap(),
                                        IntPtr.Zero,
                                         Int32Rect.Empty,
                                        BitmapSizeOptions.FromEmptyOptions());
                        PngBitmapEncoder pE = new PngBitmapEncoder();
                        pE.Frames.Add(BitmapFrame.Create(bitmapSource));
                        string picName = GetImagePath() + "\\" +  GetImageName(6) + ".jpg";
                        if (File.Exists(picName))
                        {
                            File.Delete(picName);
                        }
                        using (Stream stream = File.Create(picName))
                        {
                            pE.Save(stream);
                        }

                        Bitmap bitmap = videoSourcePlayer.GetCurrentVideoFrame();    //获取到一帧图像
                        pictureBox1.Image = Image.FromHbitmap(bitmap.GetHbitmap());

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("摄像头异常：" + ex.Message);
            }
        }

        private string GetImagePath()
        {
            string personImgPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)
                         + Path.DirectorySeparatorChar.ToString() + "PersonImg";
            if (!Directory.Exists(personImgPath))
            {
                Directory.CreateDirectory(personImgPath);
            }

            return personImgPath;
        }


        private string GetImageName(int Length)
        {
            System.Text.StringBuilder newRandom = new System.Text.StringBuilder(62);
            Random rd = new Random();
            for (int i = 0; i < Length; i++)
            {
                newRandom.Append(constant[rd.Next(62)]);
            }
            return newRandom.ToString();
        }

        private static char[] constant = 
        {
            '0','1','2','3','4','5','6','7','8','9',
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
        };

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            System.Drawing.Point mouseLoction = pictureBox1.PointToClient(Control.MousePosition);
            String mousePlace = "$MouseMove:" + mouseLoction.X.ToString() + "," + mouseLoction.Y.ToString();
            label2.Text = mousePlace;
        }

        double x1, x2, y1, y2;

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            String MouseMsg = "$MouseEvent||";
            System.Drawing.Point mouseLoction = pictureBox1.PointToClient(Control.MousePosition);
            switch (e.Button)
            {
                case MouseButtons.Left:
                    MouseMsg = "LeftMouseDown ||" + mouseLoction.X.ToString() + "||" + mouseLoction.Y.ToString() + "||";
                    x1 = mouseLoction.X;
                    y1 = mouseLoction.Y;
                    break;
                case MouseButtons.Right:
                    MouseMsg = "RightMouseDown ||" + mouseLoction.X.ToString() + "||" + mouseLoction.Y.ToString() + "||";
                    x2 = mouseLoction.X;
                    y2 = mouseLoction.Y;
                    break;
                default:
                    break;
            }

            //Return Math.Sqrt((x1 - x2) ^ 2 + (y1 - y2) ^ 2)
            label3.Text = MouseMsg + " || L:" + Math.Sqrt(Math.Pow((x1 - x2), 2) + Math.Pow((y1 - y2), 2)) + " >>> " + videoDevices[tscbxCameras.SelectedIndex].MonikerString;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //timer1.Enabled = !timer1.Enabled;
            synchronization = !synchronization;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (videoSourcePlayer.IsRunning)
                {
                    BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    videoSourcePlayer.GetCurrentVideoFrame().GetHbitmap(),
                                    IntPtr.Zero,
                                        Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                    PngBitmapEncoder pE = new PngBitmapEncoder();
                    pE.Frames.Add(BitmapFrame.Create(bitmapSource));
                    string picName = GetImagePath() + "\\" + "On-line" + ".jpg";
                    if (File.Exists(picName))
                    {
                        File.Delete(picName);
                    }
                    using (Stream stream = File.Create(picName))
                    {
                        pE.Save(stream);
                    }

                    pictureBox1.ImageLocation = picName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("摄像头异常：" + ex.Message);
            }
        }

        private void videoSourcePlayer_MouseDown(object sender, MouseEventArgs e)
        {
            String MouseMsg = "$MouseEvent:";
            System.Drawing.Point mouseLoction = videoSourcePlayer.PointToClient(Control.MousePosition);
            switch (e.Button)
            {
                case MouseButtons.Left:
                    MouseMsg = "LeftMouseDown :" + mouseLoction.X.ToString() + "||" + mouseLoction.Y.ToString();
                    x1 = mouseLoction.X;
                    y1 = mouseLoction.Y;
                    label3.Text = MouseMsg;
                    break;
                case MouseButtons.Right:
                    MouseMsg = "RightMouseDown :" + mouseLoction.X.ToString() + "||" + mouseLoction.Y.ToString();
                    x2 = mouseLoction.X;
                    y2 = mouseLoction.Y;
                    label4.Text = MouseMsg;
                    break;
                default:
                    break;
            }

            //Return Math.Sqrt((x1 - x2) ^ 2 + (y1 - y2) ^ 2)
            label5.Text = "Distance :" + Math.Sqrt(Math.Pow((x1 - x2), 2) + Math.Pow((y1 - y2), 2));
        }

        private void videoSourcePlayer_MouseMove(object sender, MouseEventArgs e)
        {
            System.Drawing.Point mouseLoction = videoSourcePlayer.PointToClient(Control.MousePosition);
            String mousePlace = "$MouseMove:" + mouseLoction.X.ToString() + "," + mouseLoction.Y.ToString();
            label2.Text = mousePlace;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Bitmap bitmap = this.videoSourcePlayer.GetCurrentVideoFrame();
            bitmap.Save(GetImagePath() + "\\" + "img.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        private void button3_Click(object sender, EventArgs e)
        {            
            if (is_record_video)
            {
                is_record_video = false;
                //synchronization = false;

                writer.Close();

                button3.BackColor = Color.White;
                button3.Text = "录制";
            }
            else {
                int width = 640;    //录制视频的宽度
                int height = 480;   //录制视频的高度
                int fps = 8;

                //创建一个视频文件
                if (videoSourcePlayer.IsRunning)
                {

                    writer.Open(GetImagePath() + "\\" + GetImageName(6) + ".avi", width, height, fps, VideoCodec.MPEG4);

                    is_record_video = true;
                    //synchronization = true;

                    button3.BackColor = Color.Red;
                    button3.Text = "结束录制";
                }
            }
        }

        //新帧的触发函数
        private void show_video(object sender, NewFrameEventArgs eventArgs)
        {
             Bitmap bitmap = eventArgs.Frame;

             if (synchronization)
             {
                 pictureBox1.Image = Image.FromHbitmap(bitmap.GetHbitmap());
             }

            if (is_record_video)
            {
                writer.WriteVideoFrame(bitmap);
                //bitmap.Dispose();
            }
        }
    }
}

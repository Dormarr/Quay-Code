﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.XPhoto;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.XImgproc;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using Image = System.Windows.Controls.Image;

namespace Quay_Code
{
    public partial class Detect
    {
        public void IdentifyFromVideo(Image webcamImg)
        {

            bool pause = false;

            try
            {
                VideoCapture vid = new(0, VideoCapture.API.DShow);
                vid.Set(CapProp.FrameHeight, 480);
                vid.Set(CapProp.FrameWidth, 640);

                try
                {
                    

                    while (!pause)
                    {
                        Mat frame = new();
                        vid.Read(frame);

                        //CvInvoke.Imshow("Raw Input", frame);
                        webcamImg.Source = frame.ToImageSource();

                        if (frame != null)
                        {

                            //Mat[] outputArray = FindSquares(frame);
                            //CvInvoke.Imshow("Human Vision", outputArray[1]);
                        }
                    }
                    //key to pause


                }
                catch(Exception exc)
                {
                    MessageBox.Show($"Error Processing Frame: {exc.Message}");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Error Initialising Webcam: {ex.Message}");
            }
        }

        public Mat[] FindSquares(Mat input)
        {
            Mat frameGray = new();
            Mat threshMat = new();
            //MCvScalar sclr = new MCvScalar(85, 255, 55);
            CvInvoke.CvtColor(input, frameGray, ColorConversion.Bgr2Gray);
            CvInvoke.AdaptiveThreshold(frameGray, threshMat, 255, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, 11, 5);

            VectorOfVectorOfPoint cnts = new VectorOfVectorOfPoint();

            CvInvoke.FindContours(threshMat, cnts, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            List<PointF> cand = new List<PointF>();
            VectorOfPointF cnt = new VectorOfPointF();
            VectorOfPointF cnt2 = new VectorOfPointF();

            for(int i = 0; i < cnts.Size; i++)
            {
                CvInvoke.ApproxPolyDP(cnts[i], cnt, 0.005 * CvInvoke.ArcLength(cnts[i], true), true);

                if(cnt.Size !=4 || CvInvoke.ContourArea(cnt) < 200 || !CvInvoke.IsContourConvex(cnt))
                {
                    continue;
                }

                CvInvoke.CornerSubPix(threshMat, cnt, new Size(5, 5), new Size(-1, -1), new MCvTermCriteria(30, 0.01));

                cnt2 = OrderContour(cnt);

                //Mark Corner
                CvInvoke.Circle(threshMat, PointFToPoint(cnt2[0]), 4, new MCvScalar(0, 0, 255), 7);

                //Draw Mid Point
                int cx = (int)(cnt2[0].X + cnt2[1].X + cnt2[2].X + cnt2[3].X) / 4;
                int cy = (int)(cnt2[0].Y + cnt2[1].Y + cnt2[2].Y + cnt2[3].Y) / 4;
                CvInvoke.Circle(threshMat, new Point(cx, cy), 4, new MCvScalar(255, 50, 200));

                PointF[] pointsF = ConvertVectorOfPointToPointFArray(cnt2);

                for (int j = 0; j < pointsF.Length; j++)
                {
                    cand.Add(pointsF[j]);
                }
            }

            DrawContourFloat(threshMat, cand, new MCvScalar(100, 255, 100));

            if(cnt2.Size == 4)
            {
                GetContourBits(input, cnt2, 1024);
                DrawContourFloat(input, cand, new MCvScalar(0, 255, 0));
            }

            Mat[] mats = new Mat[2];
            mats[0] = threshMat;
            mats[1] = input;

            return mats;
        }

        VectorOfPointF OrderContour(VectorOfPointF cntV)
        {
            PointF[] cnt = ConvertVectorOfPointToPointFArray(cntV);

            float cx = (cnt[0].X + cnt[1].X + cnt[2].X + cnt[3].X) / 4.0f;
            float cy = (cnt[0].Y + cnt[1].Y + cnt[2].Y + cnt[3].Y) / 4.0f;

            // IMPORTANT! We assume the contour points are counter-clockwise (as we use EXTERNAL contours in findContours)
            if (cnt[0].X <= cx && cnt[0].Y <= cy)
            {
                Swap(ref cnt[1], ref cnt[3]);
            }
            else
            {
                Swap(ref cnt[0], ref cnt[1]);
                Swap(ref cnt[2], ref cnt[3]);
            }
            VectorOfPointF send = new VectorOfPointF(cnt);
            return send;
        }

        void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        void DrawContourFloat(Mat img, List<PointF> cnt, MCvScalar color)
        {
            for (int i = 0; i < cnt.Count; ++i)
            {
                PointF from = cnt[i];
                PointF to = cnt[(i + 1) % cnt.Count];
                Line ln = new Line();
                CvInvoke.Line(img, PointFToPoint(from), PointFToPoint(to), color, 2);

                //Redefine when this should be drawn, because it looks like madness at the moment.
            }
        }

        Point PointFToPoint(PointF pointF)
        {
            return new Point((int)Math.Round(pointF.X), (int)Math.Round(pointF.Y));
        }

        PointF[] ConvertVectorOfPointToPointFArray(VectorOfPointF vectorOfPoint)
        {
            PointF[] pointFArray = new PointF[vectorOfPoint.Size];

            for (int i = 0; i < vectorOfPoint.Size; i++)
            {
                PointF point = vectorOfPoint[i];
                pointFArray[i] = new PointF(point.X, point.Y);
            }

            return pointFArray;
        }

        void GetContourBits(Mat image, VectorOfPointF cnt, int bits)
        {
            int pixelLen = (int)Math.Sqrt(bits);

            PointF[] corners = new PointF[4] { new PointF(0, 0), new PointF(bits, 0), new PointF(bits, bits), new PointF(0, bits) };
            VectorOfPointF cornerV = new VectorOfPointF(corners);

            Mat m = CvInvoke.GetPerspectiveTransform(cnt, cornerV);
            Mat binary = new();
            CvInvoke.WarpPerspective(image, binary, m, new Size(bits, bits));
            CvInvoke.Threshold(binary, binary, 64, 255, ThresholdType.Binary);

            int scaleFactor = DetermineSize(binary);

            if (scaleFactor != 100)
            {
                CvInvoke.PutText(image, scaleFactor.ToString(), PointFToPoint(cnt[0]), FontFace.HersheyPlain, 2, new MCvScalar(0, 0, 0));
                DrawFullGrid(binary, scaleFactor, scaleFactor, new MCvScalar(0, 0, 255, 100));
                //binary.Save("C:\\Users\\Ryan\\Desktop\\Software Testing Ground\\Spam\\bin" + DateTime.Now.Ticks + ".png");

                //NOW WE HAVE SIZE, WE CAN JUST DECODE.
                //add header reading ability.

                /*
                int dataCount = ReadHeader(binary, scaleFactor)[0];
                string readUntrunc = ReadCode(binary, scaleFactor);

                string read = TruncateData(readUntrunc, dataCount);

                CvInvoke.PutText(image, " " + read, PointFToPoint(cnt[2]), FontFace.HersheyPlain, 1.2, new MCvScalar(0, 0, 0));
                */
            }
        }

        void DrawFullGrid(Mat img, int rows, int cols, MCvScalar color)
        {
            int cellW = img.Cols / cols;
            int cellH = img.Rows / rows;

            for (int i = 1; i < rows; i++)
            {
                int y = i * cellH;
                CvInvoke.Line(img, new Point(0, y), new Point(img.Cols, y), color, 4);
            }
            for (int i = 1; i < cols; i++)
            {
                int x = i * cellW;
                CvInvoke.Line(img, new Point(x, 0), new Point(x, img.Rows), color, 4);
            }
        }

        string TruncateData(string input, int dataLength)
        {
            string output = input.Substring(0, dataLength);

            return output;
        }

        static int DetermineSize(Mat image)
        {
            //Add a version of this that accounts for Black border AND white border (if on black background)

            if (CheckForSize(image, 64, 16))
            {
                return 12;
            }
            else if (CheckForSize(image, 46, 22))
            {
                return 18;
            }
            else if(CheckForSize(image, 36, 28))
            {
                return 24;
            }
            else if (CheckForSize(image, 28, 36))
            {
                return 32;
            }
            else
            {
                return 100;
            }
        }

        static bool CheckForSize(Mat image, int metric, int size)
        {
            int pixelLen = metric;
            List<char> chars = new List<char>();

            for (int r = 2; r <= 2; ++r)
            {
                for (int c = 0; c < size; ++c)
                {
                    int y = r * pixelLen + (pixelLen / 2);
                    int x = c * pixelLen + (pixelLen / 2);

                    if (image.GetRawData(y, x)[0] >= 128)
                    {
                        chars.Add('W');
                    }
                    else
                    {
                        chars.Add('B');
                    }
                }
            }
            StringBuilder charsToString = new StringBuilder();
            foreach (char c in chars)
            {
                charsToString.Append(c);
            }

            //size is adjusted because of border. So Q12 becomes 16 etc.

            string strArray = null;
            string str12 = "BWBWBWBWBWBWBBWB";
            string str18 = "BWBWBWBWBWBWBWBWBWBBWB";
            string str24 = "BWBWBWBWBWBWBWBWBWBWBWBWBBWB";
            string str32 = "BWBWBWBWBWBWBWBWBWBWBWBWBWBWBWBBBBWB";

            switch (size)
            {
                case 16:
                    strArray = str12;
                    break;
                case 22:
                    strArray = str18;
                    break;
                case 28:
                    strArray = str24;
                    break;
                case 36:
                    strArray = str32;
                    break;

            }
            if (strArray != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        int[] ReadHeader(Mat image, int sizeMetric)
        {

            (int,int)[] headerSlots = Coords.GetHeader(sizeMetric);

            List<string> rawHeader = CodeReader(headerSlots, sizeMetric, image);

            string[] dataCountArray = new string[] { (rawHeader[0] + rawHeader[1]), (rawHeader[2] + rawHeader[3]) };

            int[] ints = CustomBinary.Read4Bit(dataCountArray);



            int dataCountFinal = ints[1];

            if (ints[0] == 1)
            {
                dataCountFinal += 10;
            }
            else if (ints[0] == 2)
            {
                dataCountFinal += 20;
            }

            int[] finalInts = new int[] { dataCountFinal, 1 };

            return finalInts;
        }

        List<string> CodeReader((int, int)[] dataArray, int sizeMetric, Mat image)
        {
            byte[] blue = new byte[] { 255, 0, 0 };
            byte[] brightBlue = new byte[] { 255, 255, 0 };
            byte[] red = new byte[] { 0, 0, 255 };
            byte[] brightRed = new byte[] { 255, 0, 255 };
            byte[] yellow = new byte[] { 0, 255, 255 };
            byte[] black = new byte[] { 0, 0, 0 };
            byte[] white = new byte[] { 255, 255, 255 };

            List<string> rawRead = new List<string>();

            int pixelLen = (int)1024 / (sizeMetric + 4);

            for (int i = 0; i < dataArray.Length; i++)
            {
                int x = dataArray[i].Item1 * pixelLen + (pixelLen / 2);
                int y = dataArray[i].Item2 * pixelLen + (pixelLen / 2);
                byte[] rawData = image.GetRawData(y, x); //used to be x,y, but y,x seems to actually work.

                if (rawData[0] == black[0] && rawData[1] == black[1] && rawData[2] == black[2])
                {
                    rawRead.Add("11");
                }
                else if (rawData[0] == white[0] && rawData[1] == white[1] && rawData[2] == white[2])
                {
                    rawRead.Add("10");
                }
                else if (rawData[0] == blue[0] && rawData[1] == blue[1] && rawData[2] == blue[2])
                {
                    rawRead.Add("00");
                }
                else if (rawData[0] == brightBlue[0] && rawData[1] == brightBlue[1] && rawData[2] == brightBlue[2])
                {
                    rawRead.Add("00");
                }
                else if (rawData[0] == red[0] && rawData[1] == red[1] && rawData[2] == red[2])
                {
                    rawRead.Add("01");
                }
                else if (rawData[0] == brightRed[0] && rawData[1] == brightRed[1] && rawData[2] == brightRed[2])
                {
                    rawRead.Add("01");
                }
                else if (rawData[0] == yellow[0] && rawData[1] == yellow[1] && rawData[2] == yellow[2])
                {
                    rawRead.Add("10");
                }
                else
                {
                    rawRead.Add("00");
                }
            }

            return rawRead;
        }

        string ReadCode(Mat image, int sizeMetric)
        {
            (int, int)[] dataArray = new (int, int)[] { };
            
            dataArray = Coords.GetDataSlots(sizeMetric);

            List<string> rawRead = CodeReader(dataArray, sizeMetric, image);

            StringBuilder str = new StringBuilder();

            foreach (string s in rawRead)
            {
                str.Append(s);
            }
            string output = str.ToString();

            if (output != null)
            {
                return new MainWindow().Decode(output, sizeMetric);
            }
            else
            {
                return "botch";
            }
        }

    }

    public static class EmguExtensions
    {
        public static ImageSource ToImageSource(this Mat mat)
        {
            return mat.ToBitmap().ToImageSource();
        }

        public static ImageSource ToImageSource(this System.Drawing.Bitmap bitmap)
        {
            using (var memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
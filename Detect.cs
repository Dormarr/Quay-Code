using System;
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
using System.Windows.Threading;
using System.ComponentModel;
using System.Diagnostics;
using Quay_Code.Entities;

namespace Quay_Code
{


    public partial class Detect
    {
        MainWindow mainWindow;
        public Detect()
        {

            mainWindow = Application.Current.MainWindow as MainWindow;


        }

        public void DetectFromVideo()
        {
            //_vp.IdentifyFromVideo(webcamImg);
            //Mat[] postPro = FindSquares(_vp.ToProcess());
            //webcamImg.Source = postPro[0].ToImageSource();
        }

        //========================= Draw & Identify ==================================

        public Mat[] FindSquares(Mat input)
        {
            Mat frameGray = new();
            Mat threshMat = new();
            MCvScalar sclr = new(85, 255, 55);
            CvInvoke.CvtColor(input, frameGray, ColorConversion.Bgr2Gray);
            CvInvoke.AdaptiveThreshold(frameGray, threshMat, 255, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, 7, 7);

            VectorOfVectorOfPoint cnts = new();

            CvInvoke.FindContours(threshMat, cnts, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            List<PointF> cand = new();
            VectorOfPointF cnt = new();
            VectorOfPointF cnt2 = new();

            for (int i = 0; i < cnts.Size; i++)
            {
                CvInvoke.ApproxPolyDP(cnts[i], cnt, 0.005 * CvInvoke.ArcLength(cnts[i], true), true);

                if (cnt.Size != 4 || CvInvoke.ContourArea(cnt) < 200 || !CvInvoke.IsContourConvex(cnt))
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

                cand.AddRange(ConvertVectorOfPointToPointFArray(cnt2));
            }

            if (cnt2.Size == 4)
            {
                GetContourBits(input, cnt2, 1024);
                DrawContourFloat(input, cand, new MCvScalar(0, 255, 0));
            }

            //DrawContourFloat(threshMat, cand, new MCvScalar(100, 255, 100));

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
            VectorOfPointF send = new(cnt);
            return send;
        }

        void Swap<T>(ref T a, ref T b)
        {
            (b, a) = (a, b);
        }

        void DrawContourFloat(Mat img, List<PointF> cnt, MCvScalar color)
        {
            for (int i = 0; i < cnt.Count; ++i)
            {
                PointF from = cnt[i];
                PointF to = cnt[(i + 1) % cnt.Count];
                CvInvoke.Line(img, PointFToPoint(from), PointFToPoint(to), color, 2);
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

        async void GetContourBits(Mat image, VectorOfPointF cnt, int bits)
        {
            int pixelLen = (int)Math.Sqrt(bits);

            PointF[] corners = new PointF[4] { new(0, 0), new(bits, 0), new(bits, bits), new(0, bits) };
            VectorOfPointF cornerV = new(corners);

            Mat m = CvInvoke.GetPerspectiveTransform(cnt, cornerV);
            Mat binary = new();
            Mat persp = new();
            CvInvoke.WarpPerspective(image, persp, m, new Size(bits, bits));
            CvInvoke.Threshold(persp, binary, 64, 255, ThresholdType.Binary);

            int scaleFactor = DetermineSize(binary);
            //int scaleFactor = DetermineSize(image);

            if (scaleFactor != 100)
            {
                CvInvoke.PutText(image, scaleFactor.ToString(), PointFToPoint(cnt[0]), FontFace.HersheyPlain, 2, new MCvScalar(0, 0, 0));
                DrawFullGrid(binary, scaleFactor, new MCvScalar(0, 0, 255, 100));

                DrawContourFloat(image, corners.ToList(), new MCvScalar(0, 255, 0));

                //NOW WE HAVE SIZE, WE CAN JUST DECODE.

                //There's an issue here.
                //Can I just have it so that if the reader malfunctions, it just moves on? Without alert?
                //It's labelling it as Q12 even when it knows it's not.

                try
                {
                    int dataCount = ReadHeader(binary, scaleFactor);

                    Debug.WriteLine($"DataCount = {dataCount}");
                    string readUntrunc = ReadCodeData(binary, scaleFactor);
                    //string read = TruncateData(readUntrunc, dataCount);

                    Debug.WriteLine(readUntrunc);
                    OnTextOutputChanged(readUntrunc);
                    //CvInvoke.PutText(image, "    " + readUntrunc, PointFToPoint(cnt[2]), FontFace.HersheyPlain, 1.2, new MCvScalar(255, 255, 40));
                    //Await most common output
                    //binary.Save("C:\\Users\\Ryan\\Desktop\\Software Testing Ground\\Spam\\bin" + DateTime.Now.Ticks + ".png");
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Issue with GetContourBits: {e.Message} Source: {e.StackTrace}");
                }


            }
        }

        private Action<string> textOutputCallback;

        public void SetTextOutputCallback(Action<string> callback)
        {
            textOutputCallback = callback;
            Debug.WriteLine("Callback Set");
        }

        private void OnTextOutputChanged(string newText)
        {
            try
            {
                textOutputCallback?.Invoke(newText);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception in OnTextOutputChanged: {e.Message}");
            }
        }

        void DrawFullGrid(Mat img, int cols, MCvScalar color)
        {
            int cell = img.Cols / cols;

            for (int i = 1; i < cols; i++)
            {
                int y = i * cell;
                CvInvoke.Line(img, new Point(0, y), new Point(img.Cols, y), color, 4);
                CvInvoke.Line(img, new Point(y, 0), new Point(y, img.Rows), color, 4);
            }
        }

        static int DetermineSize(Mat image)
        {
            //Add a version of this that accounts for Black border AND white border (if on black background)
            List<int> sizes = new (){12, 18, 24, 32};
            Dictionary<int, ImageData> imgDataDict = new()
            {
                {0, new ImageData(image, 64, 16) },
                {1, new ImageData(image, 46, 22) },
                {2, new ImageData(image, 36, 28) },
                {3, new ImageData(image, 28, 36) }
            };

            int i = 0;
            ImageData imgData = imgDataDict[i];
            while(!CheckForSize(imgData.GetImage(), imgData.GetMetric(), imgData.GetSize())) {
                if(!imgDataDict.ContainsKey(++i))
                {
                    return 100;
                }
                imgData = imgDataDict[i];
            }

            return sizes[i];
        }

        static bool CheckForSize(Mat image, int metric, int size)
        {
            //Can this be refactored to use the coordinate system?

            //Can I draw grid and define coords based on that? If it matches up, then use?
            //Measure the white coords, because black can vary, too much room for error.

            (int, int)[] dataArray = Coords.GetRefSlots(size - 4);
            string result = "";

            for (int i = 0; i < dataArray.Length; i++)
            {
                byte[] rawData = image.GetRawData(dataArray[i].Item2 * metric + (metric / 2), dataArray[i].Item1 * metric + (metric / 2));
                result += rawData[0] == 255 && rawData[1] == 255 && rawData[2] == 255 ? "" : "x";
            }

            return result == "" ? true : false;
        }

        int ReadHeader(Mat image, int sizeMetric)
        {
            (int, int)[] headerSlots = Coords.GetHeader(sizeMetric);
            List<string> rawHeader = CodeReader(headerSlots, sizeMetric, image);
            string[] dataCountArray = new string[] { (rawHeader[3] + rawHeader[2]), (rawHeader[1] + rawHeader[0]) };
            int[] ints = CustomBinary.Read4Bit(dataCountArray);
            int finalHeader = int.Parse(string.Join("", ints));

            return finalHeader;
        }

        List<string> CodeReader((int, int)[] dataArray, int sizeMetric, Mat image)
        {
            List<string> rawRead = new();
            int pixelLen = 1024 / (sizeMetric + 4);

            for (int i = 0; i < dataArray.Length; i++)
            {
                int x = dataArray[i].Item1 * pixelLen + (pixelLen / 2);
                int y = dataArray[i].Item2 * pixelLen + (pixelLen / 2);
                byte[] rawData = image.GetRawData(y, x); //used to be x,y, but y,x seems to actually work.
                string flag = "00";

                flag = rawData.All(new byte[] { 0, 0, 0 }.Contains) ? "11" : flag; //black
                flag = rawData.All(new byte[] { 255, 255, 255 }.Contains) ? "10" : flag; //white
                flag = rawData.All(new byte[] { 255, 0, 0 }.Contains) ? "00" : flag; //blue
                flag = rawData.All(new byte[] { 255, 255, 0 }.Contains) ? "00" : flag; //light blue
                flag = rawData.All(new byte[] { 0, 0, 255 }.Contains) ? "01" : flag; //red
                flag = rawData.All(new byte[] { 255, 0, 255 }.Contains) ? "01" : flag;// light red
                flag = rawData.All(new byte[] { 0, 255, 255 }.Contains) ? "10" : flag; // yellow or default

                rawRead.Add(flag);
            }

            return rawRead;
        }

        string ReadCodeData(Mat image, int sizeMetric)
        {
            (int, int)[] dataArray = Coords.GetDataSlots(sizeMetric);
            List<string> rawRead = CodeReader(dataArray, sizeMetric, image);

            return CodeReader(dataArray, sizeMetric, image).Count() != 0 ? mainWindow.Decode(String.Join("", rawRead), sizeMetric) : "botch";
        }

    }
}
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using Image = System.Windows.Controls.Image;
using System.ComponentModel;
using Emgu.CV.CvEnum;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Quay_Code
{
    public partial class VideoProcessor
    {
        public bool _isProcessing;
        Mat frameOut;
        Image webcamImage;
        Detect _dtc = new();

        public VideoProcessor(Image imageFrame)
        {
            webcamImage = imageFrame;
        }

        public async void IdentifyFromVideo()
        {
            VideoCapture _vid = new(0, VideoCapture.API.DShow);
            _isProcessing = true;

            try
            {
                Mat frame = new();
                

                while (_isProcessing)
                {
                    _vid.Read(frame);


                    //Need to implement alternative methods of detecting different types of codes.
                    //Same principle as FindSquares, do for Aruco and QR. Need more in depth ways of distinguishing Quay and QR.
                    Mat[] outputArray = _dtc.FindSquares(frame);
                    
                    //CvInvoke.Imshow("ThreshMat", outputArray[0]);
                    frameOut = outputArray[1];
                    var bitmapSource = frame.ToImageSource();
                    webcamImage.Source = bitmapSource;
                    await Task.Delay(50);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Initialising Webcam: {ex.Message}");
            }
        }

        public Mat ToProcess()
        {
            return frameOut;
        }
    }

    public static class EmguExtensions
    {
        public static ImageSource ToImageSource(this Mat mat)
        {
            return BitmapSourceConvert.ToBitmapSource(mat.ToBitmap());
        }
    }

    public static class BitmapSourceConvert
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource ToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }

            IntPtr hBitmap = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap();
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                {
                    DeleteObject(hBitmap);
                }
            }
        }
    }
}

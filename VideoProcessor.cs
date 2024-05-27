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
using System.Diagnostics;

namespace Quay_Code
{
    public partial class VideoProcessor
    {
        public bool _isProcessing;
        Mat frameOut;
        Image webcamImage;
        Detect _dtc = new();
        VideoCapture _vid;


        public VideoProcessor(Image imageFrame)
        {
            StartFrameUpdate(imageFrame);
        }

        private void StartFrameUpdate(Image imageFrame)
        {
            webcamImage = imageFrame;
            _vid = new(0, VideoCapture.API.DShow);
        }

        public void TurnOffCamera(Image imageFrame)
        {
            _isProcessing = false;

            int width = (int)imageFrame.ActualWidth;
            int height = (int)imageFrame.ActualHeight;

            WriteableBitmap blackBitmap = new WriteableBitmap(width, height, 32, 32, PixelFormats.Bgra32, null);
            int[] blackPixels = new int[width * height];

            for(int i = 0; i < blackPixels.Length; i++)
            {
                blackPixels[i] = 171717;
            }

            blackBitmap.WritePixels(new Int32Rect(0, 0, width, height), blackPixels, width * 4, 0);

            try
            {                
                _vid.Dispose();
                _vid = null;

            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            webcamImage.Source = blackBitmap;
            imageFrame.Source = blackBitmap;
            
        }

        public async void IdentifyFromVideo()
        {
            //_vid = new(0, VideoCapture.API.DShow);
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

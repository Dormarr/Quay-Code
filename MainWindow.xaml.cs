using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Quay_Code
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CusDebug cusDebug;
        public bool isInit;
        public MainWindow()
        {
            InitializeComponent();
            isInit = true;
            Detect detectScript = new();
            this.ConnectToCV(detectScript);

            //cusDebug.Show(); //debug window
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void drag_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        //============================ ENCODE ===========================

        public int sizeMetric;
        WriteableBitmap bitmap;
        int staticSize = 240;
        int scaledSize;

        private void generateBtn_Click(object sender, RoutedEventArgs e)
        {
            string input = inputTxt.Text;
            int inputCount = input.Length;

            switch (inputCount)
            {
                case int i when i > 0 && i <= 14:
                    sizeMetric = 12;
                    break;
                case int i when i > 14 && i <= 41:
                    sizeMetric = 18;
                    break;
                case int i when i > 41 && i <= 88:
                    sizeMetric = 24;
                    break;
                case int i when i > 88 && i <= 130:
                    sizeMetric = 32;
                    break;
                case 0:
                    MessageBox.Show("Input box it empty.");
                    return;
                default:
                    MessageBox.Show("Input is too long!");
                    return;
            }

            //cusDebug.debug_SizeMetric.Text = $"{sizeMetric}";

            scaledSize = (sizeMetric + 4) * (staticSize / (sizeMetric + 4));
            //bitmap = new WriteableBitmap(staticSize, staticSize, 96, 96, PixelFormats.Bgra32, null);
            bitmap = new WriteableBitmap(scaledSize, scaledSize, 96, 96, PixelFormats.Bgra32, null);

            string passAlong = PadText(input);
            passAlong = WriteECC(passAlong, input.Length);
            //cusDebug.debug_ECCOut.Text = passAlong; //-----------------------------------------------
            passAlong = CustomBinary.WriteBinary(passAlong, sizeMetric);
            CreateGraphicCode(EncodeToPairs(passAlong), Coords.GetDataSlots(sizeMetric));

            //cusDebug.debug_BinOut.Text = passAlong;//-----------------------------------------------

            WriteHeader(inputCount);
            CreateOverlay(sizeMetric);
        }

        private string PadText(string input)
        {
            Dictionary<int, int> padAmtDict = new()
            {
                {12, 20}, {18, 53}, {24, 112}, {32, 197}
            };

            if (!padAmtDict.ContainsKey(sizeMetric))
            {
                //catch error here
            }

            return input.PadRight(padAmtDict[sizeMetric], 'x');
        }

        private string WriteECC(string input, int inputCount)
        {
            ECC ecc = new();
            int[] newInts = ecc.Encode(input, inputCount);
            char[] chars = new char[newInts.Length];

            for (int i = 0; i < newInts.Length; i++)
            {
                chars[i] = (char)newInts[i];
            }

            return new string(chars);
        }

        private void WriteHeader(int inputCount)
        {
            //additional header elements not yet implemented. Only input count.
            Dictionary<int, string> symCountDict = new()
            {
                {12, PadToLeft(inputCount, 3)}, {18,  PadToLeft(inputCount, 3)}, {24,  PadToLeft(inputCount, 6)}, {32,  PadToLeft(inputCount, 8)}
            };

            //Modular approach to easily expand later.
            //divide into chars
            char[] headerChars = symCountDict[sizeMetric].ToCharArray();
            string headerUnpaired = CustomBinary.Write4Bit(headerChars);

            CreateGraphicCode(EncodeToPairs(headerUnpaired), Coords.GetHeader(sizeMetric));

            //cusDebug.debug_BinHeader.Text = headerUnpaired;
        }

        private String PadToLeft(int inputCount, int padding)
        {
            return $"{inputCount}".PadLeft(padding, '0');
        }

        private string[] EncodeToPairs(string input)
        {
            string[] pairs = new string[input.Length / 2];

            for (int i = 0; i < input.Length; i += 2)
            {
                pairs[i / 2] = input.Substring(i, 2);
            }

            return pairs;
        }

        //====================== Decode ==========================

        public string Decode(string input, int sizeMetric)
        {
            Dictionary<int, int> padAmtDict = new()
            {
                {12, 160}, {18, 424}, {24, 896}, {32, 1576}
            };

            string paddedIn = input.PadRight(padAmtDict[sizeMetric], '0');
            //byte[] B4data = Convert.FromBase64String(paddedIn);
            //Debug.WriteLine($"{B4data}");
            return Encoding.ASCII.GetString(GetBytesFromBinaryString(paddedIn));
        }

        public static Byte[] GetBytesFromBinaryString(string binary)
        {
            var list = new List<byte>();

            for (int i = 0; i < binary.Length; i += 8)
            {
                list.Add(Convert.ToByte(binary.Substring(i, 8), 2));
            }

            return list.ToArray();
        }


        //===================== Drawing =======================

        private WriteableBitmap Mark(int binPair, double scaleFactor, int x, int y, int w, int h, WriteableBitmap _bitmap)
        {
            int stride = (int)(scaleFactor * _bitmap.Format.BitsPerPixel + 7) / 8;
            //int stride = (int)(scaleFactor * _bitmap.Format.BitsPerPixel) / 8;
            Dictionary<int, byte[]> colourDict = new() {
                {00, new byte[] { 230, 200, 20, 255 }}, //CYAN
                {01, new byte[] { 140, 45, 255, 255 }}, //MAGENTA
                {10, new byte[] { 20, 220, 255, 255 }}, //YELLOW
                {11, new byte[] { 20, 20, 20, 255 }}, //BLACK
                {22,new byte[] { 245, 245, 245, 255 }} //WHITE
            };
            byte[] colour = colourDict.ContainsKey(binPair) ? colourDict[binPair] : colourDict[22];
            Byte[] colourData = ColourIndex(colour, x, y, w, h);

            _bitmap.WritePixels(new Int32Rect(x, y, w, h), colourData, stride, 0);

            return _bitmap;
        }

        private byte[] ColourIndex(byte[] inputColour, int rectX, int rectY, int rectWidth, int rectHeight)
        {
            byte[] pixelData = new byte[rectWidth * rectHeight * 4];
            int pixelIndex = 0;

            for (int y = rectY; y < rectY + rectHeight; y++)
            {
                for (int x = rectX; x < rectX + rectWidth; x++)
                {
                    if (pixelIndex >= 1600) { break; }

                    pixelData[pixelIndex] = inputColour[0];
                    pixelData[pixelIndex + 1] = inputColour[1];
                    pixelData[pixelIndex + 2] = inputColour[2];
                    pixelData[pixelIndex + 3] = inputColour[3];

                    pixelIndex += 4;
                }
            }
            return pixelData;
        }

        private void CreateOverlay(int sizeMetric)
        {
            (int, int)[] Bcoords = Coords.GetBlackSlots(sizeMetric);
            string[] Bdata = new string[Bcoords.Length];
            for (int i = 0; i < Bcoords.Length; i++)
            {
                Bdata[i] = "11";
            }
            CreateGraphicCode(Bdata, Bcoords);

            (int, int)[] Wcoords = Coords.GetWhiteSlots(sizeMetric);
            string[] Wdata = new string[Wcoords.Length];
            for (int i = 0; i < Wcoords.Length; i++)
            {
                Wdata[i] = "22";
            }
            CreateGraphicCode(Wdata, Wcoords);

            (int, int)[] Ccoords = new (int, int)[] { (8, 3), (9, 3), (10, 3) };
            string[] Cdata = new string[] { "00", "01", "10" };
            CreateGraphicCode(Cdata, Ccoords);
        }

        private void CreateGraphicCode(string[] data, (int, int)[] coords)
        {
            //int scaleFactor = scaledSize / (sizeMetric + 4);
            int scaleFactor = staticSize / (sizeMetric + 4);

            for (int i = 0; i < data.Length; i++)
            {
                (int, int) coord = coords[i];

                int rectX = coord.Item1 * scaleFactor; //col
                int rectY = coord.Item2 * scaleFactor; //row

                int rectW = scaleFactor;
                int rectH = scaleFactor;

                bitmap = Mark(int.Parse(data[i]), scaleFactor, rectX, rectY, rectW, rectH, bitmap);
            }

            this.bitmapImg.Source = bitmap;
        }

        public void ConnectToCV(Detect detect)
        {
            detect.SetTextOutputCallback(TextOutputChanged);
            Debug.WriteLine("Connect Called.");
        }

        public void TextOutputChanged(string text)
        {
            try
            {
                Debug.WriteLine(text);
                OutputTxt.Text = text;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Issue in TextOutputChanged: {e.Message}");
            }
        }

        //=========================== Buttons =====================================

        private void Phone_Click(object sender, RoutedEventArgs e)
        {
            inputTxt.Text = "tel:+1234567890";
        }

        private void Email_Click(object sender, RoutedEventArgs e)
        {
            inputTxt.Text = "mailto:john.doe@example.com";
        }

        private void URL_Click(object sender, RoutedEventArgs e)
        {
            inputTxt.Text = "https://www.example.com";
        }

        private void Wifi_Click(object sender, RoutedEventArgs e)
        {
            inputTxt.Text = "wifi:S:SSID;P:password;T:WPA;";
        }

        private void SMS_Click(object sender, RoutedEventArgs e)
        {
            inputTxt.Text = "sms:+1234567890?body=Hello%20World";
        }

        private void Geo_Click(object sender, RoutedEventArgs e)
        {
            inputTxt.Text = "geo:latitude,longitude";
        }

        VideoProcessor _vp;
        private void Det_Cam_Click(object sender, RoutedEventArgs e)
        {
            //Detect dtc = new Detect(webcamImage);
            //dtc.DetectFromVideo();

            _vp = new(webcamImage);
            _vp.IdentifyFromVideo();
        }

        private void TurnOff_Cam_Click(object sender, RoutedEventArgs e)
        {
            _vp.TurnOffCamera(webcamImage);
            Debug.WriteLine("Turn Off Camera Clicked");
        }

        private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _vp.TurnOffCamera(webcamImage);
            Debug.WriteLine("Changed Tab.");
        }

        //========================================= Download ==============================================

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            string downloadsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";

            DownloadBitmap(bitmap, downloadsFolderPath + "Quay" + DateTime.UtcNow.Ticks + ".png");
        }

        private void DownloadBitmap(WriteableBitmap finalBitmap, string filePath)
        {
            if (finalBitmap != null)
            {
                SaveBitmapAsPng(finalBitmap, filePath);

                // Open a SaveFileDialog to allow the user to specify the download location
                SaveFileDialog saveFileDialog = new()
                {
                    FileName = "Quay" + DateTime.UtcNow.Ticks + ".png",
                    Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    System.IO.File.Copy(filePath, saveFileDialog.FileName, true);
                    MessageBox.Show("Bitmap downloaded.");
                }
            }
            else
            {
                MessageBox.Show("Create a bitmap first.");
            }
        }

        private void SaveBitmapAsPng(WriteableBitmap finalBitmap, string filePath)
        {
            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                PngBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(finalBitmap));
                encoder.Save(stream);
            }
        }


    }
}

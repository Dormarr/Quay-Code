using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Quay_Code
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CusDebug cusDebug = new CusDebug();
        public MainWindow()
        {
            InitializeComponent();

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

            cusDebug.debug_SizeMetric.Text = $"{sizeMetric}";

            bitmap = new WriteableBitmap(staticSize, staticSize, 96, 96, PixelFormats.Bgra32, null);

            string passAlong = PadText(input);
            passAlong = WriteECC(passAlong, input.Length);
            cusDebug.debug_ECCOut.Text = passAlong; //-----------------------------------------------
            passAlong = CustomBinary.WriteBinary(passAlong, sizeMetric);
            CreateGraphicCode(EncodeToPairs(passAlong), Coords.GetDataSlots(sizeMetric));

            cusDebug.debug_BinOut.Text = passAlong;//-----------------------------------------------

            WriteHeader(inputCount);

            CreateOverlay(sizeMetric);
        }

        private string PadText(string input)
        {
            int padAmt;

            switch(sizeMetric)
            {
                case 12:
                    padAmt = 20;
                    break;
                case 18:
                    padAmt = 53;
                    break;
                case 24:
                    padAmt = 112;
                    break;
                case 32:
                    padAmt = 197;
                    break;
                default:
                    //add error render.
                    return null;
            }
            return input.PadRight(padAmt, 'x');

        }

        private string WriteECC(string input, int inputCount)
        {
            ECC ecc = new ECC();
            int[] newInts = ecc.Encode(input, inputCount);
            char[] chars = new char[newInts.Length];

            for(int i = 0; i < newInts.Length; i++)
            {
                chars[i] = (char)newInts[i];
            }

            return new string(chars);
        }

        private void WriteHeader(int inputCount)
        {
            //additional header elements not yet implemented. Only input count.
            string symCount;

            switch(sizeMetric)
            {
                case 12:
                    symCount = $"{inputCount}".PadRight(3, '0');
                    break;
                case 18:
                    symCount = $"{inputCount}".PadRight(3, '0');
                    break;
                case 24:
                    symCount = $"{inputCount}".PadRight(6, '0');
                    break;
                case 32:
                    symCount = $"{inputCount}".PadRight(8, '0');
                    break;
                default:
                    return;
            }

            //Modular approach to easily expand later.

            string headerOut = symCount; // + other definings.

            //divide into chars

            char[] headerChars = headerOut.ToCharArray();
            string headerUnpaired = CustomBinary.Write4Bit(headerChars);

            CreateGraphicCode(EncodeToPairs(headerUnpaired), Coords.GetHeader(sizeMetric));

            cusDebug.debug_BinHeader.Text = headerUnpaired;
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
            int padAmt;

            switch (sizeMetric)
            {
                case 12:
                    padAmt = 160;
                    break;
                case 18:
                    padAmt = 430;
                    break;
                case 24:
                    padAmt = 898;
                    break;
                case 32:
                    padAmt = 1580;
                    break;
                default:
                    padAmt = 160;
                    break;
            }
            string paddedIn = input.PadRight(padAmt, '0');
            byte[] data = GetBytesFromBinaryString(paddedIn);
            byte[] B4data = Convert.FromBase64String(paddedIn);

            return Encoding.UTF8.GetString(data);
        }

        public static Byte[] GetBytesFromBinaryString(string binary)
        {
            var list = new List<Byte>();

            for (int i = 0; i < binary.Length; i += 8)
            {
                String t = binary.Substring(i, 8);

                list.Add(Convert.ToByte(t, 2));
            }

            return list.ToArray();
        }


        //===================== Drawing =======================

        private WriteableBitmap Mark(int binPair, int scaleFactor, int x, int y, int w, int h, WriteableBitmap bitmap)
        {
            int stride = (scaleFactor * bitmap.Format.BitsPerPixel + 7) / 8;
            byte[] colour;

            switch (binPair)
            {
                case 00:
                    colour = new byte[] { 230, 200, 20, 255 }; //CYAN
                    break;
                case 01:
                    colour = new byte[] { 140, 45, 255, 255 }; //MAGENTA
                    break;
                case 10:
                    colour = new byte[] { 20, 220, 255, 255 }; //YELLOW
                    break;
                case 11:
                    colour = new byte[] { 20, 20, 20, 255 }; // BLACK
                    break;
                default:
                    colour = new byte[] { 245, 245, 245, 255 }; //WHITE
                    break;
            }

            Byte[] colourData = ColourIndex(colour, x, y, w, h);
            bitmap.WritePixels(new Int32Rect(x, y, w, h), colourData, stride, 0);

            return bitmap;
        }

        private byte[] ColourIndex(byte[] inputColour, int rectX, int rectY, int rectWidth, int rectHeight)
        {
            byte[] pixelData = new byte[rectWidth * rectHeight * 4];
            int pixelIndex = 0;

            for (int y = rectY; y < rectY + rectHeight; y++)
            {
                for (int x = rectX; x < rectX + rectWidth; x++)
                {
                    if (pixelIndex >= 1600)
                    {
                        break;
                    }

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
            for(int i = 0; i < Bcoords.Length; i++)
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
            CreateGraphicCode (Cdata, Ccoords);
        }

        private void CreateGraphicCode(string[] data, (int,int)[] coords)
        {
            int scaleFactor = staticSize / (sizeMetric + 4);

            for(int i = 0; i < data.Length; i++)
            {
                int pairInt;
                int.TryParse(data[i], out pairInt);

                (int, int) coord = coords[i];

                int col = coord.Item1;
                int row = coord.Item2;

                int rectX = col * scaleFactor;
                int rectY = row * scaleFactor;

                int rectW = scaleFactor;
                int rectH = scaleFactor;

                bitmap = Mark(pairInt, scaleFactor, rectX, rectY, rectW, rectH, bitmap);
            }

            this.bitmapImg.Source = bitmap;
        }

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

        private void Det_Cam_Click(object sender, RoutedEventArgs e)
        {
            //Detect dtc = new Detect(webcamImage);
            //dtc.DetectFromVideo();

            VideoProcessor _vp = new VideoProcessor(webcamImage);
            _vp.IdentifyFromVideo();
        }
    }
}

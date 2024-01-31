using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quay_Code.Entities
{
    internal class ImageData
    {
        public Mat image;
        public int metric;
        public int size;

        public ImageData() { }
        public ImageData(Mat image, int metric, int size)
        {
            this.image = image;
            this.metric = metric;
            this.size = size;
        }

        public Mat GetImage() { return image; }
        public int GetMetric() { return metric; }
        public int GetSize() { return size; }
    }
}

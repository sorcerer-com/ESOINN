using Main.Network;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Main.Samples
{
    public class ImageSample : SampleBase
    {
        public override Rectangle Rectangle
        {
            get
            {
                Rectangle rect = new Rectangle();
                rect.Width = 640;
                rect.Height = 480;
                rect.Fill = new ImageBrush(new BitmapImage(new Uri("img.png", UriKind.RelativeOrAbsolute)));
                (rect.Fill as ImageBrush).Opacity = 0.5;
                Canvas.SetLeft(rect, 0);
                Canvas.SetTop(rect, 0);
                return rect;
            }
        }

        private System.Drawing.Bitmap bitmap;
        private Random rand = new Random();


        public ImageSample(string imagePath)
        {
            Model = new ESOINN(5, 100, 1000);

            bitmap = new System.Drawing.Bitmap(imagePath);
            SamplesCount = bitmap.Width * bitmap.Height / 1;
            ReduceNoise = true;
            Winners = new List<Vertex>();
        }


        public override bool Process(int sample, bool learn = true)
        {
            int x = rand.Next(bitmap.Width);
            int y = rand.Next(bitmap.Height);
            var color = bitmap.GetPixel(x, y);
            bool b = color.ToArgb() == System.Drawing.Color.Black.ToArgb();
            if (!ReduceNoise || (!b || rand.NextDouble() > 0.999f))
            {
                var winner = Model.Process(new double[] { x, y, color.R, color.G, color.B });
                Winners.Add(winner);
            }
    
            return true;
        }

        public override Point GetVertexPosition(Vertex vertex)
        {
            return new Point(vertex.Weight[0], vertex.Weight[1]);
        }

    }
}

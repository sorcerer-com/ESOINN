using Main.Network;
using Main.Samples;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SampleBase Sample { get; set; }
        private bool learn;

        private bool runing;

        private Point start;
        private Point origin;
        private SettingsWindow settingsWindows;

        public MainWindow()
        {
            InitializeComponent();
            startButton.Focus();
            this.learn = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            this.runing = false;
            Thread.Sleep(500);
            this.Sample.Dispose();

            base.OnClosing(e);
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);

            if (settingsWindows == null || !settingsWindows.IsVisible)
                return;

            settingsWindows.Top = this.Top;
            settingsWindows.Left = this.Left + this.ActualWidth;
        }
        
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TransformGroup tg = canvas.RenderTransform as TransformGroup;
            TranslateTransform tt = tg.Children[2] as TranslateTransform;

            start = e.GetPosition(this);
            origin = new Point(tt.X, tt.Y);
            canvas.CaptureMouse();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (canvas.IsMouseCaptured)
            {
                TransformGroup tg = canvas.RenderTransform as TransformGroup;
                TranslateTransform tt = tg.Children[2] as TranslateTransform;

                Vector v = start - e.GetPosition(this);
                tt.X = origin.X - v.X;
                tt.Y = origin.Y - v.Y;

                if (!this.runing)
                    refreshUI(0, new TimeSpan());
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            canvas.ReleaseMouseCapture();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TransformGroup tg = canvas.RenderTransform as TransformGroup;
            ScaleTransform st = tg.Children[0] as ScaleTransform;
            TranslateTransform tt = tg.Children[2] as TranslateTransform;

            if (e.ChangedButton == MouseButton.Left && Sample != null)
            {
                Point min = new Point(double.MaxValue, double.MaxValue);
                Point max = new Point(double.MinValue, double.MinValue);
                List<Vertex> vertices = new List<Vertex>();
                lock(Sample)
                    vertices.AddRange(Sample.Model.Graph.Vertices);
                foreach (var vertex in vertices)
                {
                    Point p = Sample.GetVertexPosition(vertex);
                    min.X = Math.Min(min.X, p.X);
                    min.Y = Math.Min(min.Y, p.Y);
                    max.X = Math.Max(max.X, p.X);
                    max.Y = Math.Max(max.Y, p.Y);
                }

                tt.X = (canvas.ActualWidth / 2) - (max.X + min.X) / 2;
                tt.Y = (canvas.ActualHeight / 2) - (max.Y + min.Y) / 2;

                double zoom = Math.Min(canvas.ActualWidth, canvas.ActualHeight) / Math.Max(max.X - min.X, max.Y - min.Y);
                if (double.IsInfinity(zoom))
                    zoom = 1.0;
                st.ScaleX = zoom;
                st.ScaleY = zoom;
                tt.X *= st.ScaleX;
                tt.Y *= st.ScaleY;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                tt.X = 0;
                tt.Y = 0;
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;
            }

            if (!this.runing)
                refreshUI(0, new TimeSpan());
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TransformGroup tg = canvas.RenderTransform as TransformGroup;
            ScaleTransform st = tg.Children[0] as ScaleTransform;
            TranslateTransform tt = tg.Children[2] as TranslateTransform;
            
            double zoom = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            st.ScaleX *= zoom;
            st.ScaleY *= zoom;
            var pos = e.GetPosition(canvas);
            tt.X *= zoom;
            tt.Y *= zoom;

            if (!this.runing)
                refreshUI(0, new TimeSpan());
        }


        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Sample == null)
                return;
            if (settingsWindows != null && settingsWindows.IsVisible)
                settingsWindows.Close();

            settingsWindows = new SettingsWindow(Sample, Sample.Model);
            settingsWindows.Owner = this;
            settingsWindows.Top = this.Top;
            settingsWindows.Left = this.Left + this.ActualWidth;
            settingsWindows.Show();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.runing)
            {
                if (comboBox.Text == "Image")
                    Sample = new ImageSample("img.png");
                else if (comboBox.Text == "Sound")
                    Sample = new SoundSample();

                this.canvas.Children.Clear();
                refreshUI(0, new TimeSpan());

                this.runing = true;
                Thread thread = new Thread(new ThreadStart(process));
                thread.Name = "worker";
                thread.IsBackground = true;
                thread.Start();

                startButton.Content = "Stop";

                this.settingsButton_Click(sender, e);
            }
            else
            {
                this.runing = false;
                startButton.Content = "Start";

                if (settingsWindows != null && settingsWindows.IsVisible)
                    settingsWindows.Close();
            }
        }

        private void learnToggleButton_Click(object sender, RoutedEventArgs e)
        {
            this.learn = this.learnToggleButton.IsChecked == true;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Sample == null)
                return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".txt";
            sfd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            sfd.FileName = "ESOINN.txt";
            if (sfd.ShowDialog() == true)
            {
                Thread.Sleep(100);
                lock(Sample)
                    Sample.Model.Save(sfd.FileName);
            }
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            if (Sample == null)
                return;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".txt";
            ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            ofd.FileName = "ESOINN.txt";
            if (ofd.ShowDialog() == true)
            {
                Thread.Sleep(100);
                lock (Sample)
                    Sample.Model.Load(ofd.FileName);
            }
        }


        private void process()
        {
            DateTime start = DateTime.Now;
            DateTime temp = DateTime.Now;
            for (int i = 0; i < Sample.SamplesCount; i++)
            {
                if (!this.runing)
                    return;

                bool res = true;
                lock (Sample)
                    res = Sample.Process(i, this.learn);
                if (!res)
                {
                    i--;
                    Thread.Sleep(10);
                }

                if ((DateTime.Now - temp).Milliseconds > 200 && this.runing)
                {
                    if (Sample.Model.IterationCount % Sample.Model.IterationThreshold == 0 || !this.learn)
                        Sample.Model.Classify();
                    Dispatcher.Invoke(() => { refreshUI(i, DateTime.Now - start); });
                    temp = DateTime.Now;
                }
                //Console.WriteLine("{0}/{1}", i, Sample.SamplesCount);
            }
            Sample.Model.Classify();

            Dispatcher.Invoke(() => { refreshUI(Sample.SamplesCount, DateTime.Now - start); });
        }

        private void refreshUI(int sample, TimeSpan elapsed)
        {
            if (Sample == null)
                return;

            progress.Maximum = Sample.SamplesCount;
            progress.Value = sample; 
            if (sample == Sample.SamplesCount)
            {
                this.runing = false;
                startButton.Content = "Start";
            }

            log.Items.Clear();
            int idx = Sample.Winners.Count - 1;
            while (log.Items.Count < 10)
            {
                StringBuilder sbr = new StringBuilder("");
                while (idx >= 0 && Sample.Winners[idx] != null)
                {
                    double len = 0.0;
                    for (int i = 0; i < Sample.Winners[idx].Weight.Length; i++)
                        len += Sample.Winners[idx].Weight[i] * Sample.Winners[idx].Weight[i];
                    len = Math.Sqrt(len);

                    sbr.Insert(0, string.Format("{0,4:000}({1:00.000}) ", Sample.Model.Graph.Vertices.IndexOf(Sample.Winners[idx]), len));
                    idx--;
                }
                if (sbr.Length > 0)
                    log.Items.Add(sbr.ToString());
                if (idx == -1)
                    break;
                idx--;
            }

            // Graph
            TransformGroup tg = canvas.RenderTransform as TransformGroup;
            ScaleTransform st = tg.Children[0] as ScaleTransform;
            TranslateTransform tt = tg.Children[2] as TranslateTransform;

            if (this.canvas.Children.Count == 0)
            {
                this.canvas.Children.Add(Sample.Rectangle);
                this.canvas.Children.Add(new TextBlock());
            }

            if (Sample.Model == null)
                return;

            int childIndex = 1;
            foreach (var edge in Sample.Model.Graph.Edges)
            {
                byte a = (byte)Math.Min(255, 105 + 50 * edge.Vertex1.Edges.Count);
                Color c = Color.FromArgb(a, 
                    (byte)(((double)((edge.Vertex1.ClassId + 0) % 3) / 3) * 100),
                    (byte)(((double)((edge.Vertex1.ClassId + 1) % 3) / 3) * 100),
                    (byte)(((double)((edge.Vertex1.ClassId + 2) % 3) / 3) * 100));

                Line line = this.canvas.Children[childIndex] as Line;
                if (line == null)
                {
                    line = new Line();
                    this.canvas.Children.Insert(childIndex, line);
                }
                childIndex++;

                Point pos1 = Sample.GetVertexPosition(edge.Vertex1);
                Point pos2 = Sample.GetVertexPosition(edge.Vertex2);
                line.X1 = pos1.X;
                line.Y1 = pos1.Y;
                line.X2 = pos2.X;
                line.Y2 = pos2.Y;
                line.Stroke = new SolidColorBrush(c);
                line.StrokeThickness = 1.0 / ((st.ScaleX - 1.0) / 2 + 1.0);
            }


            foreach (var vertex in Sample.Model.Graph.Vertices)
            {
                byte a = (byte)Math.Min(255, 105 + 50 * vertex.Edges.Count);
                Color c = Color.FromArgb(a,
                    (byte)(((double)((vertex.ClassId + 0) % 3) / 3) * 255),
                    (byte)(((double)((vertex.ClassId + 1) % 3) / 3) * 255),
                    (byte)(((double)((vertex.ClassId + 2) % 3) / 3) * 255));

                Rectangle rect = this.canvas.Children[childIndex] as Rectangle;
                if (rect == null)
                {
                    rect = new Rectangle();
                    this.canvas.Children.Insert(childIndex, rect);
                }
                childIndex++;

                rect.Width = 4 / ((st.ScaleX - 1.0) / 2 + 1.0);
                rect.Height = 4 / ((st.ScaleY - 1.0) / 2 + 1.0);
                rect.Fill = new SolidColorBrush(c);
                Point pos = Sample.GetVertexPosition(vertex);
                Canvas.SetLeft(rect, pos.X - (rect.Width / 2));
                Canvas.SetTop(rect, pos.Y - (rect.Height / 2));
            }

            this.canvas.Children.RemoveRange(childIndex, this.canvas.Children.Count - childIndex - 1);

            TextBlock tb = this.textBlock;
            tb.Background = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(progress.Value + " / " + progress.Maximum);
            sb.AppendLine("Time: " + elapsed);
            sb.AppendLine("Classes: " + Sample.Model.NumberOfClasses);
            sb.AppendLine("Vertices: " + Sample.Model.NumberOfVertices);
            sb.AppendLine("Edges: " + Sample.Model.NumberOfEdges);
            sb.AppendLine("IterationCount: " + Sample.Model.IterationCount);
            tb.Text = sb.ToString();
        }

    }
}

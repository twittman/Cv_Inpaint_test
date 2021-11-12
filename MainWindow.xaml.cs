using System;
using System.Drawing;
using System.IO;
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
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace inpaint
{
    public partial class MainWindow : System.Windows.Window
    {
        List<List<OpenCvSharp.Point>> inPaintPoints = null;
        List<OpenCvSharp.Point> inPaintCurrentPoints = null;
        bool inPaintSelection = false;

        System.Windows.Point currentPoint = new System.Windows.Point();
        // Opens image
        Mat inputImageCv;
        Mat copyOfMat;
            void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select Image";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                inputImageCv = Cv2.ImRead(op.FileName);
                ImageBrush bgBrush = new ImageBrush();
                bgBrush.ImageSource = MatToBitmapImage(inputImageCv);

                ImageImageImage.Source = MatToBitmapImage(inputImageCv);
                //leftCanvas.Background = bgBrush;
                //leftCanvas.Width = inputImageCv.Cols;
                //leftCanvas.Height = inputImageCv.Rows;
                //InputImage.Source = new BitmapImage(new Uri(op.FileName));
                copyOfMat = inputImageCv.Clone();
            }
        }

        private void Canvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                currentPoint = e.GetPosition(this);
        }
        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (inPaintSelection == true && e.LeftButton == MouseButtonState.Pressed)
            {
                Ellipse nEllipse = new Ellipse();
                nEllipse.Fill = System.Windows.Media.Brushes.Black;
                nEllipse.Width = (int)11;
                nEllipse.Height = (int)11;
                Canvas.SetLeft(nEllipse, e.GetPosition(this).X);
                Canvas.SetTop(nEllipse, e.GetPosition(this).Y);
                leftCanvas.Children.Add(nEllipse);
            }
        }

        private System.Drawing.Image ImageWpfToGDI(System.Windows.Media.ImageSource image)
        {
            MemoryStream ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image as System.Windows.Media.Imaging.BitmapSource));
            encoder.Save(ms);
            ms.Flush();
            return System.Drawing.Image.FromStream(ms);
        }
        public BitmapImage MatToBitmapImage(Mat image)
        {
            Bitmap bitmap = MatToBitmap(image);
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();

                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }
        public Bitmap MatToBitmap(Mat image)
        {
            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
        }

        private void ZoomInClick(object sender, RoutedEventArgs e)
        {
            leftCanvas.Height /= 1.5;
            leftCanvas.Width /= 1.5;
            OutputImage.Height *= 1.5;
            OutputImage.Width *= 1.5;
        }
        private void ZoomOutClick(object sender, RoutedEventArgs e)
        {
            leftCanvas.Height *= 1.5;
            leftCanvas.Width *= 1.5;
            OutputImage.Height /= 1.5;
            OutputImage.Width /= 1.5;
        }

        private void AddMask(object sender, RoutedEventArgs e)
        {
            inPaintSelection = true;
            inPaintCurrentPoints = new List<OpenCvSharp.Point>();
            inPaintPoints = new List<List<OpenCvSharp.Point>>();
        }

        private void CreatePng(Canvas canvas, int width, int height, string filename)
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)canvas.Width, (int)canvas.Height,
                96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(canvas);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            using (FileStream file = File.Create(filename))
            {
                encoder.Save(file);
            }
        }
        private void ButtonSave(object sender, RoutedEventArgs e)
        {
            Mat copyMatImage = copyOfMat.Clone();
            int Width = copyMatImage.Cols;
            int Height = copyMatImage.Rows;
            CreatePng(leftCanvas, Width, Height, "mask.png");
        }

        private void ShowResizedMask(object sender, RoutedEventArgs e)
        {
            Mat copyMatImage1 = copyOfMat.Clone();
            int Width = copyMatImage1.Cols;
            int Height = copyMatImage1.Rows;
            CreatePng(leftCanvas, Width, Height, "mask.png");


            OpenCvSharp.Size maskSize = new OpenCvSharp.Size(Width, Height);

            Mat mask1;
            mask1 = Cv2.ImRead("mask.png", ImreadModes.Grayscale);
            Cv2.Resize(mask1, mask1, maskSize);
            Cv2.ImShow("window1", mask1);
            Cv2.ImWrite("maskResized.png", mask1);
        }


        // Copies image from `Input` box to `Output` box
        private void ButtonProcess(object sender, RoutedEventArgs e)
        {
            Mat copyMatImage = copyOfMat.Clone();
            int Width = copyMatImage.Cols;
            int Height = copyMatImage.Rows;
            int CenterW = Width / 2;
            int CenterH = Height / 2;

            OpenCvSharp.Size maskSize = new OpenCvSharp.Size(Width, Height);

            Mat test = Mat.Zeros(Width, Height, MatType.CV_8UC1);
            Mat mask;
            //Mat mask2 = new Mat();

            mask = Cv2.ImRead("mask.png", ImreadModes.Grayscale);

            //OpenCvSharp.Rect cropLoc = new OpenCvSharp.Rect(0, 0, Width, Height);
            //Mat maskCropped = new Mat(mask, cropLoc);
            Cv2.Resize(mask, mask, maskSize);

            Cv2.Inpaint(copyMatImage, mask, test, 16, InpaintMethod.NS);
            //OutputImage.Source = MatToBitmapImage(outputMatImage);
            OutputImage.Source = MatToBitmapImage(test);
        }
    }
}

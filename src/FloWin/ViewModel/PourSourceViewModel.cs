using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FloWin.ViewModel
{
    public class PourSourceViewModel : Common.ViewModelBase
    {
        private Model.PourSource _src = null;
        public PourSourceViewModel() { }

        public void setPourSource(Model.PourSource src)
        {
            _src = src;
            updateSourceImage();

            Width = _src.Bounds.Width;
            Height = _src.Bounds.Height;
            P1 = _src.SrcPoint1;
            P2 = _src.SrcPoint2;
            IsDragging = false;
        }

        private Drawing _drawing;
        public Drawing Test
        {
            get { return _drawing; }
            private set { _drawing = value; RaisePropertyChanged("Test"); }
        }
        private void updateSourceImage()
        {
            BitmapImage img = null;
            using (System.Drawing.Bitmap bmp = Properties.Resources.pour_source)
            {
                img = bmp.ToBitmapImage();
            }

            // 描画用画像を作成
            Rect rect = new Rect(0, 0, img.PixelWidth - 1, img.PixelHeight - 1);
            DrawingGroup dg = new DrawingGroup();
            dg.ClipGeometry = new RectangleGeometry(rect);
            using (var dc = dg.Open())
            {
                dc.DrawImage(img, new Rect(0, 0, img.PixelWidth, img.PixelHeight));
            }
            Test = dg;

            // 下端ピクセル1列を取得
            uint[] bottom = new uint[img.PixelWidth - 1];
            img.CopyPixels(new Int32Rect(0, img.PixelHeight - 1, img.PixelWidth - 1, 1),
                bottom, ((img.PixelWidth - 1) * img.Format.BitsPerPixel + 7) / 8, 0);

            // 右端ピクセル1列を取得
            uint[] right = new uint[img.PixelHeight - 1];
            img.CopyPixels(new Int32Rect(img.PixelWidth - 1, 0, 1, img.PixelHeight - 1),
                right, (img.Format.BitsPerPixel + 7) / 8, 0);

            // 注ぎ口の座標を得る
            bool on = false;
            int x1 = -1, x2 = -1;
            int y1 = -1, y2 = -1;
            for (int i = 0; i < bottom.Length; i++)
            {
                if (!on && bottom[i] != 0) { on = true; x1 = i; }
                else if (on && bottom[i] == 0) { on = false; x2 = i; }
            }
            on = false;
            for (int i = 0; i < right.Length; i++)
            {
                if (!on && right[i] != 0) { on = true; y1 = i; }
                else if (on && right[i] == 0) { on = false; y2 = i; }
            }
            if (x1 < 0) x1 = 0;
            if (x2 < 0) x2 = (int)Math.Floor(rect.Width);
            if (y1 < 0) y1 = 0;
            if (y2 < 0) y2 = (int)Math.Floor(rect.Height);

            _src.Bounds = new Rect(_src.Bounds.X, _src.Bounds.Y, rect.Width, rect.Height);
            _src.SrcPoint1 = new Point(x1, Math.Max(y1, y2));
            _src.SrcPoint2 = new Point(x2, Math.Max(y1, y2));
        }

        private double _width = 10;
        public double Width
        {
            get { return _width; }
            private set
            {
                _width = value;
                RaisePropertyChanged("Width");
            }
        }

        private double _height = 10;
        public double Height
        {
            get { return _height; }
            private set
            {
                _height = value;
                RaisePropertyChanged("Height");
            }
        }

        public double X
        {
            get { return _src.Bounds.X; }
        }
        public double Y
        {
            get { return _src.Bounds.Y; }
        }

        private Point _p1 = new Point(0, 0);
        public Point P1
        {
            get { return _p1; }
            private set { _p1 = value; RaisePropertyChanged("P1"); }
        }
        private Point _p2 = new Point(0, 0);
        public Point P2
        {
            get { return _p2; }
            private set { _p2 = value; RaisePropertyChanged("P2"); }
        }

        private bool _dragging = false;
        public bool IsDragging
        {
            get { return _dragging; }
            set { _dragging = value; RaisePropertyChanged("IsDragging"); }
        }

        public void MoveTo(double nx, double ny)
        {
            _src.MoveTo(nx, ny);
            RaisePropertyChanged("X", "Y");
        }
    }

    static class BitmapExtensions
    {
        public static BitmapImage ToBitmapImage(this System.Drawing.Bitmap bmp)
        {
            BitmapImage img = new BitmapImage();
            using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
            {
                bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Png);

                memory.Seek(0, System.IO.SeekOrigin.Begin);
                img.BeginInit();
                img.StreamSource = memory;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
            }
            return img;
        }
    }
}

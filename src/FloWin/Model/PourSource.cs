using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace FloWin.Model
{
    public class PourSource
    {
        private Rect _bounds;
        public Rect Bounds {
            get { return _bounds; }
            set { _bounds = value; }
        }

        public Point SrcPoint1 { get; set; }
        public Point SrcPoint2 { get; set; }

        public PourSource()
        {
            _bounds = new Rect(100, 100, 40, 40);
            SrcPoint1 = new Point(0, _bounds.Height);
            SrcPoint2 = new Point(_bounds.Width, _bounds.Height);
        }

        public void MoveTo(double x, double y)
        {
            _bounds.X = x; _bounds.Y = y;
        }
    }
}

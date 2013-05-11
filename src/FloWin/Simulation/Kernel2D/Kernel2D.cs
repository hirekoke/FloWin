using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloWin.Simulation.Kernel2D
{
    class Kernel2D
    {
        private static double _h;
        public static double H
        {
            get { return _h; }
            set
            {
                _h = value;
                Poly6.H = _h;
                Spiky.H = _h;
                Viscosity.H = _h;
                Spline.H = _h;
                SuperGaussian.H = _h;
                ForthOrderWeighting.H = _h;
            }
        }
    }
}

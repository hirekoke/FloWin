using System;
using System.Windows;

namespace FloWin.Simulation.Kernel2D
{
    /// <summary>
    /// Poly6 カーネル関数。密度計算などに用いる。
    /// </summary>
    class Poly6
    {
        /// <summary>有効半径</summary>
        private static double _h;
        /// <summary>有効半径を取得・設定する</summary>
        public static double H
        {
            get { return _h; }
            set
            {
                _h = value;

                _h2 = _h * _h;

                double tmp = 1 / (Math.PI * Math.Pow(_h, 8));
                _cPoly6 = 4 * tmp;
                _CGradient = 24 * tmp;
            }
        }

        /// <summary>有効半径二乗</summary>
        private static double _h2;
        /// <summary>関数値計算用係数</summary>
        private static double _cPoly6;
        /// <summary>勾配・ラプラシアン計算用係数</summary>
        private static double _CGradient;

        /// <summary>
        /// Poly6カーネル関数値の計算
        /// </summary>
        /// <param name="r2">距離の二乗</param>
        /// <returns>関数値</returns>
        public static double Func(double r2)
        {
            if (r2 >= 0 && r2 < _h2)
            {
                return _cPoly6 * Math.Pow(_h2 - r2, 3);
            }
            return 0.0;
        }

        /// <summary>
        /// Poly6カーネル関数勾配値の計算
        /// </summary>
        public static void Gradient(Vector rv, ref Vector result)
        {
            double r2 = rv.LengthSquared;
            if (r2 >= 0 && r2 < _h2)
            {
                double q = _h2 - r2;
                result = _CGradient * q * q * rv;
                return;
            }
            result.X = 0; result.Y = 0;
        }

        /// <summary>
        /// Poly6カーネル関数ラプラシアンの計算
        /// </summary>
        /// <param name="r2">距離の二乗</param>
        /// <returns>ラプラシアンの値</returns>
        public static double Laplacian(double r2)
        {
            if (r2 >= 0 && r2 < _h2)
            {
                double q = _h2 - r2;
                return - _CGradient * (3 * q * q - 4 * r2 * q);
            }
            return 0.0;
        }
    }
}

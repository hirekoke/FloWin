using System;
using System.Windows;

namespace FloWin.Simulation.Kernel2D
{
    /// <summary>
    /// Spline関数
    /// </summary>
    class Spline
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

                _ih = 1 / _h; _ih2 = 1 / (_h * _h); _ih3 = 1 / (_h * _h * _h);

                _CSpline = 10 * _ih2 / (7 * Math.PI);
                _CSplineGradient = 15 / (14 * Math.PI * Math.Pow(_h, 4));
                _CSplineLaplacian = 30 / (7 * Math.PI * Math.Pow(_h, 4));
            }
        }

        private static double _ih; // 逆数
        private static double _ih2; // 逆数
        private static double _ih3; // 逆数

        /// <summary>関数値計算用係数</summary>
        private static double _CSpline;
        /// <summary>勾配計算用係数</summary>
        private static double _CSplineGradient;
        /// <summary>ラプラシアン計算用係数</summary>
        private static double _CSplineLaplacian;

        /// <summary>
        /// Viscosityカーネル関数値の計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>関数値</returns>
        public static double Func(double r)
        {
            double rih = r * _ih;
            if (rih >= 0 && rih < 1)
            {
                return _CSpline * (1 - 1.5 * rih * rih + 0.75 * rih * rih * rih);
            }
            else if (rih < 2)
            {
                return _CSpline * 0.25 * Math.Pow(2 - rih, 3);
            }
            return 0.0;
        }

        /// <summary>
        /// Viscosityカーネル関数勾配値の計算
        /// </summary>
        /// <param name="r">距離</param>
        public static double Gradient(double r)
        {
            double rih = r * _ih;
            if (rih >= 0 && rih < 1)
            {
                return _CSplineGradient * (0.75 * rih - 1);
            }
            else if (rih < 2)
            {
                double q = 2 - rih;
                return -_CSplineGradient * q * q * rih;
            }
            return 0;
        }

        /// <summary>
        /// Viscosityカーネル関数ラプラシアンの計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>ラプラシアンの値</returns>
        public static double Laplacian(double r)
        {
            double rih = r * _ih;
            if (rih >= 0 && rih < 1)
            {
                return _CSplineLaplacian * (1.5 * rih - 1);
            }
            else if (rih < 2)
            {
                return _CSplineLaplacian * (1 - 0.5 * rih);
            }
            return 0.0;
        }
    }
}

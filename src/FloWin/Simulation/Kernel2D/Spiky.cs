using System;
using System.Windows;

namespace FloWin.Simulation.Kernel2D
{
    /// <summary>
    /// Spikyカーネル関数。圧力項計算に用いる。
    /// </summary>
    class Spiky
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

                double tmp = 1 / (Math.PI * Math.Pow(_h, 5));
                _CSpiky = tmp * 10;
                _CSpikyGradient = -30 * tmp;
                _CSpikyLaplacian = -60 * tmp;
            }
        }

        /// <summary>関数値計算用係数</summary>
        private static double _CSpiky;
        /// <summary>勾配計算用係数</summary>
        private static double _CSpikyGradient;
        /// <summary>ラプラシアン計算用係数</summary>
        private static double _CSpikyLaplacian;

        /// <summary>
        /// Spikyカーネル関数値の計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>関数値</returns>
        public static double Func(double r)
        {
            if (r >= 0 && r < _h)
            {
                return _CSpiky * Math.Pow(_h - r, 3);
            }
            return 0.0;
        }

        /// <summary>
        /// Spikyカーネル関数勾配値の計算
        /// </summary>
        /// <param name="rv">位置の差ベクトル</param>
        public static void Gradient(Vector rv, Vector ov, ref Vector result)
        {
            double r = rv.Length;
            Vector nv = new Vector(rv.X, rv.Y);
            if (r == 0)
            {
                nv.X = ov.X; nv.Y = ov.Y;
            }
            nv.Normalize();
            if (r >= 0 && r < _h)
            {
                double q = _h - r;
                result = _CSpikyGradient * q * q * nv;
                return;
            }
            result.X = 0; result.Y = 0;
        }

        /// <summary>
        /// Spikyカーネル関数ラプラシアンの計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>ラプラシアンの値</returns>
        public static double Laplacian(double r)
        {
            if (r > 0 && r < _h)
            {
                double q = _h - r;
                return _CSpikyLaplacian * (q * q / r - q);
            }
            return 0.0;
        }
    }
}

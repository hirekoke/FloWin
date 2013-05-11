using System;
using System.Windows;

namespace FloWin.Simulation.Kernel2D
{
    /// <summary>
    /// Gaussian関数
    /// </summary>
    class ForthOrderWeighting
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

                _ih = 1 / _h;

                _CFow = 63.0 / (478.0 * Math.PI * _h * _h);
                _CFowGradient = -945.0 / (478.0 * Math.PI * Math.Pow(_h, 3));
                _CFowLaplacian = 5670.0 / (239.0 * Math.PI * Math.Pow(_h, 4));
            }
        }

        private static double _ih; // 逆数

        /// <summary>関数値計算用係数</summary>
        private static double _CFow;
        /// <summary>勾配計算用係数</summary>
        private static double _CFowGradient;
        /// <summary>ラプラシアン計算用係数</summary>
        private static double _CFowLaplacian;

        /// <summary>
        /// Viscosityカーネル関数値の計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>関数値</returns>
        public static double Func(double r)
        {
            double rih3 = 3.0 * r * _ih;
            if (rih3 >= 0 && rih3 < 1)
            {
                return _CFow * (Math.Pow(3 - rih3, 5) - 6.0 * Math.Pow(2 - rih3, 5) + 15.0 * Math.Pow(1 - rih3, 5));
            }
            else if (rih3 < 2)
            {
                return _CFow * (Math.Pow(3 - rih3, 5) - 6.0 * Math.Pow(2 - rih3, 5));
            }
            else if (rih3 < 3)
            {
                return _CFow * Math.Pow(3 - rih3, 5);
            }
            return 0.0;
        }

        /// <summary>
        /// Viscosityカーネル関数勾配値の計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <param name="v">相対位置ベクトル</param>
        /// <param name="result">結果の勾配値を保存するベクトル</param>
        public static void Gradient(double r, Vector v, ref Vector result)
        {
            double rih3 = 3.0 * r * _ih;
            if (rih3 >= 0 && rih3 < 1)
            {
                result = v / r * _CFowGradient * (Math.Pow(3.0 - rih3, 4) - 6.0 * Math.Pow(2.0 - rih3, 4) + 15.0 * Math.Pow(1.0 - rih3, 4));
                return;
            }
            else if (rih3 < 2)
            {
                result = v / r * _CFowGradient * (Math.Pow(3.0 - rih3, 4) - 6.0 * Math.Pow(2.0 - rih3, 4));
                return;
            }
            else if (rih3 < 3)
            {
                result = v / r * _CFowGradient * Math.Pow(3.0 - rih3, 4);
                return;
            }
            result.X = 0.0; result.Y = 0.0;
        }

        /// <summary>
        /// Viscosityカーネル関数ラプラシアンの計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>ラプラシアンの値</returns>
        public static double Laplacian(double r)
        {
            double rih3 = 3.0 * r * _ih;
            if (rih3 >= 0 && rih3 < 1)
            {
                return _CFowLaplacian * (Math.Pow(3.0 - rih3, 3) - 6.0 * Math.Pow(2.0 - rih3, 3) + 15.0 * Math.Pow(1.0 - rih3, 3));
            }
            else if (rih3 < 2)
            {
                return _CFowLaplacian * (Math.Pow(3.0 - rih3, 3) - 6.0 * Math.Pow(2.0 - rih3, 3));
            }
            else if (rih3 < 3)
            {
                return _CFowLaplacian * Math.Pow(3.0 - rih3, 3);
            }
            return 0.0;
        }
    }
}

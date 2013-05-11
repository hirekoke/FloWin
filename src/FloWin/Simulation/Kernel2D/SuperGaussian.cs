using System;
using System.Windows;

namespace FloWin.Simulation.Kernel2D
{
    /// <summary>
    /// Gaussian関数
    /// </summary>
    class SuperGaussian
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

                _ih2 = 1 / (_h * _h);

                _CGaussian = 1 / (Math.Pow(Math.PI, 1.5) * _h * _h * _h);
                _CGaussianGradient = -2 * _CGaussian;
                _CGaussianLaplacian = 2 * _CGaussian;
            }
        }

        private static double _ih2; // 逆数

        /// <summary>関数値計算用係数</summary>
        private static double _CGaussian;
        /// <summary>勾配計算用係数</summary>
        private static double _CGaussianGradient;
        /// <summary>ラプラシアン計算用係数</summary>
        private static double _CGaussianLaplacian;

        /// <summary>
        /// Viscosityカーネル関数値の計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>関数値</returns>
        public static double Func(double r)
        {
            return _CGaussian * (2.5 - r * r) * Math.Exp(-r * r * _ih2);
        }

        /// <summary>
        /// Viscosityカーネル関数勾配値の計算
        /// </summary>
        /// <param name="r">距離</param>
        public static double Gradient(double r)
        {
            double tmp = r * r * _ih2;
            return -_CGaussianGradient * (1 + 2.5 * _ih2 - tmp) * Math.Exp(-tmp);
        }

        /// <summary>
        /// Viscosityカーネル関数ラプラシアンの計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>ラプラシアンの値</returns>
        public static double Laplacian(double r)
        {
            double tmp = r * r * _ih2;
            return _CGaussianLaplacian * (2 * tmp - (1 + 2.5 * _ih2 - tmp) * (1 - 2 * tmp)) * Math.Exp(-tmp);
        }
    }
}

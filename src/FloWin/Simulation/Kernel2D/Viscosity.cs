using System;
using System.Windows;

namespace FloWin.Simulation.Kernel2D
{
    /// <summary>
    /// Viscosityカーネル関数。粘性拡散項計算に用いる。
    /// </summary>
    class Viscosity
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

                _h2 = _h * _h; _h3 = _h * _h * _h;
                _ih = 1 / _h;  _ih2 = 1 / _h2; _ih3 = 1 / _h3;

                _CViscosity = 10 / (3 * Math.PI * _h * _h);
                _CViscosityGradient = 10 / (3 * Math.PI * Math.Pow(_h, 4));
                _CViscosityLaplacian = 20 / (3 * Math.PI * Math.Pow(_h, 5));
            }
        }

        private static double _ih; // 逆数
        private static double _h2;
        private static double _ih2; // 逆数
        private static double _h3;
        private static double _ih3; // 逆数

        /// <summary>関数値計算用係数</summary>
        private static double _CViscosity;
        /// <summary>勾配計算用係数</summary>
        private static double _CViscosityGradient;
        /// <summary>ラプラシアン計算用係数</summary>
        private static double _CViscosityLaplacian;

        /// <summary>
        /// Viscosityカーネル関数値の計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>関数値</returns>
        public static double Func(double r)
        {
            if (r >= 0 && r < _h)
            {
                return _CViscosity * (-0.5 * r * r * r * _ih3 + r * r * _ih2 + 0.5 * _h / r - 1);
            }
            return 0.0;
        }

        /// <summary>
        /// Viscosityカーネル関数勾配値の計算
        /// </summary>
        public static void Gradient(Vector rv, ref Vector result)
        {

        }

        /// <summary>
        /// Viscosityカーネル関数ラプラシアンの計算
        /// </summary>
        /// <param name="r">距離</param>
        /// <returns>ラプラシアンの値</returns>
        public static double Laplacian(double r)
        {
            if (r >= 0 && r < _h)
            {
                return _CViscosityLaplacian * (_h - r);
            }
            return 0.0;
        }
    }
}

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace FloWin.Effect
{
    public class LiquidEffect : ShaderEffect
    {
        #region Constructors

        static LiquidEffect()
        {
            _pixelShader.UriSource = EffectUtil.MakePackUri("Effect/LiquidEffect.ps");
        }

        public LiquidEffect()
        {
            this.PixelShader = _pixelShader;

            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ThreasholdProperty);
            UpdateShaderValue(FillColorProperty);
        }

        #endregion

        #region Dependency Properties

        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(LiquidEffect), 0);

        /// <summary>描画するかどうかの閾値, 0-1</summary>
        public double Threashold
        {
            get { return (double)GetValue(ThreasholdProperty); }
            set { SetValue(ThreasholdProperty, value); }
        }
        public static readonly DependencyProperty ThreasholdProperty =
            DependencyProperty.Register("Threashold", typeof(double), typeof(LiquidEffect),
            new UIPropertyMetadata(1.0, PixelShaderConstantCallback(0)));

        /// <summary>塗りつぶし色</summary>
        public Color FillColor
        {
            get { return (Color)GetValue(FillColorProperty); }
            set { SetValue(FillColorProperty, value); }
        }
        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register("FillColor", typeof(Color), typeof(LiquidEffect),
            new UIPropertyMetadata(Colors.AliceBlue, PixelShaderConstantCallback(1)));
        #endregion

        #region Member Data

        private static PixelShader _pixelShader = new PixelShader();

        #endregion

    }
}

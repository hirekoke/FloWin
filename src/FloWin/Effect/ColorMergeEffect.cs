using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Reflection;

namespace FloWin.Effect
{
    public class ColorMergeEffect : ShaderEffect
    {
        public enum ColorKey : int
        {
            Red = 1,
            Green = 2,
            Blue = 3,
        }

        private PixelShader _pixelShader = new PixelShader();

        #region Constructors

        static ColorMergeEffect()
        {
        }

        public ColorMergeEffect()
        {
            _pixelShader.UriSource = EffectUtil.MakePackUri("Effect/ColorMergeEffect.ps");
            this.PixelShader = _pixelShader;

            UpdateShaderValue(InputProperty);
            UpdateShaderValue(Input2Property);
            UpdateShaderValue(KeyProperty);
        }

        #endregion

        #region Dependency Properties

        /// <summary>入力画像1</summary>
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", 
            typeof(ColorMergeEffect), 0);

        /// <summary>入力画像2</summary>
        public Brush Input2
        {
            get { return (Brush)GetValue(Input2Property); }
            set { SetValue(Input2Property, value); }
        }
        public static readonly DependencyProperty Input2Property =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input2", 
            typeof(ColorMergeEffect), 1);

        public double Key
        {
            get { return (double)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, (double)value); }
        }
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register("Key", typeof(double), typeof(ColorMergeEffect),
            new UIPropertyMetadata(1.0, PixelShaderConstantCallback(0)));
        #endregion

    }

}

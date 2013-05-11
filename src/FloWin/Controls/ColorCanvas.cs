using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FloWin.Controls
{
    /// <summary>
    /// </summary>
    [TemplatePart(Name = PART_ColorShadingCanvas, Type = typeof(Canvas))]
    [TemplatePart(Name = PART_ColorShadeSelector, Type = typeof(Canvas))]
    [TemplatePart(Name = PART_SpectrumSlider, Type = typeof(ColorSpectrumSlider))]
    [TemplatePart(Name = PART_HexadecimalTextBox, Type = typeof(TextBox))]
    public class ColorCanvas : Control
    {
        private const string PART_ColorShadingCanvas = "PART_ColorShadingCanvas";
        private const string PART_ColorShadeSelector = "PART_ColorShadeSelector";
        private const string PART_SpectrumSlider = "PART_SpectrumSlider";
        private const string PART_HexadecimalTextBox = "PART_HexadecimalTextBox";

        private TranslateTransform _colorShadeSelectorTransform = new TranslateTransform();
        private Canvas _colorShadingCanvas;
        private Canvas _colorShadeSelector;
        private ColorSpectrumSlider _spectrumSlider;
        private TextBox _hexadecimalTextBox;
        private Point? _currentColorPosition;
        private bool _surpressPropertyChanged;

        static ColorCanvas()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorCanvas), new FrameworkPropertyMetadata(typeof(ColorCanvas)));
        }

        #region SelectedColor
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorCanvas),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));
        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }
        private static void OnSelectedColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ColorCanvas colorCanvas = o as ColorCanvas;
            if (colorCanvas != null)
                colorCanvas.OnSelectedColorChanged((Color)e.OldValue, (Color)e.NewValue);
        }
        protected virtual void OnSelectedColorChanged(Color oldValue, Color newValue)
        {
            SetHexadecimalStringProperty(ColorUtilities.GetFormatedColorString(newValue), false);
            UpdateRGBValues(newValue);
            UpdateColorShadeSelectorPosition(newValue);

            RoutedPropertyChangedEventArgs<Color> args = new RoutedPropertyChangedEventArgs<Color>(oldValue, newValue);
            args.RoutedEvent = SelectedColorChangedEvent;
            RaiseEvent(args);
        }
        #endregion

        #region ARGB

        #region A
        public static readonly DependencyProperty AProperty = 
            DependencyProperty.Register("A", typeof(byte), typeof(ColorCanvas), 
            new UIPropertyMetadata((byte)255, OnAChanged));
        public byte A
        {
            get { return (byte)GetValue(AProperty); }
            set { SetValue(AProperty, value); }
        }
        private static void OnAChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ColorCanvas colorCanvas = o as ColorCanvas;
            if (colorCanvas != null)
                colorCanvas.OnAChanged((byte)e.OldValue, (byte)e.NewValue);
        }
        protected virtual void OnAChanged(byte oldValue, byte newValue)
        {
            if (!_surpressPropertyChanged) UpdateSelectedColor();
        }
        #endregion //A

        #region R
        public static readonly DependencyProperty RProperty =
            DependencyProperty.Register("R", typeof(byte), typeof(ColorCanvas),
            new UIPropertyMetadata((byte)0, OnRChanged));
        public byte R
        {
            get { return (byte)GetValue(RProperty); }
            set { SetValue(RProperty, value); }
        }
        private static void OnRChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ColorCanvas colorCanvas = o as ColorCanvas;
            if (colorCanvas != null)
                colorCanvas.OnRChanged((byte)e.OldValue, (byte)e.NewValue);
        }
        protected virtual void OnRChanged(byte oldValue, byte newValue)
        {
            if (!_surpressPropertyChanged) UpdateSelectedColor();
        }
        #endregion //R

        #region G
        public static readonly DependencyProperty GProperty =
            DependencyProperty.Register("G", typeof(byte), typeof(ColorCanvas),
            new UIPropertyMetadata((byte)0, OnGChanged));
        public byte G
        {
            get { return (byte)GetValue(GProperty); }
            set { SetValue(GProperty, value); }
        }
        private static void OnGChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ColorCanvas colorCanvas = o as ColorCanvas;
            if (colorCanvas != null)
                colorCanvas.OnGChanged((byte)e.OldValue, (byte)e.NewValue);
        }
        protected virtual void OnGChanged(byte oldValue, byte newValue)
        {
            if (!_surpressPropertyChanged) UpdateSelectedColor();
        }
        #endregion //G

        #region B
        public static readonly DependencyProperty BProperty =
            DependencyProperty.Register("B", typeof(byte), typeof(ColorCanvas), 
            new UIPropertyMetadata((byte)0, OnBChanged));
        public byte B
        {
            get { return (byte)GetValue(BProperty); }
            set { SetValue(BProperty, value); }
        }
        private static void OnBChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ColorCanvas colorCanvas = o as ColorCanvas;
            if (colorCanvas != null)
                colorCanvas.OnBChanged((byte)e.OldValue, (byte)e.NewValue);
        }
        protected virtual void OnBChanged(byte oldValue, byte newValue)
        {
            if (!_surpressPropertyChanged) UpdateSelectedColor();
        }
        #endregion //B

        #endregion //RGB

        #region HexadecimalString

        public static readonly DependencyProperty HexadecimalStringProperty =
            DependencyProperty.Register("HexadecimalString", typeof(string), typeof(ColorCanvas),
            new UIPropertyMetadata("#FFFFFFFF", OnHexadecimalStringChanged, OnCoerceHexadecimalString));
        public string HexadecimalString
        {
            get { return (string)GetValue(HexadecimalStringProperty); }
            set { SetValue(HexadecimalStringProperty, value); }
        }
        private static void OnHexadecimalStringChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ColorCanvas colorCanvas = o as ColorCanvas;
            if (colorCanvas != null) colorCanvas.OnHexadecimalStringChanged((string)e.OldValue, (string)e.NewValue);
        }
        protected virtual void OnHexadecimalStringChanged(string oldValue, string newValue)
        {
            string newColorString = ColorUtilities.GetFormatedColorString(newValue);
            string currentColorString = ColorUtilities.GetFormatedColorString(SelectedColor);
            if (!currentColorString.Equals(newColorString))
                UpdateSelectedColor((Color)ColorConverter.ConvertFromString(newColorString));

            SetHexadecimalTextBoxTextProperty(newValue);
        }

        private static object OnCoerceHexadecimalString(DependencyObject d, object basevalue)
        {
            var colorCanvas = (ColorCanvas)d;
            if (colorCanvas == null) return basevalue;

            return colorCanvas.OnCoerceHexadecimalString(basevalue);
        }
        private object OnCoerceHexadecimalString(object newValue)
        {
            var value = newValue as string;
            string retValue = value;

            try
            {
                ColorConverter.ConvertFromString(value);
            }
            catch
            {
                //When HexadecimalString is changed via Code-Behind and hexadecimal format is bad, throw.
                throw new System.IO.InvalidDataException("Color provided is not in the correct format.");
            }

            return retValue;
        }

        #endregion //HexadecimalString

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_colorShadingCanvas != null)
            {
                _colorShadingCanvas.MouseLeftButtonDown -= ColorShadingCanvas_MouseLeftButtonDown;
                _colorShadingCanvas.MouseLeftButtonUp -= ColorShadingCanvas_MouseLeftButtonUp;
                _colorShadingCanvas.MouseMove -= ColorShadingCanvas_MouseMove;
                _colorShadingCanvas.SizeChanged -= ColorShadingCanvas_SizeChanged;
            }
            _colorShadingCanvas = this.Template.FindName(PART_ColorShadingCanvas, this) as Canvas;
            if (_colorShadingCanvas != null)
            {
                _colorShadingCanvas.MouseLeftButtonDown += ColorShadingCanvas_MouseLeftButtonDown;
                _colorShadingCanvas.MouseLeftButtonUp += ColorShadingCanvas_MouseLeftButtonUp;
                _colorShadingCanvas.MouseMove += ColorShadingCanvas_MouseMove;
                _colorShadingCanvas.SizeChanged += ColorShadingCanvas_SizeChanged;
            }

            _colorShadeSelector = this.Template.FindName(PART_ColorShadeSelector, this) as Canvas;
            if (_colorShadeSelector != null)
                _colorShadeSelector.RenderTransform = _colorShadeSelectorTransform;

            if (_spectrumSlider != null)
                _spectrumSlider.ValueChanged -= SpectrumSlider_ValueChanged;
            _spectrumSlider = this.Template.FindName(PART_SpectrumSlider, this) as ColorSpectrumSlider;
            if (_spectrumSlider != null)
                _spectrumSlider.ValueChanged += SpectrumSlider_ValueChanged;

            if (_hexadecimalTextBox != null)
                _hexadecimalTextBox.LostFocus -= new RoutedEventHandler(HexadecimalTextBox_LostFocus);
            _hexadecimalTextBox = this.Template.FindName(PART_HexadecimalTextBox, this) as TextBox;
            if (_hexadecimalTextBox != null)
                _hexadecimalTextBox.LostFocus += new RoutedEventHandler(HexadecimalTextBox_LostFocus);

            UpdateRGBValues(SelectedColor);
            UpdateColorShadeSelectorPosition(SelectedColor);

            // When changing theme, HexadecimalString needs to be set since it is not binded.
            SetHexadecimalTextBoxTextProperty(ColorUtilities.GetFormatedColorString(SelectedColor));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            //hitting enter on textbox will update Hexadecimal string
            if (e.Key == Key.Enter && e.OriginalSource is TextBox)
            {
                TextBox textBox = (TextBox)e.OriginalSource;
                if (textBox.Name == PART_HexadecimalTextBox)
                    SetHexadecimalStringProperty(textBox.Text, true);
            }
        }

        #region Event Handlers

        void ColorShadingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(_colorShadingCanvas);
            UpdateColorShadeSelectorPositionAndCalculateColor(p, true);
            _colorShadingCanvas.CaptureMouse();
        }

        void ColorShadingCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _colorShadingCanvas.ReleaseMouseCapture();
        }

        void ColorShadingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(_colorShadingCanvas);
                UpdateColorShadeSelectorPositionAndCalculateColor(p, true);
                Mouse.Synchronize();
            }
        }

        void ColorShadingCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_currentColorPosition != null)
            {
                Point _newPoint = new Point
                {
                    X = ((Point)_currentColorPosition).X * e.NewSize.Width,
                    Y = ((Point)_currentColorPosition).Y * e.NewSize.Height
                };

                UpdateColorShadeSelectorPositionAndCalculateColor(_newPoint, false);
            }
        }

        private void SpectrumSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_currentColorPosition != null)
                CalculateColor((Point)_currentColorPosition);
        }

        private void HexadecimalTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textbox = sender as TextBox;
            SetHexadecimalStringProperty(textbox.Text, true);
        }

        #endregion //Event Handlers

        #region Events

        public static readonly RoutedEvent SelectedColorChangedEvent = EventManager.RegisterRoutedEvent("SelectedColorChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<Color>), typeof(ColorCanvas));
        public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged
        {
            add
            {
                AddHandler(SelectedColorChangedEvent, value);
            }
            remove
            {
                RemoveHandler(SelectedColorChangedEvent, value);
            }
        }

        #endregion //Events

        #region Methods

        private void UpdateSelectedColor()
        {
            SelectedColor = Color.FromArgb(A, R, G, B);
        }

        private void UpdateSelectedColor(Color color)
        {
            SelectedColor = Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private void UpdateRGBValues(Color color)
        {
            _surpressPropertyChanged = true;

            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;

            _surpressPropertyChanged = false;
        }

        private void UpdateColorShadeSelectorPositionAndCalculateColor(Point p, bool calculateColor)
        {
            // 境界
            if (p.Y < 0) p.Y = 0;
            if (p.X < 0) p.X = 0;
            if (p.X > _colorShadingCanvas.ActualWidth) p.X = _colorShadingCanvas.ActualWidth;
            if (p.Y > _colorShadingCanvas.ActualHeight) p.Y = _colorShadingCanvas.ActualHeight;

            _colorShadeSelectorTransform.X = p.X - (_colorShadeSelector.Width / 2);
            _colorShadeSelectorTransform.Y = p.Y - (_colorShadeSelector.Height / 2);

            p.X = p.X / _colorShadingCanvas.ActualWidth;
            p.Y = p.Y / _colorShadingCanvas.ActualHeight;

            _currentColorPosition = p;

            if (calculateColor) CalculateColor(p);
        }

        private void UpdateColorShadeSelectorPosition(Color color)
        {
            if (_spectrumSlider == null || _colorShadingCanvas == null)
                return;

            _currentColorPosition = null;

            HsvColor hsv = ColorUtilities.ConvertRgbToHsv(color.R, color.G, color.B);

            if (!(color.R == color.G && color.R == color.B))
                _spectrumSlider.Value = hsv.H;

            Point p = new Point(hsv.S, 1 - hsv.V);

            _currentColorPosition = p;

            _colorShadeSelectorTransform.X = (p.X * _colorShadingCanvas.Width) - 5;
            _colorShadeSelectorTransform.Y = (p.Y * _colorShadingCanvas.Height) - 5;
        }

        /// <summary>現在の座標に応じた色を計算する</summary>
        /// <param name="p">PART_ColorShadingCanvas 内での座標</param>
        private void CalculateColor(Point p)
        {
            HsvColor hsv = new HsvColor(360 - _spectrumSlider.Value, 1, 1) { S = p.X, V = 1 - p.Y };
            var currentColor = ColorUtilities.ConvertHsvToRgb(hsv.H, hsv.S, hsv.V);
            currentColor.A = A;
            SelectedColor = currentColor;
            SetHexadecimalStringProperty(ColorUtilities.GetFormatedColorString(SelectedColor), false);
        }

        private void SetHexadecimalStringProperty(string newValue, bool modifyFromUI)
        {
            if (modifyFromUI)
            {
                try
                {
                    ColorConverter.ConvertFromString(newValue);
                    HexadecimalString = newValue;
                }
                catch
                {
                    // When HexadecimalString is changed via UI and hexadecimal format is bad,
                    // keep the previous HexadecimalString.
                    SetHexadecimalTextBoxTextProperty(HexadecimalString);
                }
            }
            else
            {
                // When HexadecimalString is changed via Code-Behind,
                // hexadecimal format will be evaluated in OnCoerceHexadecimalString()
                HexadecimalString = newValue;
            }
        }

        /// <summary>16進数テキストボックスの表示を変更する</summary>
        private void SetHexadecimalTextBoxTextProperty(string newValue)
        {
            if (_hexadecimalTextBox != null) _hexadecimalTextBox.Text = newValue;
        }
        #endregion //Methods
    }


    public class ColorToSolidColorBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a Color to a SolidColorBrush.
        /// </summary>
        /// <param name="value">The Color produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted SolidColorBrush. If the method returns null, the valid null value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null) return new SolidColorBrush((Color)value);
            return value;
        }

        /// <summary>
        /// Converts a SolidColorBrush to a Color.
        /// </summary>
        /// <remarks>Currently not used in toolkit, but provided for developer use in their own projects</remarks>
        /// <param name="value">The SolidColorBrush that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null) return ((SolidColorBrush)value).Color;
            return value;
        }
    }
}

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
    public class ColorPicker : Control
    {
        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), 
                new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        #region
        private static readonly RoutedEvent SelectedColorChangedEvent = EventManager.RegisterRoutedEvent( "SelectedColorChanged", RoutingStrategy.Bubble, typeof( RoutedPropertyChangedEventHandler<Color> ), typeof( ColorPicker ) );
        public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged
        {
          add { AddHandler( SelectedColorChangedEvent, value ); }
          remove { RemoveHandler( SelectedColorChangedEvent, value ); }
        }
        #endregion

        #region ButtonStyle
        public static readonly DependencyProperty ButtonStyleProperty = 
            DependencyProperty.Register("ButtonStyle", typeof(Style), typeof(ColorPicker));
        public Style ButtonStyle
        {
            get { return (Style)GetValue(ButtonStyleProperty); }
            set { SetValue(ButtonStyleProperty, value); }
        }
        #endregion

        #region IsOpen
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(ColorPicker), 
            new UIPropertyMetadata(false));
        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }
        #endregion

        #region SelectedColor
        public static readonly DependencyProperty SelectedColorProperty = 
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorPicker),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnSelectedColorPropertyChanged)));
        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }
        private static void OnSelectedColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorPicker colorPicker = (ColorPicker)d;
            if (colorPicker != null) colorPicker.OnSelectedColorChanged((Color)e.OldValue, (Color)e.NewValue);
        }
        private void OnSelectedColorChanged(Color oldValue, Color newValue)
        {
            SelectedColorText = ColorUtilities.GetFormatedColorString(newValue);

            RoutedPropertyChangedEventArgs<Color> args = new RoutedPropertyChangedEventArgs<Color>(oldValue, newValue);
            args.RoutedEvent = ColorPicker.SelectedColorChangedEvent;
            RaiseEvent(args);
        }
        #endregion

        #region SelectedColorText
        public static readonly DependencyProperty SelectedColorTextProperty = 
            DependencyProperty.Register("SelectedColorText", typeof(string), typeof(ColorPicker), 
            new UIPropertyMetadata("Black"));
        public string SelectedColorText
        {
            get { return (string)GetValue(SelectedColorTextProperty); }
            protected set { SetValue(SelectedColorTextProperty, value); }
        }
        #endregion

    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

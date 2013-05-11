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
using System.Windows.Shapes;

using FloWin.ViewModel;

namespace FloWin.View
{
    /// <summary>
    /// ConfigWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigWindow : Window
    {
        ConfigViewModel _viewModel;

        public ConfigWindow()
        {
            _viewModel = new ConfigViewModel();
            DataContext = _viewModel;
            InitializeComponent();
        }

        private EventHandler<GlobalHook.KeyEventArgs> _globalHookHandler = null;
        private Brush _tbBorderBrush;
        private Thickness _tbBorderThickness;

        private void KeyBindTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)e.Source;
            KeyConfigViewModel kc = _viewModel.Keys[(int)tb.Tag];
            if (_globalHookHandler != null) GlobalHook.KeyDown -= _globalHookHandler;

            _tbBorderBrush = tb.BorderBrush;
            _tbBorderThickness = tb.BorderThickness;

            tb.BorderBrush = SystemColors.MenuHighlightBrush;
            tb.BorderThickness = new Thickness(2);
            _globalHookHandler = (gs, ge) =>
            {
                kc.Key = ge.Key;
                kc.ModifierKeys = ge.Modifiers;
            };
            GlobalHook.KeyDown += _globalHookHandler;
        }
        private void KeyBindTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)e.Source;
            tb.BorderBrush = _tbBorderBrush;
            tb.BorderThickness = _tbBorderThickness;

            if (_globalHookHandler != null) GlobalHook.KeyDown -= _globalHookHandler;
            _globalHookHandler = null;
        }
    }

    /// <summary>Key + ModifierKeys と キー文字列の変換, KeyConfigを介して行う</summary>
    public class KeysConverter : IMultiValueConverter
    {
        private Model.KeyConfig _keys = new Model.KeyConfig("dummy", "dummy", Key.None, ModifierKeys.None);

        // Key, ModifierKeys -> String
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length != 2) return null;
            if (values[0] is Key && values[1] is ModifierKeys)
            {
                _keys.Key = (Key)values[0];
                _keys.ModifierKeys = (ModifierKeys)values[1];
                return _keys.KeyToString();
            }
            else
            {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            object[] ret = new object[2];
            if (!(value is string))
            {
                ret[0] = Key.None;
                ret[1] = ModifierKeys.None;
                return ret;
            }
            else
            {
                _keys.ParseKey((string)value);
                ret[0] = _keys.Key;
                ret[1] = _keys.ModifierKeys;
                return ret;
            }
        }
    }
}

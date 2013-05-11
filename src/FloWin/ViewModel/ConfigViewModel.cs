using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

using FloWin.Model;

namespace FloWin.ViewModel
{
    /// <summary>ConfigのViewModel</summary>
    public class ConfigViewModel : Common.ViewModelBase
    {
        public ConfigViewModel()
        {
            Keys = new ObservableCollection<KeyConfigViewModel>();
            foreach (KeyConfig kc in Config.Instance.Keys)
            {
                Keys.Add(new KeyConfigViewModel(kc));
            }
        }

        public Common.DelegateCommand SaveCommand { get; private set; }
        public Common.DelegateCommand LoadDefaultCommand { get; private set; }
        public Common.DelegateCommand LoadCommand { get; private set; }

        public ObservableCollection<KeyConfigViewModel> Keys { get; set; }

        public bool DrawLiquid
        {
            get { return Config.Instance.DrawLiquid; }
            set
            {
                Config.Instance.DrawLiquid = value;
                RaisePropertyChanged("DrawLiquid");
            }
        }

        public Color LiquidColor
        {
            get { return Config.Instance.LiquidColor; }
            set
            {
                Config.Instance.LiquidColor = value;
                RaisePropertyChanged("LiquidColor");
            }
        }
    }

    /// <summary>Config.KeysのViewModel</summary>
    public class KeyConfigViewModel : Common.ViewModelBase
    {
        private KeyConfig Value;

        public KeyConfigViewModel(KeyConfig keyConfig)
        {
            Value = keyConfig;
        }
        public int Index { get { return Value.Index; } }
        public string Label { get { return Value.Label; } }
        public Key Key
        {
            get { return Value.Key; }
            set { Value.Key = value; RaisePropertyChanged("Key"); }
        }
        public ModifierKeys ModifierKeys
        {
            get { return Value.ModifierKeys; }
            set { Value.ModifierKeys = value; RaisePropertyChanged("ModifierKeys"); }
        }
    }
}

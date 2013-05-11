using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace FloWin.Model
{
    public sealed class Config
    {
        private const string CONFIG_FILE = "config.xml";

        private const string OP_EXIT = "Exit";
        private const string OP_PAUSE = "Pause";
        private const string OP_POUR = "Pour";
        private const string OP_DRAWWALL = "DrawWall";

        /// <summary>設定のインスタンス, 設定の値にはこれを通してアクセスする</summary>
        public static readonly Config Instance = new Config();

        static Config() { }
        /// <summary>コンストラクタ, 初期値をセットする</summary>
        private Config()
        {
            /// 設定ファイルを置くディレクトリを作る
            MakeDirs();

            /// 初期値
            FrameRate = 30;
            WinUpdateMSec = 500;

            DrawLiquid = true;
            LiquidColor = Color.FromArgb(128, 32, 32, 255);
            FpsLocation = new Point(10, 10);
            ParticleCountLocation = new Point(10, 40);
            WinEnumLocation = new Point(10, 80);

            /// キー設定初期値
            Exit = new KeyConfig(OP_EXIT, Properties.Resources.Command_Exit,
                Key.Escape, ModifierKeys.None);
            Pause = new KeyConfig(OP_PAUSE, Properties.Resources.Command_Pause,
                Key.F11, ModifierKeys.None);
            Pour = new KeyConfig(OP_POUR, Properties.Resources.Command_Pour,
                Key.F10, ModifierKeys.None);
            DrawWall = new KeyConfig(OP_DRAWWALL, Properties.Resources.Command_DrawWall,
                Key.W, ModifierKeys.Control | ModifierKeys.Alt);

            /// 全キー設定
            Keys = new List<KeyConfig>() { Exit, Pause, Pour, DrawWall };
        }

        #region 設定ファイル
        /// <summary>設定ファイルが存在する場合に設定ファイルを読み込み、値をセットする</summary>
        public void Load()
        {
            if (!File.Exists(FilePath)) return;

            // 読み込み
            XDocument doc = XDocument.Load(FilePath);
            if(doc.Root != null) ReadXml(doc.Root);
        }

        /// <summary>設定を設定ファイルに書き出す</summary>
        public void Save()
        {
            // 保存
            XElement elem = CreateXml();
            XDocument doc = new XDocument(elem);
            doc.Save(FilePath, SaveOptions.None);
        }

        private string _filePath = null;
        /// <summary>設定ファイルのパス</summary>
        public string FilePath
        {
            get
            {
                if (_filePath == null)
                {
                    _filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    Assembly asm = Assembly.GetEntryAssembly();
                    //_filePath += Path.DirectorySeparatorChar + ((AssemblyCompanyAttribute)asm.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0]).Company;
                    _filePath += Path.DirectorySeparatorChar + asm.GetName().Name;
                    _filePath += Path.DirectorySeparatorChar + CONFIG_FILE;
                }
                return _filePath;
            }
        }
        #endregion

        #region 設定値
        #region General
        /// <summary>理想フレームレート</summary>
        public double FrameRate { get; set; }

        /// <summary>ウィンドウ位置を更新する間隔</summary>
        public int WinUpdateMSec { get; set; }
        #endregion

        #region Render
        public bool DrawLiquid { get; set; }
        public Color LiquidColor { get; set; }

        public Point FpsLocation { get; set; }
        public Point ParticleCountLocation { get; set; }
        public Point WinEnumLocation { get; set; }
        #endregion

        #region Simulation
        #endregion

        #region KeyConfig
        public KeyConfig Exit { get; set; }
        public KeyConfig Pause { get; set; }
        public KeyConfig Pour { get; set; }
        public KeyConfig DrawWall { get; set; }

        public List<KeyConfig> Keys { get; private set; }
        #endregion

        #endregion //設定値

        #region XML
        private const string XML_ROOT = "config";

        private const string XML_ELEM_FRAMERATE = "fps";
        private const string XML_ELEM_WINUPDATE = "win_update";

        private const string XML_ELEM_LIQUID = "liquid";
        private const string XML_ATTR_DRAWLIQUID = "draw";
        private const string XML_ATTR_LIQUIDCOLOR = "color";

        private const string XML_ELEM_KEYS = "keys";

        /// <summary>設定ファイルを保存するディレクトリを作成する</summary>
        private void MakeDirs()
        {
            List<string> createDirs = new List<string>();
            string dir = Path.GetDirectoryName(FilePath);
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.Exists(dir)) break;
                createDirs.Add(dir.ToString());
                dir = Path.GetDirectoryName(dir);
            }
            foreach (var d in createDirs.Reverse<string>())
            {
                Directory.CreateDirectory(d);
            }
        }

        /// <summary>設定全体を表すXElementを作る</summary>
        private XElement CreateXml()
        {
            XElement elem = new XElement(XML_ROOT,
                new XElement(XML_ELEM_FRAMERATE, FrameRate.ToString()),
                new XElement(XML_ELEM_WINUPDATE, WinUpdateMSec.ToString()),

                new XElement(XML_ELEM_LIQUID,
                    new XAttribute(XML_ATTR_DRAWLIQUID, DrawLiquid ? "1" : "0"),
                    new XAttribute(XML_ATTR_LIQUIDCOLOR, LiquidColor.ToString())));

            XElement keysElem = new XElement(XML_ELEM_KEYS);
            foreach (KeyConfig kc in Keys) keysElem.Add(kc.CreateXml());

            elem.Add(keysElem);

            return elem;
        }

        /// <summary>XElementを読み込んで値をセットする</summary>
        private void ReadXml(XElement elem)
        {
            XElement e; XAttribute attr;
            
            /// frame rate
            e = elem.Element(XML_ELEM_FRAMERATE);
            if (e != null)
            {
                double fr;
                if (double.TryParse(e.Value, out fr)) FrameRate = fr;
            }

            /// window update span
            e = elem.Element(XML_ELEM_WINUPDATE);
            if (e != null)
            {
                int wu;
                if (int.TryParse(e.Value, out wu)) WinUpdateMSec = wu;
            }

            /// liquid rendering
            e = elem.Element(XML_ELEM_LIQUID);
            if (e != null && e.HasAttributes)
            {
                attr = e.Attribute(XML_ATTR_DRAWLIQUID);
                if (attr != null) DrawLiquid = (attr.Value.Trim() == "1");

                attr = e.Attribute(XML_ATTR_LIQUIDCOLOR);
                if (attr != null)
                {
                    Color newLiquidColor = LiquidColor;
                    try
                    {
                        newLiquidColor = (Color)ColorConverter.ConvertFromString(attr.Value);
                        LiquidColor = newLiquidColor;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        Debug.WriteLine(attr.Value);
                    }
                }
            }

            /// key config
            e = elem.Element(XML_ELEM_KEYS);
            if (e != null && e.HasElements)
                foreach (var ce in e.Elements()) 
                    KeyConfig.ReadXml(ce, Keys);
        }

        #endregion
    }

    /// <summary>
    /// 1つの操作に対するキー設定を表すクラス
    /// </summary>
    public sealed class KeyConfig
    {
        private static int cnt = 0;
        private int _index = 0;
        public int Index { get { return _index; } }

        /// <summary>操作名</summary>
        public string Name { get; set; }

        /// <summary>メニュー等に表示するラベル</summary>
        public string Label { get; set; }

        /// <summary>最終押下キー</summary>
        public Key Key { get; set; }
        /// <summary>修飾キー</summary>
        public ModifierKeys ModifierKeys { get; set; }

        internal KeyConfig(string name, string label, Key key, ModifierKeys modifiers)
        {
            _index = cnt++;
            Label = label;
            Name = name;
            Key = key;
            ModifierKeys = modifiers;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}:{2} - {3}",
                Index, Name, Label, KeyToString());
        }

        /// <summary>
        /// この設定のキーを文字列にする
        /// </summary>
        /// <returns>キーを表す文字列</returns>
        internal string KeyToString()
        {
            StringBuilder sb = new StringBuilder();
            if (ModifierKeys == ModifierKeys.None)
            {
                sb.Append(Key.ToString());
            }
            else
            {
                sb.Append(ModifierKeys.ToString())
                    .Append(" + ").Append(Key.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// 与えられたkey, modifiersがこの設定と合致するか否か
        /// </summary>
        /// <param name="key">テストするKey</param>
        /// <param name="modifiers">テストするModifierKeys</param>
        /// <returns>合致した場合true, しない場合false</returns>
        public bool Hit(Key key, ModifierKeys modifiers)
        {
            return key == Key && modifiers == ModifierKeys;
        }

        /// <summary>
        /// キー文字列をパースして設定にセットする
        /// <remarks>パースできないキーは無視する</remarks>
        /// </summary>
        /// <param name="str">パースするキー文字列(ex. "Alt, Control + A")</param>
        internal void ParseKey(string str)
        {
            var ss  = str.Split('+', ',').Select(s => s.Trim());
            ModifierKeys mks = ModifierKeys.None;
            Key = Key.None;
            foreach (var s in ss.Reverse())
            {
                if (Key == Key.None)
                {
                    Key k = Key.None;
                    if (Enum.TryParse(s, true, out k)) Key = k;
                }
                else
                {
                    // 最後の一個がKey, それ以前はModifiers
                    ModifierKeys mk = ModifierKeys.None;
                    if (Enum.TryParse(s, true, out mk))
                    {
                        mks |= mk;
                    }
                    else if (s.StartsWith("Left"))
                    {
                        if (Enum.TryParse(s.Substring("Left".Length), true, out mk))
                            mks |= mk;
                    }
                    else if (s.StartsWith("Right"))
                    {
                        if (Enum.TryParse(s.Substring("Right".Length), true, out mk))
                            mks |= mk;
                    }
                }
            }
            ModifierKeys = mks;
        }

        #region Write/Read
        private const string XML_ROOT = "key";
        private const string XML_ATTR_OP = "name";
        private const string XML_ATTR_KEY = "keys";

        /// <summary>このキー設定を表すXElementを作る</summary>
        internal XElement CreateXml()
        {
            return new XElement(XML_ROOT,
                new XAttribute(XML_ATTR_OP, Name),
                new XAttribute(XML_ATTR_KEY, KeyToString()));
        }

        /// <summary>キー設定を表すXElementを読み込む</summary>
        internal static void ReadXml(XElement keyElem, List<KeyConfig> keys)
        {
            if (!keyElem.HasAttributes) return;

            var attr = keyElem.Attribute(XML_ATTR_OP);
            if (attr == null) return;
            string op = attr.Value;

            attr = keyElem.Attribute(XML_ATTR_KEY);
            if (attr == null) return;
            string key = attr.Value;

            KeyConfig fkc = keys.Find(kc => kc.Name == op);
            fkc.ParseKey(key);
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Linq;

namespace FloWin.Model
{
    /// <summary>Window情報を保持する構造体</summary>
    class WindowInfo
    {
        /// <summary>Windowハンドル</summary>
        public IntPtr Handle;
        /// <summary>Windowタイトル</summary>
        public string Title;
        /// <summary>クラス名</summary>
        public string ClassName;
        /// <summary>プロセスID</summary>
        public int ProcessId;
        /// <summary>Window位置・サイズ</summary>
        public Int32Rect Rect;
        /// <summary>最小化されているかどうか</summary>
        public bool IsMinimized;
        /// <summary>最大化されているかどうか</summary>
        public bool IsMaximized;

        public int ZOrder;

        public Int32Rect PrevRect;

        /// <summary>構造体を生成する</summary>
        /// <param name="title">Windowタイトル</param>
        /// <param name="className">クラス名</param>
        /// <param name="procId">プロセスID</param>
        /// <param name="rect">Window位置・サイズ</param>
        /// <param name="handle">Windowハンドル</param>
        /// <param name="maximized">最大化されているかどうか</param>
        /// <param name="minimized">最小化されているかどうか</param>
        public static WindowInfo Create(IntPtr handle, string title, string className, int procId, Int32Rect rect, bool minimized, bool maximized)
        {
            WindowInfo info = new WindowInfo();
            info.Handle = handle;
            info.Title = title;
            info.ClassName = className;
            info.ProcessId = procId;
            info.Rect = rect;
            info.IsMinimized = minimized;
            info.IsMaximized = maximized;
            info.PrevRect = Int32Rect.Empty;

            info.ZOrder = 0;
            return info;
        }

        public void SetZOrder(int z) { ZOrder = z; }

        public override string ToString()
        {
            return string.Format("{0} [({1}){2}({3})]({4}, {5}, {6}x{7})", ZOrder, Handle, Title, ClassName, Rect.X, Rect.Y, Rect.Width, Rect.Height)
                + (IsMinimized ? "(min)" : "") + (IsMaximized ? "(max)" : "");
        }
    }

    /// <summary>Window情報を列挙するクラス</summary>
    class WinEnumerator : DependencyObject
    {
        /// <summary>壁部分</summary>
        private uint[] _wallPixels = null;

        /// <summary>呼び出しウィンドウのハンドル</summary>
        private IntPtr _mainWindowHandle = IntPtr.Zero;

        /// <summary>このアプリのプロセスID</summary>
        private int _mainProcessId = 0;

        /// <summary>現在のWindow一覧</summary>
        private List<WindowInfo> _windows = new List<WindowInfo>();
        /// <summary>最大化も最小化もされていない現在のWindow一覧</summary>
        private List<WindowInfo> _windowsNormal = new List<WindowInfo>();
        /// <summary>デスクトップウィンドウ</summary>
        private WindowInfo _windowDesktop;
        private string _windowStr = string.Empty;

        /// <summary>無視するウィンドウのクラス名</summary>
        private string[] _ignoreClassNames = new string[] {
            "ThumbnailClass",         // プレビュー(Vista)
            "TaskListOverlayWnd",     // プレビュー(7)
            "TaskListThumbnailWnd",   // プレビュー(7)
            "tooltips_class32",       // ツールチップ
            "SysShadow",              // ポップアップウィンドウの影
            "SideBar_HTMLHostWindow", // Windowsガジェット
        };

        private const string CLS_TASKBAR = "Shell_TrayWnd";

        private long _lastUpdatedTime = 0;
        /// <summary>最終更新tick</summary>
        public long LastUpdateTime { get { return _lastUpdatedTime; } }

        private bool _curWorking = false;
        /// <summary>現在更新中か否か</summary>
        public bool IsUpdating { get { return _curWorking; } }
        
        private Int32Rect _screenRect;

        /// <summary>コンストラクタ</summary>
        /// <param name="hWnd">現在のWindowハンドル</param>
        public WinEnumerator(IntPtr hWnd, Int32Rect screenRect, uint[] wallPixels)
        {
            _mainWindowHandle = hWnd;
            _mainProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            _windowDesktop = WindowInfo.Create(IntPtr.Zero, "デスクトップ", null, _mainProcessId, screenRect, false, true);

            _screenRect = screenRect;
            _wallPixels = wallPixels;

            _blurEffect = new BlurEffect { Radius = Simulation.Sph.WALL_RADIUS };
            _blurEffect.Freeze();
        }

        /// <summary>現在のWindow一覧を取得する</summary>
        public IEnumerable<WindowInfo> Windows
        {
            get
            {
                lock (_windows)
                {
                    foreach (WindowInfo info in _windows) yield return info;
                }
            }
        }

        /// <summary>最大化も最小化もされていない現在のWindow一覧</summary>
        public IEnumerable<WindowInfo> NormalWindows
        {
            get
            {
                lock (_windows)
                {
                    foreach (WindowInfo info in _windowsNormal) yield return info;
                }
            }
        }
        public string WindowString
        {
            get { return _windowStr; }
        }

        /// <summary>デスクトップウィンドウを取得する</summary>
        public WindowInfo DesktopWindow
        {
            get { return _windowDesktop; }
        }

        /// <summary>
        /// Window一覧を更新を開始する
        /// </summary>
        public void Update()
        {
            if (_curWorking) return;
            _curWorking = true;
            Task task = Task.Factory.StartNew(() =>
            {
                UpdateWindowsList();
            });
        }

        #region 更新中情報

        /// <summary>現在のWindow一覧(更新用)</summary>
        private List<WindowInfo> _wkgWindows = new List<WindowInfo>();
        /// <summary>最大化も最小化もされていない現在のWindow一覧(更新用)</summary>
        private List<WindowInfo> _wkgWindowsNormal = new List<WindowInfo>();
        private StringBuilder _wkgWinStr = new StringBuilder();
        private Dictionary<IntPtr, WindowInfo> _hndWinMap = new Dictionary<IntPtr, WindowInfo>();

        private RenderTargetBitmap _rBmp = null;
        private BlurEffect _blurEffect = null;

        /// <summary>最前面ウィンドウ</summary>
        private IntPtr _fgWinHnd;

        private static Brush _aroundWinBGBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        private static Brush _aroundWinFGBrush = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255));
        private static Brush _innerWinBGBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        private static Brush _innerWinFGBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 255));
        static WinEnumerator()
        {
            _aroundWinBGBrush.Freeze();
            _innerWinBGBrush.Freeze();
            _aroundWinFGBrush.Freeze();
            _innerWinFGBrush.Freeze();
        }

        /// <summary>Window一覧を更新する</summary>
        private void UpdateWindowsList()
        {
            /// デスクトップウィンドウを得る
            IntPtr desktopHandle = WinApi.GetDesktopWindow();
            _windowDesktop.Handle = desktopHandle;
            _windowDesktop.Rect = WinApi.GetWindowRect(desktopHandle);
            _fgWinHnd = WinApi.GetForegroundWindow();

            /// 古い情報をクリア
            _hndWinMap.Clear();
            foreach (var win in _wkgWindowsNormal) _hndWinMap.Add(win.Handle, win);
            _wkgWindows.Clear();
            _wkgWindowsNormal.Clear();
            _wkgWinStr.Clear();

            /// 更新
            WinApi.EnumWindows(new WinApi.EnumWindowsDelegate(EnumWindowsProc), IntPtr.Zero);
            SortByZOrder();

            foreach (WindowInfo wi in _wkgWindows)
            {
                _wkgWinStr.Append(wi + Environment.NewLine);
            }

            /// 壁重み画像更新
            UpdateWallWeight();

            /// 結果を格納
            lock (_windows)
            {
                _windows.Clear();
                _windows.AddRange(_wkgWindows);
                _windowsNormal.Clear();
                _windowsNormal.AddRange(_wkgWindowsNormal);
                _windowStr = _wkgWinStr.ToString();
                _lastUpdatedTime = Environment.TickCount;
                _curWorking = false;
            }
        }

        /// <summary>窓をZオーダーで並べなおす</summary>
        private void SortByZOrder()
        {
            int z = 0;
            List<WindowInfo> wis = new List<WindowInfo>(_wkgWindows);
            WindowInfo taskbarInfo = wis.FirstOrDefault(w => w.ClassName == CLS_TASKBAR);

            IntPtr fw = WinApi.GetWindow(_fgWinHnd, WinApi.GW.HWndFirst);

            int fi = wis.FindIndex(w => { return w.Handle == fw; });
            if (fi >= 0)
            {
                wis[fi].ZOrder = z;
                wis.RemoveAt(fi);
                z++;
            }

            IntPtr cw = fw;
            while (wis.Count > 0)
            {
                cw = WinApi.GetWindow(cw, WinApi.GW.HWndNext);
                if (cw == IntPtr.Zero) break;
                int ci = wis.FindIndex(w => { return w.Handle == cw; });
                if (ci >= 0)
                {
                    wis[ci].ZOrder = z;
                    wis.RemoveAt(ci);
                    z++;
                }
            }

            taskbarInfo.ZOrder = -1;

            _wkgWindows.Sort((wi1, wi2) => { return wi1.ZOrder.CompareTo(wi2.ZOrder); });
            _wkgWindowsNormal.Sort((wi1, wi2) => { return wi1.ZOrder.CompareTo(wi2.ZOrder); });
        }

        /// <summary>壁重み用の画像を作成する</summary>
        private void UpdateWallWeight()
        {
            // 窓がある部分の周囲をぼかして塗りつぶした画像を作成
            DrawingVisual blurVisual = new DrawingVisual();
            blurVisual.Effect = _blurEffect;
            using (DrawingContext dc = blurVisual.RenderOpen())
            {
                // 一旦塗りつぶし
                dc.DrawRectangle(_aroundWinFGBrush, null, new Rect(-_screenRect.Width / 2.0, -_screenRect.Height / 2.0, _screenRect.Width * 2, _screenRect.Height * 2));
                // スクリーンの境界部分のぼかしを作る
                dc.DrawRectangle(_aroundWinBGBrush, null, new Rect(_blurEffect.Radius / 2.0, _blurEffect.Radius / 2.0, _screenRect.Width - _blurEffect.Radius, _screenRect.Height - _blurEffect.Radius));
                // 各窓の領域を塗りつぶし
                foreach (WindowInfo info in _wkgWindows)
                {
                    if (info.IsMaximized) break;
                    if (info.IsMinimized) continue;
                    dc.DrawRectangle(_aroundWinFGBrush, null,
                        new Rect(info.Rect.X - _screenRect.X - _blurEffect.Radius / 2.0,
                            info.Rect.Y - _screenRect.Y - _blurEffect.Radius / 2.0,
                            info.Rect.Width + _blurEffect.Radius, info.Rect.Height + _blurEffect.Radius));
                }
            }
            blurVisual.Clip = new RectangleGeometry(new Rect(0, 0, _screenRect.Width, _screenRect.Height));

            // 窓内部を塗りつぶした画像を作成
            DrawingVisual noblurVisual = new DrawingVisual();
            using (DrawingContext dc = noblurVisual.RenderOpen())
            {
                dc.DrawRectangle(_innerWinBGBrush, null, new Rect(0, 0, _screenRect.Width, _screenRect.Height));
                foreach (WindowInfo info in _wkgWindows)
                {
                    if (info.IsMaximized) break;
                    if (info.IsMinimized) continue;
                    dc.DrawRectangle(_innerWinFGBrush, null, 
                        new Rect(info.Rect.X, info.Rect.Y, info.Rect.Width, info.Rect.Height));
                }
            }
            noblurVisual.Clip = new RectangleGeometry(new Rect(0, 0, _screenRect.Width, _screenRect.Height));

            // 2枚の画像を重畳
            DrawingVisual _wkgDrawingVisual = new DrawingVisual();
            VisualBrush vb1 = new VisualBrush(blurVisual);
            VisualBrush vb2 = new VisualBrush(noblurVisual);
            Effect.ColorMergeEffect ef = new Effect.ColorMergeEffect();
            ef.Key = (double)Effect.ColorMergeEffect.ColorKey.Green;
            ef.Input2 = vb2;
            _wkgDrawingVisual.Effect = ef;
            using (DrawingContext dc = _wkgDrawingVisual.RenderOpen())
            {
                dc.DrawRectangle(vb1, null, new Rect(0, 0, _screenRect.Width, _screenRect.Height));
            }

            // 画像から壁重みの配列に変換
            _rBmp = new RenderTargetBitmap((int)_screenRect.Width, (int)_screenRect.Height,
                Common.Constants.DPIX, Common.Constants.DPIY, PixelFormats.Pbgra32);
            _rBmp.Render(_wkgDrawingVisual);
            _rBmp.Freeze();
            _rBmp.CopyPixels(_wallPixels, (_rBmp.PixelWidth * _rBmp.Format.BitsPerPixel + 7) / 8, 0);

        }

        /// <summary>EnumWindows 関数から呼び出されるコールバック関数</summary>
        /// <param name="hWnd">トップレベルウィンドウのハンドル</param>
        /// <param name="lParam">EnumWindows 関数から渡されるアプリケーション定義の値</param>
        /// <returns>列挙を続行する場合は、0 以外の値(true)。列挙を中止する場合は、0(false)。</returns>
        private bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam)
        {
            if (!WinApi.IsVisible(hWnd)) return true;        // 見えてないWindowは省く
            if (hWnd == _windowDesktop.Handle) return true; // デスクトップWindowは省く
            if (hWnd == _mainWindowHandle) return true;     // このアプリ自体のWindowは省く

            String className = WinApi.GetWindowClassName(hWnd);
            if (_ignoreClassNames.Contains(className)) return true; // 除外クラス名のWindowは省く

            int procId = WinApi.GetProcessId(hWnd);
            if (_mainProcessId == procId) return true; // このアプリが作成したWindowは省く

            Int32Rect rect = WinApi.GetWindowRect(hWnd);
            if (rect.Width == 0 || rect.Height == 0) return true; // 大きさが0のWindowは省く

            bool maximized = WinApi.IsMaximized(hWnd);
            // 最大化状態でなくても、スクリーンと同じ大きさなら最大化として扱う
            if (!maximized)
            {
                if (rect.X <= _screenRect.X && rect.Y <= _screenRect.Y &&
                    rect.X + rect.Width >= _screenRect.X + _screenRect.Width &&
                    rect.Y + rect.Height >= _screenRect.Y + _screenRect.Height) maximized = true;
            }
            bool minimized = WinApi.IsMinimized(hWnd);

            WindowInfo info = WindowInfo.Create(hWnd, WinApi.GetWindowText(hWnd), className, procId, rect, minimized, maximized);
            if (_hndWinMap.ContainsKey(hWnd))
            {
                WindowInfo prev = _hndWinMap[hWnd];
                info.PrevRect = prev.Rect;
            }

            _wkgWindows.Add(info);
            if (!minimized && !maximized)
            {
                _wkgWindowsNormal.Add(info);
            }
            return true;
        }

        #endregion
    }

}

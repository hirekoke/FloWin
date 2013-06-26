using System;
using System.Collections.Generic;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;
using System.Threading;
using System.Windows.Interop;

using FloWin.Model;
using FloWin.Common;

namespace FloWin.View
{
    /// <summary>メインウィンドウ(全画面): ユーザ入力処理・描画担当</summary>
    public partial class MainWindow : Window
    {
        /// <summary>メインロジック</summary>
        private SimModel _model;
        public SimModel Model { get { return _model; } }

        private bool _isClosed = false;
        /// <summary>ウィンドウが閉じられたかどうか</summary>
        public bool IsClosed { get { return _isClosed; } }

        private Int32Rect _scrRect32 = Int32Rect.Empty;
        private Rect _scrRect = Rect.Empty;

        /// <summary>タスクトレイアイコン</summary>
        private NotifyIconManager _notifyIconManager;

        /// <summary>設定画面</summary>
        private View.ConfigWindow _configWin = null;

        /// <summary>壁を描画するかどうか</summary>
        private bool _drawWall = false;

        /// <summary>壁重み描画用画像</summary>
        private WriteableBitmap _wallBmp;

        private App _app;

        #region 文字描画用変数
        /// <summary>文字描画用 typeface</summary>
        private static Typeface _typeface = new Typeface(SystemFonts.MessageFontFamily, 
            SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Normal);

        private static Pen _txtStrokePen = new Pen(Brushes.White, 2);

        private double _prevFps = 0;
        private FormattedText _fpsText = null;
        private Geometry _fpsStroke = null;

        private FormattedText _winText = null;
        private long _winUpdateTime = 0;

        private long _prevCnt = 0;
        private FormattedText _cntText = null;
        #endregion

        #region 液体描画用変数
        /// <summary>液体描画用エフェクト</summary>
        private Effect.LiquidEffect _liquidEffect = null;
        /// <summary>液体描画用グラデーション</summary>
        private static RadialGradientBrush _gradBrush = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
            GradientStops = new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.0),
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0)
            }
        };
        private RenderTargetBitmap _liquidGradPoint;
        private double _liquidRadius = Simulation.Sph.H * 2;
        /// <summary>粒子描画用ブラシ</summary>
        private static SolidColorBrush _particleBrush;
        #endregion

        public MainWindow(SimModel model)
        {
            InitializeComponent();
            this.DataContext = this;

            _app = (App)Application.Current;
            _model = model;

            //// タイトル
            this.Title = Properties.Resources.Str_AppName;

            //// アイコン
            System.IO.MemoryStream memStream = new System.IO.MemoryStream();
            (Properties.Resources.Ico_App as System.Drawing.Icon).Save(memStream);
            this.Icon = BitmapFrame.Create(memStream);

            // タスクトレイアイコン表示
            _notifyIconManager = new NotifyIconManager(this);

            // input関係初期化
            InitInput();

            // Load後の処理
            this.Loaded += (s, e) =>
            {
                // スクリーンサイズ取得
                System.Drawing.Rectangle r = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                _scrRect32 = new Int32Rect(r.X, r.Y, r.Width, r.Height);
                _scrRect = new Rect(r.X, r.Y, r.Width, r.Height);

                // 最大化する
                // (AllosTransParency=True で Maximized だと何故か隙間ができるので手動で大きさ設定)
                this.Left = r.X; this.Top = r.Y;
                this.Width = r.Width; this.Height = r.Height;

                // pourSrcViewにPourSourceを結びつける
                pourSrcView.ViewModel.setPourSource(_model.PourSrc);

                //キャンバス初期化
                InitCanvas();

                HwndSource hwndSource = (HwndSource)HwndSource.FromVisual(this);
                _model.Start(_scrRect32, hwndSource.Handle);
            };

            // 閉じた後の処理
            this.Closing += (s, e) =>
            {
                _model.Stop();
                if (_configWin != null) _configWin.Close();
                _isClosed = true;
                if (_notifyIconManager != null) _notifyIconManager.Dispose();
            };
        }

        /// <summary>コマンド-注水状態のトグル</summary>
        public DelegateCommand TogglePourCommand { get; private set; }

        /// <summary>コマンド-注水開始</summary>
        public DelegateCommand StartPourCommand { get; private set; }

        /// <summary>コマンド-注水停止</summary>
        public DelegateCommand StopPourCommand { get; private set; }

        /// <summary>コマンド-停止状態のトグル</summary>
        public DelegateCommand TogglePauseCommand { get; private set; }

        /// <summary>コマンド-設定</summary>
        public DelegateCommand ConfigCommand { get; private set; }

        /// <summary>コマンド-終了</summary>
        public DelegateCommand ExitCommand { get; private set; }

        /// <summary>コマンド-障害物を描画する</summary>
        public DelegateCommand DrawWallCommand { get; private set; }
        /// <summary>コマンド-障害物を描画しない</summary>
        public DelegateCommand UnDrawWallCommand { get; private set; }

        /// <summary>ユーザ入力関係の初期化</summary>
        private void InitInput()
        {
            #region コマンド初期化
            TogglePourCommand = new DelegateCommand(
                Properties.Resources.Command_Pour,
                () =>
                {
                    if (_model.IsPouring) _model.StopPour();
                    else _model.StartPour();
                },
                () => _model.IsAvailable && _configWin == null);

            StartPourCommand = new DelegateCommand(Properties.Resources.Command_Pour,
                () => _model.StartPour(), () => !_model.IsPouring && _configWin == null);
            StopPourCommand = new DelegateCommand(Properties.Resources.Command_Pour,
                () => _model.StopPour(), () => _model.IsPouring && _configWin == null);

            DrawWallCommand = new DelegateCommand(
                Properties.Resources.Command_DrawWall,
                () => _drawWall = true, () => _model.IsAvailable && _configWin == null);
            UnDrawWallCommand = new DelegateCommand(
                Properties.Resources.Command_DrawWall,
                () => _drawWall = false, () => _model.IsAvailable && _configWin == null);
                
            TogglePauseCommand = new DelegateCommand(
                _model.IsPaused ? Properties.Resources.Command_Resume : Properties.Resources.Command_Pause,
                () =>
                {
                    _model.TogglePause();
                    TogglePauseCommand.Label = _model.IsPaused ?
                        Properties.Resources.Command_Resume : Properties.Resources.Command_Pause;
                },
                () => _model.IsAvailable && _configWin == null);

            ConfigCommand = new DelegateCommand(Properties.Resources.Command_Config,
                () =>
                {
                    if(_configWin == null) _configWin = new View.ConfigWindow();
                    bool configPause = false;
                    if (!_model.IsPaused) // 設定中は停止する
                    {
                        configPause = true;
                        _model.Pause();
                    }
                    _configWin.ShowDialog();
                    if (configPause) // 停止した場合は再開する
                    {
                        configPause = false;
                        _model.Resume();
                    }
                    _configWin = null;
                },
                () => _model.IsAvailable && _configWin == null);

            ExitCommand = new DelegateCommand(Properties.Resources.Command_Exit,
                () => Close(), () => _configWin == null);
            #endregion

            #region グローバルキー
            GlobalHook.KeyDown += (s, e) =>
            {
                if (Config.Instance.Pour.Hit(e.Key, e.Modifiers))
                {
                    if (StartPourCommand.CanExecute()) StartPourCommand.Execute();
                }
                else if (Config.Instance.DrawWall.Hit(e.Key, e.Modifiers))
                {
                    if (DrawWallCommand.CanExecute()) DrawWallCommand.Execute();
                }
                else if (Config.Instance.Pause.Hit(e.Key, e.Modifiers))
                {
                    if (TogglePauseCommand.CanExecute()) TogglePauseCommand.Execute();
                }
                else if (Config.Instance.Exit.Hit(e.Key, e.Modifiers))
                {
                    if (ExitCommand.CanExecute()) ExitCommand.Execute();
                }
                
            };

            GlobalHook.KeyUp += (s, e) =>
            {
                if (Config.Instance.Pour.Hit(e.Key, e.Modifiers))
                {
                    if (StopPourCommand.CanExecute()) StopPourCommand.Execute();
                }
                else if (Config.Instance.DrawWall.Hit(e.Key, e.Modifiers))
                {
                    if (UnDrawWallCommand.CanExecute()) UnDrawWallCommand.Execute();
                }
            };
            #endregion

            // 通知アイコンのメニュー
            _notifyIconManager.AddMenuItem(TogglePauseCommand);
            _notifyIconManager.AddMenuItem(ConfigCommand);
            _notifyIconManager.AddMenuItem(ExitCommand);
        }

        private void InitCanvas()
        {
            _wallBmp = new WriteableBitmap(_scrRect32.Width, _scrRect32.Height,
                Constants.DPIX, Constants.DPIY, PixelFormats.Pbgra32, null);
            
            simDrawing.ClipGeometry = new RectangleGeometry(_scrRect);

            _liquidEffect = new Effect.LiquidEffect();
            _liquidEffect.Threashold = 0.8;
            _liquidEffect.FillColor = Config.Instance.LiquidColor;
            _liquidEffect.Freeze();

            _particleBrush = new SolidColorBrush(Config.Instance.LiquidColor);

            // 粒子をぼかして描画するための画像作成
            _liquidGradPoint = new RenderTargetBitmap((int)_liquidRadius * 2, (int)_liquidRadius * 2, 
                Constants.DPIX, Constants.DPIY, PixelFormats.Pbgra32);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, _liquidGradPoint.PixelWidth, _liquidGradPoint.PixelHeight));
                dc.DrawEllipse(_gradBrush, null, new Point(_liquidRadius, _liquidRadius), _liquidRadius, _liquidRadius);
            }
            _liquidGradPoint.Render(dv);
            _liquidGradPoint.Freeze();
        }

        #region 描画

        /// <summary>描画処理</summary>
        public void Draw()
        {
            if (!_model.IsAvailable) return;

            #region 設定値の反映
            if (_liquidEffect.FillColor != Config.Instance.LiquidColor)
            {
                _liquidEffect = new Effect.LiquidEffect();
                _liquidEffect.Threashold = 0.8;
                _liquidEffect.FillColor = Config.Instance.LiquidColor;
                _liquidEffect.Freeze();
            }

            if (_particleBrush.Color != Config.Instance.LiquidColor)
                _particleBrush.Color = Config.Instance.LiquidColor;
            #endregion

            drawBackground();

            // 粒子描画
            if (Config.Instance.DrawLiquid)
                drawLiquid();
            else
                drawParticles();

            drawForeground();
        }

        /// <summary>背景を描画する</summary>
        private void drawBackground()
        {
            if (_drawWall)
            { // 障害物描画
                _wallBmp.WritePixels(_scrRect32, _model.WallPixels, _scrRect32.Width * _wallBmp.Format.BitsPerPixel / 8, 0);
                using (DrawingContext dc = backDrawing.Open())
                {
                    dc.DrawImage(_wallBmp, _scrRect);
                }
            }
            else
            { // 何も描画しない
                using (DrawingContext dc = backDrawing.Open())
                {
                    dc.DrawRectangle(Brushes.Transparent, null, _scrRect);
                }
            }
        }

        /// <summary>前景を描画する</summary>
        private void drawForeground()
        {
            bool update = false;
            // fps
            if (_fpsText == null || _app.FrameRate != _prevFps)
            {
                _prevFps = _app.FrameRate;
                _fpsText = new FormattedText(
                    "fps: " + _app.FrameRate.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ?
                        FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                    _typeface, SystemFonts.MessageFontSize, SystemColors.WindowFrameBrush);
                _fpsStroke = _fpsText.BuildGeometry(Config.Instance.FpsLocation);
                _fpsStroke.Freeze();
                update = true;
            }

            if (_cntText == null || _prevCnt != _model.Sph.ParticleCount)
            {
                _prevCnt = _model.Sph.ParticleCount;
                _cntText = new FormattedText(
                    "count: " + _model.Sph.ParticleCount.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ?
                        FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                    _typeface, SystemFonts.MessageFontSize, SystemColors.WindowFrameBrush);
                update = true;
            }

            // 窓一覧
            if (_winUpdateTime < _model.WinEnum.LastUpdateTime)
            {
                _winUpdateTime = _model.WinEnum.LastUpdateTime;
                _winText = new FormattedText(
                    _model.WinEnum.WindowString,
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ?
                        FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                    _typeface, SystemFonts.MessageFontSize, SystemColors.WindowFrameBrush);
                update = true;
            }

            if (!update) return;
            using (DrawingContext dc = foreDrawing.Open())
            {
                // 透明で塗りつぶし
                dc.DrawRectangle(Brushes.Transparent, null, _scrRect);

                //_pourSrc.Draw(dc);
                // FPS
                dc.DrawGeometry(Brushes.White, _txtStrokePen, _fpsStroke);
                dc.DrawText(_fpsText, Config.Instance.FpsLocation);

                dc.DrawText(_cntText, Config.Instance.ParticleCountLocation);

                // 窓一覧
                dc.DrawText(_winText, Config.Instance.WinEnumLocation);
            }
        }

        /// <summary>液体描画</summary>
        private void drawLiquid()
        {
            if (simImage.Effect != _liquidEffect) simImage.Effect = _liquidEffect;
            Rect prect = new Rect(0, 0, _liquidRadius * 2, _liquidRadius * 2);
            using (DrawingContext dc = simDrawing.Open())
            {
                // 透明で塗りつぶし
                dc.DrawRectangle(Brushes.White, null, _scrRect);

                dc.PushTransform(new TranslateTransform(-_liquidRadius, -_liquidRadius));
                // 粒子描画
                foreach (Point p in _model.Sph.ParticleLocations)
                {
                    prect.X = p.X; prect.Y = p.Y;
                    dc.DrawImage(_liquidGradPoint, prect);
                }

                dc.Pop();
            }
        }

        /// <summary>粒子描画</summary>
        private void drawParticles()
        {
            if (simImage.Effect != null) simImage.Effect = null;
            using (DrawingContext dc = simDrawing.Open())
            {
                // 透明で塗りつぶし
                dc.DrawRectangle(Brushes.Transparent, null, _scrRect);

                // 粒子描画
                foreach (Point p in _model.Sph.ParticleLocations)
                    dc.DrawEllipse(_particleBrush, null, p, _liquidRadius, _liquidRadius);
            }
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace FloWin.Model
{
    /// <summary>シミュレーション本体</summary>
    public class SimModel
    {
        /// <summary>表示ウィンドウの列挙クラス</summary>
        private WinEnumerator _winEnum;
        internal WinEnumerator WinEnum { get { return _winEnum; } }

        /// <summary>シミュレータ</summary>
        private Simulation.Sph _sim;
        internal Simulation.Sph Sph { get { return _sim; } }

        /// <summary>壁重み</summary>
        private uint[] _wallPixels = null;
        public uint[] WallPixels { get { return _wallPixels; } }

        /// <summary>シミュレーション領域</summary>
        private Int32Rect _screenRect = Int32Rect.Empty;

        /// <summary>開始済みか否か</summary>
        private bool _isAvailable = false;
        public bool IsAvailable { get { return _isAvailable; } }

        /// <summary>ポーズ中か否か</summary>
#if DEBUG
        private bool _isPaused = true;
#else
        private bool _isPaused = false;
#endif
        public bool IsPaused { get { return _isPaused; } }

        private PourSource _pourSrc = new PourSource();
        public PourSource PourSrc { get { return _pourSrc; } }

        /// <summary>注ぎ中か否か</summary>
        private bool _isPouring = false;
        public bool IsPouring { get { return _isPouring; } }

        public SimModel()
        {
        }

        public void Start(Int32Rect simRect, IntPtr hWnd)
        {
            _screenRect = simRect;
            _wallPixels = new uint[_screenRect.Width * _screenRect.Height];

            // Window列挙
            _winEnum = new WinEnumerator(hWnd, _screenRect, _wallPixels);

            // SPH初期化
            _sim = new Simulation.Sph(_wallPixels, _screenRect);

            _isAvailable = true;
        }

        public void Stop()
        {
            _isAvailable = false;
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        public void StartPour() { _isPouring = true; }
        public void StopPour() { _isPouring = false; }

        /// <summary>更新処理</summary>
        public void Update()
        {
            if (!_isAvailable) return;

            // 壁重み更新
            long tick = Environment.TickCount;
            if (!_winEnum.IsUpdating &&
                tick > _winEnum.LastUpdateTime + Config.Instance.WinUpdateMSec)
            {
                _winEnum.Update();
            }

            if (!_isPaused)
            {
                // 注ぎ
                if (_isPouring)
                    _sim.AddParticles(
                        _pourSrc.Bounds.X + _pourSrc.SrcPoint1.X,
                        _pourSrc.Bounds.Y + _pourSrc.SrcPoint1.Y,
                        _pourSrc.Bounds.X + _pourSrc.SrcPoint2.X,
                        _pourSrc.Bounds.Y + _pourSrc.SrcPoint2.Y);

                // SPH更新
                _sim.Step();
            }
        }
    }
}

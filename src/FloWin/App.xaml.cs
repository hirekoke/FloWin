using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;

using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;

namespace FloWin
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        #region frame rate 関係の変数
        private long _nextTick;
        private long _lastCountTick;
        private long _lastFpsTick;

        /// <summary>フレームレート計算用のフレーム数</summary>
        private long _currentTick;

        /// <summary>フレームレート計算用のフレーム数</summary>
        private int _frameCount = 0;

        /// <summary>現在のフレームレート</summary>
        private double _frameRate;
        public double FrameRate { get { return _frameRate; } }

        /// <summary>理想フレームレート</summary>
        private const double _idealFrameRate = 15;
        #endregion

        /// <summary>二重起動禁止用mutex</summary>
        private static System.Threading.Mutex _mutex;

        /// <summary>メインウィンドウ</summary>
        private static View.MainWindow _win;

        /// <summary>アプリケーション初期化処理</summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 二重起動禁止
            _mutex = new System.Threading.Mutex(false, Application.ResourceAssembly.FullName);
            if (!_mutex.WaitOne(0, false))
            {
                _mutex.Close();
                _mutex = null;
                this.Shutdown();
            }

            // 開始
            Start();

            // 終了
            Shutdown();
        }

        /// <summary>終了処理</summary>
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Close();
            }
        }

        /// <summary>メイン処理を開始する</summary>
        private void Start()
        {
            // 設定読み込み
            Model.Config.Instance.Load();

            // フック開始
            GlobalHook.Hook();

            // モデル
            Model.SimModel _model = new Model.SimModel();

            // 窓の表示
            _win = new View.MainWindow(_model);
            _win.Show();

            // メインループ
            while (!_win.IsClosed)
            {
                _currentTick = Environment.TickCount;
                double diffms = Math.Floor(1000.0 / Model.Config.Instance.FrameRate);

                if (_currentTick < _nextTick)
                {
                    // 待ち
                }
                else
                {
                    // 処理
                    _model.Update();

                    if (Environment.TickCount >= _nextTick + diffms)
                    {
                        // フレームスキップ
                    }
                    else
                    {
                        // 描画
                        _win.Draw();
                    }

                    _frameCount++;
                    _lastCountTick = _currentTick;
                    while (_currentTick >= _nextTick)
                    {
                        _nextTick += (long)diffms;
                    }
                }

                // frame rate 計算
                if (_currentTick - _lastFpsTick >= 1000)
                {
                    _frameRate = _frameCount * 1000 / (double)(_currentTick - _lastFpsTick);
                    _frameCount = 0;
                    _lastFpsTick = _currentTick;
                }

                // UIメッセージ処理
                DoEvents();
            }

            // フック終了
            GlobalHook.Unhook();

            // 設定保存
            Model.Config.Instance.Save();
        }

        /// <summary>現在メッセージ待ち行列の中にある全UIメッセージを処理する</summary>
        private void DoEvents()
        {
            // 新しくネスト化されたメッセージ ポンプを作成
            DispatcherFrame frame = new DispatcherFrame();

            // DispatcherFrame (= 実行ループ) を終了させるコールバック
            DispatcherOperationCallback exitFrameCallback = (f) =>
            {
                // ネスト化されたメッセージ ループを抜ける
                ((DispatcherFrame)f).Continue = false;
                return null;
            };

            // 非同期で実行する
            // 優先度を Background にしているので、このコールバックは
            // ほかに処理するメッセージがなくなったら実行される
            DispatcherOperation exitOperation = Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background, exitFrameCallback, frame);

            // 実行ループを開始する
            Dispatcher.PushFrame(frame);

            // コールバックが終了していない場合は中断
            if (exitOperation.Status != DispatcherOperationStatus.Completed)
            {
                exitOperation.Abort();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using FloWin.Common;

namespace FloWin.View
{
    /// <summary>タスクトレイアイコンを管理するクラス</summary>
    class NotifyIconManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;

        public NotifyIconManager(MainWindow mainWindow)
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = Properties.Resources.Str_AppName;
            _notifyIcon.Icon = Properties.Resources.Ico_App;

            // コンテキストメニューを作成
            _contextMenu = new ContextMenuStrip();

            // メニューを開くタイミングで選択可否を設定
            _contextMenu.Opened += (s, e) =>
            {
                for (int i = 0; i < _contextMenu.Items.Count; i++)
                {
                    var item = _contextMenu.Items[i];
                    var command = (DelegateCommand)item.Tag;
                    item.Enabled = command.CanExecute();
                }
            };

            _notifyIcon.ContextMenuStrip = _contextMenu;
            // アイコンを表示する
            _notifyIcon.Visible = true;
        }

        public void AddMenuItem(DelegateCommand command)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(command.Label);
            item.Tag = command;
            item.Click += (s, e) =>
            {
                if (command.CanExecute()) command.Execute();
            };
            command.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case "Label":
                        item.Text = command.Label;
                        break;
                    default:
                        break;
                }
            };
            _contextMenu.Items.Add(item);
        }

        /// <summary>終了処理</summary>
        public void Dispose()
        {
            // アイコンを消す
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}

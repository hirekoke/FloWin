using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace FloWin
{
    class Win32Api
    {
        #region Window情報取得
        /// <summary>SendMessageの引数, ウィンドウタイトルを取得する</summary>
        public const uint WM_GETTEXT = 0x000D;
        /// <summary>SendMessageの引数, ウィンドウタイトルの文字列長を取得する</summary>
        public const uint WM_GETTEXTLENGTH = 0x000E;

        /// <summary>矩形の構造体
        /// typedef struct _RECT
        /// {
        ///     LONG left;
        ///     LONG top;
        ///     LONG right;
        ///     LONG bottom;
        /// } RECT, *PRECT;
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        
        /// <summary>
        /// 指定ウィンドウと同じ種類で最も高い Z オーダーを持つウィンドウのハンドルを取得する。
        /// 指定ウィンドウが最前面ウィンドウの場合は、最も高い Z オーダーを持つ最前面ウィンドウのハンドルが返る。
        /// 指定ウィンドウがトップレベルウィンドウの場合は、最も高い Z オーダーを持つトップレベルウィンドウのハンドルが返る。
        /// 指定ウィンドウが子ウィンドウの場合は、最も高い Z オーダーを持つ兄弟ウィンドウのハンドルが返る。
        /// </summary>
        public const int GW_HWNDFIRST = 0;
        /// <summary>
        /// 指定したウィンドウと同じ種類で最も低い Z オーダーを持つウィンドウのハンドルを取得する。
        /// 指定したウィンドウが最前面ウィンドウの場合は、最も低い Z オーダーを持つ最前面ウィンドウのハンドルが返る。
        /// 指定したウィンドウがトップレベルウィンドウの場合は、最も低い Z オーダーを持つトップレベルウィンドウのハンドルが返る。
        /// 指定したウィンドウが子ウィンドウの場合は、最も低い Z オーダーを持つ兄弟ウィンドウのハンドルが返る。
        /// </summary>
        public const int GW_HWNDLAST = 1;
        /// <summary>
        /// 指定したウィンドウより Z オーダーが 1 つ下のウィンドウのハンドルを取得する。
        /// 指定したウィンドウが最前面ウィンドウの場合は、1 つ下の最前面ウィンドウのハンドルが返る。
        /// 指定したウィンドウがトップレベルウィンドウの場合は、1 つ下のトップレベルウィンドウのハンドルが返る。
        /// 指定したウィンドウが子ウィンドウの場合は、1 つ下の兄弟ウィンドウのハンドルが返る。
        /// </summary>
        public const int GW_HWNDNEXT = 2;
        /// <summary>
        /// 指定したウィンドウより Z オーダーが 1 つ上のウィンドウのハンドルを取得する。
        /// 指定したウィンドウが最前面ウィンドウの場合は、1 つ上の最前面ウィンドウのハンドルが返る。
        /// 指定したウィンドウがトップレベルウィンドウの場合は、1 つ上のトップレベルウィンドウのハンドルが返る。
        /// 指定したウィンドウが子ウィンドウの場合は、1 つ上の兄弟ウィンドウのハンドルが返る。
        /// </summary>
        public const int GW_HWNDPREV = 3;
        /// <summary>
        /// 指定したウィンドウのオーナーウィンドウのハンドルを取得する。
        /// </summary>
        public const int GW_OWNER = 4;
        /// <summary>
        /// 指定したウィンドウが親ウィンドウの場合は、Z オーダーが一番上の子ウィンドウのハンドルを取得する。
        /// それ以外の場合はNULL。
        /// 指定されたウィンドウの子ウィンドウだけを調べ、それより下位の子孫は調べない。
        /// </summary>
        public const int GW_CHILD = 5;

        /// <summary>EnumWindows 関数から呼び出されるコールバック関数</summary>
        /// <param name="hWnd">トップレベルウィンドウのハンドル</param>
        /// <param name="lParam">EnumWindows 関数から渡されるアプリケーション定義の値</param>
        /// <returns>列挙を続行する場合は、0 以外の値。列挙を中止する場合は、0。</returns>
        public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        /// <summary>画面上のすべてのトップレベルウィンドウを列挙する</summary>
        /// <remarks>この関数を呼び出すと、各ウィンドウのハンドルが順々にアプリケーション定義のコールバック関数に渡される。EnumWindows 関数は、すべてのトップレベルリンドウを列挙し終えるか、またはアプリケーション定義のコールバック関数から 0（FALSE）が返されるまで処理を続ける。</remarks>
        /// <param name="lpEnumFunc">アプリケーション定義のコールバック関数へのポインタ</param>
        /// <param name="lParam">コールバック関数に渡すアプリケーション定義の値</param>
        /// <returns>成功したら0以外、失敗すると0。EnumWindowsProc 関数が 0 を返すと、戻り値は 0 になる。</returns>
        [DllImport("user32.dll")]
        public static extern int EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lParam);

        /// <summary>デスクトップウィンドウのハンドルを取得する</summary>
        /// <returns>デスクトップウィンドウのハンドル</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        /// <summary>フォアグラウンドウィンドウのハンドルを取得する</summary>
        /// <returns>フォアグラウンドウィンドウのハンドル</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>指定されたウィンドウと指定された関係(またはオーナー)にあるウィンドウのハンドルを返す</summary>
        /// <param name="hWnd">元ウィンドウのハンドル。このウィンドウを元に、uCmd パラメータの値に基づいてウィンドウが検索される。</param>
        /// <param name="uCmd">指定したウィンドウとハンドルを取得するウィンドウとの関係を指定</param>
        /// <returns>ウィンドウのハンドル。指定した関係を持つウィンドウがない場合はNULL</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        /// <summary>指定されたウィンドウの表示状態を取得する</summary>
        /// <param name="hWnd">調査するウィンドウのハンドル</param>
        /// <returns>指定されたウィンドウ、その親ウィンドウ、そのさらに上位の親ウィンドウのすべてが WS_VISIBLE スタイルを持つ場合は、0 以外の値。それ以外の場合は0。戻り値が示すのは WS_VISIBLE スタイルを持つか持たないかという情報であるため、ウィンドウがその他のウィンドウに完全に隠されていて画面に表示されていなくても 0 以外の値が返る場合がある。</returns>
        [DllImport("user32.dll")]
        public static extern int IsWindowVisible(IntPtr hWnd);

        /// <summary>指定されたウィンドウが最大化されているかどうかを調べる</summary>
        /// <param name="hWnd">調査するウィンドウのハンドル</param>
        /// <returns>最大化されている場合は0以外、最大化されていない場合は0</returns>
        [DllImport("user32.dll")]
        public static extern int IsZoomed(IntPtr hWnd);

        /// <summary>指定されたウィンドウが最小化(アイコン化)されているかどうかを調べる</summary>
        /// <param name="hWnd">調査するウィンドウのハンドル</param>
        /// <returns>最小化されている場合は0以外、最小化されていない場合は0</returns>
        [DllImport("user32.dll")]
        public static extern int IsIconic(IntPtr hWnd);

        /// <summary>指定されたウィンドウを作成したスレッドの ID を取得する</summary>
        /// <remarks>ウィンドウを作成したプロセスの ID も取得できる</remarks>
        /// <param name="hWnd">ウィンドウのハンドル</param>
        /// <param name="lpdwProcessId">プロセス ID を受け取る変数へのポインタ。ポインタを指定すると、それが指す変数にプロセス ID がコピーされる。NULL を指定した場合は、プロセス ID の取得は行われない。</param>
        /// <returns>ウィンドウを作成したスレッドの ID</returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);

        /// <summary>ウィンドウの位置、大きさを取得する</summary>
        /// <param name="hWnd">ウィンドウのハンドル</param>
        /// <param name="rect">ウィンドウの座標値</param>
        /// <returns>成功したら0以外、失敗すると0</returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowRect(IntPtr hWnd, out RECT rect);

        /// <summary>
        /// ウィンドウのクラス名を取得する
        /// </summary>
        /// <param name="hWnd">ウィンドウのハンドル</param>
        /// <param name="lpClassName">クラス名を格納するバッファ</param>
        /// <param name="nMaxCount">バッファの最大長</param>
        /// <returns>バッファにコピーされた文字列長、失敗すると0</returns>
        [DllImport("user32.dll")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        /// <summary>
        /// ウィンドウハンドルからプロセスIDを取得する
        /// </summary>
        /// <param name="hWnd">ウィンドウのハンドル</param>
        /// <param name="lpdwProcessId">ウィンドウを作成したプロセスID</param>
        /// <returns>ウィンドウを作成したスレッドID</returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        #endregion


        /// <summary>1つまたは複数のウィンドウへ指定されたメッセージを送信する</summary>
        /// <remarks>指定されたウィンドウのウィンドウプロシージャを呼び出し、そのウィンドウプロシージャがメッセージを処理し終わった後で、制御を返す。</remarks>
        /// <param name="hWnd">1 つのウィンドウのハンドル。このウィンドウのウィンドウプロシージャがメッセージを受信する。HWND_BROADCAST を指定すると、この関数は、システム内のすべてのトップレベルウィンドウ（親を持たないウィンドウ）へメッセージを送信する。</param>
        /// <param name="Msg">送信するべきメッセージ</param>
        /// <param name="wParam">メッセージ特有の追加情報</param>
        /// <param name="lParam">メッセージ特有の追加情報</param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}

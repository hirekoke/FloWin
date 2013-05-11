using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace FloWin.Simulation
{
    /// <summary>
    /// モートン番号を用いた四分木分割実装
    /// ref. http://marupeke296.com/COL_2D_No8_QuadTree.html
    /// </summary>
    /// <typeparam name="T">登録・検索するオブジェクトの型</typeparam>
    internal class QuadTree<T>
    {
        /// <summary>レベル深さの最大(実際のレベルの大きさはMAXLEVEL-1まで)</summary>
        public const int MAXLEVEL = 6;

        /// <summary>各レベルでの空間数</summary>
        private static int[] _levelNum = null;

        /// <summary>モートン数を求めるためのビットマスク</summary>
        private static long[] _bitPattern = null;

        private Rect _bounds;
        private double _unitW;
        private double _unitH;
        private double _unitWInv;
        private double _unitHInv;

        private Cell[] _cells;
        private Dictionary<T, Obj> _objMap;

        private Func<T, T, bool> _collisionTest;

        static QuadTree()
        {
            _levelNum = new int[MAXLEVEL+1];
            _levelNum[0] = 1; // root
            for (int i = 1; i < _levelNum.Length; i++)
                _levelNum[i] = _levelNum[i - 1] * 4;

            long[] t = new long[MAXLEVEL];
            t[0] = 1;
            for (int i = 1; i < t.Length; i++)
                t[i] = t[i - 1] * t[i - 1] + 2 * t[i - 1];

            _bitPattern = new long[MAXLEVEL-2];
            int mc = MAXLEVEL * 2; // 最大ビット数
            for (int i = 0; i < _bitPattern.Length; i++)
            {
                long a = t[i];
                long b = a;
                for (int j = 1; j < (mc >> (i + 1)); j++)
                {
                    b = b + (a << ((2 << i) * j));
                }
                _bitPattern[i] = b;
            }
        }

        public QuadTree(double left, double top, double right, double bottom, Func<T, T, bool> collisionTest)
        {
            _bounds = new Rect(left, top, right - left, bottom - top);
            _unitW = _bounds.Width / (1 << (MAXLEVEL - 1));
            _unitH = _bounds.Height / (1 << (MAXLEVEL - 1));
            _unitWInv = 1.0 / _unitW;
            _unitHInv = 1.0 / _unitH;

            _cells = new Cell[(_levelNum[MAXLEVEL] - 1) / 3];
            _objMap = new Dictionary<T, Obj>();
            _collisionTest = collisionTest;
        }

        /// <summary>
        /// オブジェクトを登録する
        /// </summary>
        /// <param name="item">登録するオブジェクト</param>
        /// <param name="left">オブジェクト左端</param>
        /// <param name="top">オブジェクト上端</param>
        /// <param name="right">オブジェクト右端</param>
        /// <param name="bottom">オブジェクト下端</param>
        /// <returns>登録に成功したらtrue</returns>
        public bool Regist(T item, double left, double top, double right, double bottom)
        {
            long cellIdx = GetCellArrayIdx(left, top, right, bottom);

            // セルの作成
            if (_cells[cellIdx] == null) CreateCell(cellIdx);

            // 登録オブジェクトを用意
            Obj obj;
            if (_objMap.ContainsKey(item)) obj = _objMap[item];
            else
            {
                obj = new Obj(item);
                _objMap.Add(item, obj);
            }

            // セルにオブジェクトを登録
            if (obj.cell != _cells[cellIdx])
            {
                _cells[cellIdx].Add(obj);
                return true;
            }
            return false;
        }

        /// <summary>
        /// オブジェクトを削除する
        /// </summary>
        /// <param name="item">削除するオブジェクト</param>
        /// <returns>削除に成功したらtrue</returns>
        public bool Remove(T item)
        {
            if (_objMap.ContainsKey(item))
            {
                Obj obj = _objMap[item];
                obj.cell.Remove(obj);
                return true;
            }
            return false;
        }

        /// <summary>衝突したオブジェクトのペアのリストを取得する</summary>
        public IEnumerable<Pair> GetCollisionList()
        {
            /*List<Tuple<T, T>> collisionList = new List<Tuple<T, T>>();
            Stack<Obj> stack = new Stack<Obj>();
            GetCollisionListIter(0, 0, ref stack, ref collisionList);
            return collisionList;*/
            return GetCollisionListLoop();
        }

        public T[,] GetCollisionArray()
        {
            int curLevel = 0; long mortonNum = 0;
            int sibIdx = 0;
            Stack<Obj> stack = new Stack<Obj>();
            bool[] visited = new bool[_cells.Length];

            T[,] ary = new T[1000, 2];
            int aryIdx = 0;

            while (curLevel != 0 || mortonNum != 0 || !visited[0])
            {
                long idx = GetCellIdx(curLevel, mortonNum);
                if (curLevel >= MAXLEVEL)
                {
                    curLevel--;
                    mortonNum = mortonNum >> 2;
                    sibIdx = (int)(mortonNum & 3);
                    if (curLevel < 0) break;
                    else continue;
                }

                if (_cells[idx] != null)
                {
                    if (!visited[idx])
                    {
                        visited[idx] = true;

                        // 空間内のオブジェクトどうしをチェック
                        if (_cells[idx].first != null)
                        {
                            for (var oNode = _cells[idx].first; oNode != null; oNode = oNode.next)
                            {
                                if (oNode != null)
                                {
                                    for (var pNode = oNode.next; pNode != null; pNode = pNode.next)
                                    {
                                        var o = oNode.Value; var p = pNode.Value;
                                        if (_collisionTest(o, p))
                                        {
                                            int l = ary.GetLength(0);
                                            if (aryIdx >= l)
                                            {
                                                T[,] tmp = new T[l * 2, 2];
                                                Array.Copy(ary, tmp, l);
                                                ary = tmp;
                                            }
                                            ary[aryIdx, 0] = o;
                                            ary[aryIdx, 1] = p;
                                            aryIdx++;
                                        }
                                    }
                                }
                                // 空間内のオブジェクトとstack内のオブジェクトをチェック
                                foreach (var pNode in stack)
                                {
                                    var o = oNode.Value; var p = pNode.Value;
                                    if (_collisionTest(o, p))
                                    {
                                        int l = ary.GetLength(0);
                                        if (aryIdx >= l)
                                        {
                                            T[,] tmp = new T[l * 2, 2];
                                            Array.Copy(ary, tmp, l);
                                            ary = tmp;
                                        }
                                        ary[aryIdx, 0] = o;
                                        ary[aryIdx, 1] = p;
                                        aryIdx++;
                                    }
                                }
                            }
                        }

                        // 1段下に移動
                        if (curLevel < MAXLEVEL)
                        {
                            // stackに積む
                            if (_cells[idx].first != null)
                            {
                                for (var oNode = _cells[idx].first; oNode != null; oNode = oNode.next)
                                    stack.Push(oNode);
                            }

                            curLevel++;
                            mortonNum = mortonNum << 2;
                            sibIdx = 0;
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // stackから外す
                        if (_cells[idx].first != null)
                        {
                            for (var oNode = _cells[idx].first; oNode != null; oNode = oNode.next)
                                stack.Pop();
                        }
                    }
                }

                if (sibIdx == 3)
                {
                    // 1段上に移動
                    curLevel--;
                    mortonNum = mortonNum >> 2;
                    sibIdx = (int)(mortonNum & 3);
                    if (curLevel < 0) break;
                }
                else
                {
                    // 1子次に移動
                    mortonNum++;
                    sibIdx++;
                }
            }

            return ary;
        }

        /// <summary>再帰を使わずに衝突したオブジェクトのペアのリストを取得する</summary>
        private IEnumerable<Pair> GetCollisionListLoop()
        {
            int curLevel = 0; long mortonNum = 0;
            int sibIdx = 0;
            Stack<Obj> stack = new Stack<Obj>();
            bool[] visited = new bool[_cells.Length];
            //for (int i = 0; i < visited.Length; i++) visited[i] = false;

            while (curLevel != 0 || mortonNum != 0 || !visited[0])
            {
                long idx = GetCellIdx(curLevel, mortonNum);
                if (curLevel >= MAXLEVEL)
                {
                    curLevel--;
                    mortonNum = mortonNum >> 2;
                    sibIdx = (int)(mortonNum & 3);
                    if (curLevel < 0) yield break;
                    else continue;
                }

                if (_cells[idx] != null)
                {
                    if (!visited[idx])
                    {
                        visited[idx] = true;
                        
                        // 空間内のオブジェクトどうしをチェック
                        if (_cells[idx].first != null)
                        {
                            for (var oNode = _cells[idx].first; oNode != null; oNode = oNode.next)
                            {
                                if (oNode != null)
                                {
                                    for (var pNode = oNode.next; pNode != null; pNode = pNode.next)
                                    {
                                        var o = oNode.Value; var p = pNode.Value;
                                        if (_collisionTest(o, p)) yield return (new Pair(o, p));
                                    }
                                }
                                // 空間内のオブジェクトとstack内のオブジェクトをチェック
                                foreach (var pNode in stack)
                                {
                                    var o = oNode.Value; var p = pNode.Value;
                                    if (_collisionTest(o, p)) yield return (new Pair(o, p));
                                }
                            }
                        }

                        // 1段下に移動
                        if (curLevel < MAXLEVEL)
                        {
                            // stackに積む
                            if (_cells[idx].first != null)
                            {
                                for (var oNode = _cells[idx].first; oNode != null; oNode = oNode.next)
                                    stack.Push(oNode);
                            }

                            curLevel++;
                            mortonNum = mortonNum << 2;
                            sibIdx = 0;
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // stackから外す
                        if (_cells[idx].first != null)
                        {
                            for (var oNode = _cells[idx].first; oNode != null; oNode = oNode.next)
                                stack.Pop();
                        }
                    }
                }

                if (sibIdx == 3)
                {
                    // 1段上に移動
                    curLevel--;
                    mortonNum = mortonNum >> 2;
                    sibIdx = (int)(mortonNum & 3);
                    if (curLevel < 0) yield break;
                }
                else
                {
                    // 1子次に移動
                    mortonNum++;
                    sibIdx++;
                }
            }
        }

        /// <summary>再帰的に衝突したオブジェクトペアのリストを取得する</summary>
        /// <param name="curLevel">現在のレベル</param>
        /// <param name="curMorton">現在のモートン番号</param>
        /// <param name="stack">途中状態を保存するスタック</param>
        /// <param name="result">結果を格納するリスト</param>
        private void GetCollisionListIter(int curLevel, long curMorton, ref Stack<Obj> stack, ref List<Tuple<T, T>> result)
        {
            long i = GetCellIdx(curLevel, curMorton);
            if(_cells[i] == null) return;

            // 空間内のオブジェクトどうしをチェック
            if (_cells[i].first != null)
            {
                for (var oNode = _cells[i].first; oNode != null; oNode = oNode.next)
                {
                    if (oNode != null)
                    {
                        for (var pNode = oNode.next; pNode != null; pNode = pNode.next)
                        {
                            var o = oNode.Value; var p = pNode.Value;
                            if (_collisionTest(o, p)) result.Add(new Tuple<T, T>(o, p));
                        }
                    }
                    // 空間内のオブジェクトとstack内のオブジェクトをチェック
                    foreach (var pNode in stack)
                    {
                        var o = oNode.Value; var p = pNode.Value;
                        if (_collisionTest(o, p)) result.Add(new Tuple<T, T>(o, p));
                    }
                }
            }

            int nextLevel = curLevel + 1;
            if (nextLevel >= MAXLEVEL) return;

            // 子空間のチェック
            int objNum = 0;
            if (_cells[i].first != null) // スタックに今の空間のオブジェクトを積む
            {
                for (var oNode = _cells[i].first; oNode != null; oNode = oNode.next)
                { stack.Push(oNode); objNum++; }
            }

            long cl = (curMorton << 2) + 0; // この空間に属する子空間の最初
            long cr = (curMorton << 2) + 3; // この空間に属する子空間の最後

            for (long cm = cl; cm <= cr; cm++)
            {
                long ci = GetCellIdx(nextLevel, cm);
                if (_cells[ci] != null) // 空間に登録されているオブジェクトがある
                    GetCollisionListIter(nextLevel, cm, ref stack, ref result);
            }

            if (_cells[i].first != null) // スタックから今の空間のオブジェクトを外す
            {
                for (int o = 0; o < objNum; o++)
                    stack.Pop();
            }
        }

        private void CreateCell(long cellIdx)
        {
            while (_cells[cellIdx] == null)
            {
                _cells[cellIdx] = new Cell(); // 指定要素番号に空間作成
                if (cellIdx == 0) break;
                cellIdx = (cellIdx - 1) >> 2; // 親の空間へ
            }
        }

        private long GetCellIdx(int level, long mortonNum)
        {
            int n = (_levelNum[level] - 1) / 3; // その上の空間までで持っているセルの数
            return n + mortonNum;
        }

        /// <summary>1ビット飛ばしにずらす (0b00110 -> 0b101000)</summary>
        private long BitSeparate(long n)
        {
            for (int i = MAXLEVEL-3; i >= 0; i--)
            {
                n = (n | (n << (int)Math.Pow(2, i))) & _bitPattern[i];
            }
            return n;
        }

        /// <summary>
        /// モートン番号を算出する
        /// </summary>
        /// <param name="x">xインデクス</param>
        /// <param name="y">yインデクス</param>
        /// <returns>モートン番号</returns>
        private long GetMortonNumberIdx(long x, long y)
        {
            return BitSeparate(x) | (BitSeparate(y) << 1);
        }

        /// <summary>
        /// モートン番号を算出する
        /// </summary>
        /// <param name="x">x座標</param>
        /// <param name="y">y座標</param>
        /// <returns>モートン番号</returns>
        private long GetMortonNumber(double x, double y)
        {
            return GetMortonNumberIdx((int)(x * _unitWInv), (int)(y * _unitHInv));
        }

        /// <summary>
        /// セル配列内での要素番号を得る
        /// </summary>
        /// <param name="left">オブジェクトの左上座標X</param>
        /// <param name="top">オブジェクトの左上座標Y</param>
        /// <param name="right">オブジェクトの右下座標X</param>
        /// <param name="bottom">オブジェクトの右下座標Y</param>
        /// <returns></returns>
        private long GetCellArrayIdx(double left, double top, double right, double bottom)
        {
            long ltCell = GetMortonNumber(left, top);
            long rbCell = GetMortonNumber(right, bottom);
            long xor = ltCell ^ rbCell;
            int l = MAXLEVEL-1;
            for (int i = MAXLEVEL-2; i >= 0; i--)
            {
                if (xor != 0) l = i;
                xor = xor >> 2;
            }
            long t = ltCell >> 2 * (MAXLEVEL - 1 - l);
            return GetCellIdx(l, t);
        }

        public struct Pair
        {
            public T Item1;
            public T Item2;
            public Pair(T item1, T item2) { Item1 = item1; Item2 = item2; }
        }

        /// <summary>登録するオブジェクト</summary>
        internal class Obj
        {
            internal Obj(T value)
            {
                Value = value;
            }

            internal T Value = default(T);
            internal Cell cell = null;
            internal Obj prev = null;
            internal Obj next = null;
        }

        /// <summary>空間を表すクラス, 実体は双方向リスト</summary>
        internal class Cell
        {
            internal Obj last = null;
            internal Obj first = null;
            internal void Add(Obj obj)
            {
                if (obj.cell == this) return; // 既に登録済みなら無視
                if (obj.cell != null) obj.cell.Remove(obj); // どこかに登録されている場合は外す

                obj.prev = last;
                obj.next = null;
                if (last != null) last.next = obj;
                last = obj;
                if (first == null) first = obj;
                obj.cell = this;
            }
            internal void Remove(Obj obj)
            {
                if (first == obj)
                {
                    first = obj.next;
                    if (obj.next == null) first = null;
                }
                if (last == obj)
                {
                    last = obj.prev;
                    if (obj.prev == null) last = null;
                }
                if (obj.prev != null) obj.prev.next = obj.next;
                if (obj.next != null) obj.next.prev = obj.prev;
                obj.prev = null; obj.next = null;
                obj.cell = null;
            }
        }
    }
}

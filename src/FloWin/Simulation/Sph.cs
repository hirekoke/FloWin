using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

using System.Windows.Media.Imaging;

namespace FloWin.Simulation
{
    using ParticlePair = QuadTree<Particle>.Pair; //Tuple<Particle, Particle>;

    /// <summary>SPHを用いた粒子シミュレーション</summary>
    class Sph
    {
        #region 定数
        public const double H = 5;
        private const double H2 = H * H;

        public const double WALL_RADIUS = 20;
        /// <summary>気体定数 8.314472 J / (K * mol), J = N * m</summary>
        private const double GAS_CONSTANT = 1;

        private const double PARTICLE_MASS = 3;

        /// <summary>粘度</summary>
        private const double VISCOSITY = 0.1;//0.16;

        private const double WALL_DENSITY = 0.001; //0.0001;
        /// <summary>壁との粘性定数</summary>
        private const double WALL_VISCOSITY = 0.07;
        /// <summary>壁からの圧力定数</summary>
        private const double WALL_PRESSURE = 0.035;
        
        /// <summary>重力加速度</summary>
        private const double GRAVITATIONAL_ACCEL = 0.05;

        /// <summary>時間ステップ(s)</summary>
        private const double TIME_DIFF = 2;

        /// <summary>粒子配置時の隙間</summary>
        private const int PARTICLE_PAD = (int)(H / 2.0);
        #endregion

        /// <summary>粒子</summary>
        private List<Particle> _particles = new List<Particle>();

        /// <summary>初期密度</summary>
        private double _initialDensity;

        /// <summary>壁重み</summary>
        private uint[] _wallPixels;

        /// <summary>シミュレーション領域</summary>
        private Int32Rect _simRect;

        /// <summary>空間分割</summary>
        private Simulation.QuadTree<Particle> _quadTree;

        static Sph()
        {
            Kernel2D.Kernel2D.H = H;
        }

        public Sph(uint[] wallPixels, Int32Rect screenRect)
        {
            _simRect = screenRect;
            _wallPixels = wallPixels;
            _quadTree = new Simulation.QuadTree<Particle>(
                _simRect.X, _simRect.Y, _simRect.X + _simRect.Width, _simRect.Y + _simRect.Height,
                (p1, p2) => {
                    if(p1 == p2) return false;
                    return (p1.Location - p2.Location).LengthSquared < H2;
                });
            Init();
        }

        private void Init()
        {
            _initialDensity = Kernel2D.Poly6.Func(0);

#if DEBUG
            // 流体粒子
            for (int i = 0; i < 64; i++)
            {
                Particle p = new Particle();
                p.Velocity = new Vector(0, 0);
                p.Type = ParticleType.Fluid;
                p.Mass = PARTICLE_MASS;
                p.Density = 0;

                double x = i % 8;
                double y = (i - x) / 8.0;
                p.Location = new Point(x * PARTICLE_PAD + 220, y * PARTICLE_PAD + 200);

                _particles.Add(p);
            }
#endif

            // 近傍粒子ペアを更新
            UpdateNeighborParticles();

            CalcDensity();
        }

        public void AddParticles(double x1, double y1, double x2, double y2)
        {
            Vector vec = new Vector(x2 - x1, y2 - y1);
            int num = (int)(vec.Length / (double)PARTICLE_PAD);
            if (num == 0) return;

            double dx = (x2 - x1) / (double)num;
            double dy = (y2 - y1) / (double)num;
            double px = x1; double py = y1;
            for (int i = 0; i < num; i++)
            {
                Particle p = new Particle();
                p.Velocity = new Vector(0, 1);
                p.Type = ParticleType.Fluid;
                p.Mass = PARTICLE_MASS;
                p.Density = 0;
                p.Location = new Point(px, py);
                _particles.Add(p);

                px += dx; py += dy;
            }
            CalcDensity();
        }

        public IEnumerable<Point> ParticleLocations
        {
            get
            {
                foreach (Particle p in _particles)
                {
                    yield return p.Location;
                }
            }
        }
        public IEnumerable<Particle> Particles
        {
            get { foreach (Particle p in _particles) yield return p; }
        }

        public long ParticleCount { get { return _particles.Count; } }

        private List<ParticlePair> _neighborPair = new List<ParticlePair>();
        private IEnumerable<ParticlePair> NeighborParticlePair
        {
            get
            {
                if (_neighborPair != null) foreach (var p in _neighborPair) yield return p;
            }
        }
        private Particle[,] _neighbors = new Particle[0,2];

        private void UpdateNeighborParticles()
        {
            foreach (Particle p in Particles)
                _quadTree.Regist(p, p.Location.X - H, p.Location.Y - H,
                    p.Location.X + H, p.Location.Y + H);
            
            _neighborPair.Clear();
            foreach (var pp in _quadTree.GetCollisionList()) _neighborPair.Add(pp);
            //_neighbors = _quadTree.GetCollisionArray();
        }

        /// <summary>一回のステップの初期化</summary>
        private void Setup()
        {
            // ゼロクリア
            foreach (Particle p in _particles)
            {
                p.Density = 2.0 * Kernel2D.Poly6.Func(0);
                p.Pressure = _initialDensity / 2;
                p.Force = new Vector(0, 0);

                // 密度 壁重み
                double wallDensity = GetPixelValue(p.Location.X, p.Location.Y);
                if (wallDensity > 0)
                {
                    p.Density += WALL_DENSITY * (1 - wallDensity);
                }
            }

            // 近傍粒子のペアを更新する
            UpdateNeighborParticles();
        }

        /// <summary>密度計算</summary>
        private void CalcDensity()
        {
            foreach (ParticlePair pp in NeighborParticlePair)
            {
            //for (int i = 0; i < _neighbors.GetLength(0); i++)
            //{
                Particle pi = pp.Item1; Particle pj = pp.Item2;
                //Particle pi = _neighbors[i, 0]; Particle pj = _neighbors[i, 1];
                if (pi == null) break;

                double r2 = (pj.Location - pi.Location).LengthSquared;
                double poly6 = Kernel2D.Poly6.Func(r2);
                pi.Density += pj.Mass * poly6;
                pj.Density += pi.Mass * poly6;
            }
        }

        /// <summary>圧力計算</summary>
        private void CalcPressure()
        {
            foreach (Particle p in _particles)
            {
                //// [Becker2007] Tait方程式から (非圧縮性)
                // p.Pressure = _initialDensity * 88.5 * 88.5 / 7.0 * (Math.Pow(p.Density / _initialDensity, 7) - 1);
                p.Pressure = GAS_CONSTANT * (p.Density - _initialDensity);
            }
        }

        /// <summary>かかる力を計算</summary>
        private void CalcForce()
        {
            foreach (ParticlePair pp in NeighborParticlePair)
            {
            //for (int i = 0; i < _neighbors.GetLength(0); i++)
            //{
                Particle pi = pp.Item1; Particle pj = pp.Item2;
                //Particle pi = _neighbors[i, 0]; Particle pj = _neighbors[i, 1];
                if (pi == null) break;

                Vector diffP = pj.Location - pi.Location;
                Vector diffV = pj.Velocity - pi.Velocity;
                Vector diffOP = diffP - diffV;
                Random r = new Random();
                diffOP.X += r.NextDouble() * 0.2;
                diffOP.Y += r.NextDouble() * 0.2;

                Debug.Assert(pi.Density > 0);
                Debug.Assert(pj.Density > 0);

                // 圧力項
                Vector fp = new Vector(0, 0);
                Kernel2D.Spiky.Gradient(diffP, diffOP, ref fp);
                pi.Force += 0.5 * pj.Mass * (pi.Pressure + pj.Pressure) / pj.Density * fp;
                pj.Force -= 0.5 * pi.Mass * (pj.Pressure + pi.Pressure) / pi.Density * fp;
                if (diffP.LengthSquared < H2)
                {
                    //Console.WriteLine(Environment.TickCount + ", " + fp.X + ", " + pi.Force);
                }

                // 粘性拡散項
                double lap = Kernel2D.Viscosity.Laplacian(diffP.Length);
                Vector fv = diffV * lap * VISCOSITY;
                pi.Force += pj.Mass * fv / pj.Density;
                pj.Force -= pi.Mass * fv / pi.Density;
            }

            // 外力
            foreach (Particle p in _particles)
            {
                // 重力
                p.Force += (new Vector(0, GRAVITATIONAL_ACCEL)) * p.Density;

                // 壁重み
                int x = (int)p.Location.X; int y = (int)p.Location.Y;
                double cw = GetPixelValue(x, y);
                double lw = GetPixelValue(x - 2, y); // 左
                double rw = GetPixelValue(x + 2, y); // 右
                double uw = GetPixelValue(x, y - 2); // 上
                double dw = GetPixelValue(x, y + 2); // 下

                // 壁との粘性
                if (cw > 0 && p.Velocity.LengthSquared > 0)
                {
                    Vector wallVis = -WALL_VISCOSITY * VISCOSITY * p.Velocity * (1 - cw);
                    p.Force += wallVis;
                }

                // 壁からの圧力
                Vector wallVec = new Vector(rw - lw, dw - uw);
                if (wallVec.LengthSquared > 0 && cw > 0)
                {
                    wallVec.Normalize();
                    Vector wallPress = -WALL_PRESSURE * wallVec * H * cw;
                    p.Force += wallPress;
                }
            }

        }

        /// <summary>
        /// ピクセルの値を0-1で返す, 1=壁, 0=無し
        /// </summary>
        /// <param name="x">位置 x</param>
        /// <param name="y">位置 y</param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private double GetPixelValue(double x, double y)
        {
            int ploc = (int)x + (int)(y * _simRect.Width);
            uint pixel = 0x00ffffff;
            if (ploc >= 0 && ploc < _wallPixels.Length) pixel = _wallPixels[ploc];
            uint r = (pixel >> 16) & 0xff;
            return 1.0 - (r / 255.0);
        }
        private bool IsInWall(double x, double y)
        {
            if (x < 0 || y < 0 || x >= _simRect.Width || y >= _simRect.Height) return true;

            int ploc = (int)x + (int)(y * _simRect.Width);
            uint pixel = 0x00ffffff;
            if (ploc >= 0 && ploc < _wallPixels.Length) pixel = _wallPixels[ploc];
            uint g = (pixel >> 8) & 0xff;
            return g != 0xff;
        }

        /// <summary>力に応じて移動</summary>
        private void Move()
        {
            foreach (Particle p in _particles)
            {
                // 力, 加速度計算
                if (p.Type == ParticleType.Boundary)
                {
                    p.Velocity = new Vector(0, 0);
                }
                else
                {
                    // 速度更新
                    //// とりあえずEular法
                    p.Velocity += p.Force / p.Density * TIME_DIFF;
                    p.Location += p.Velocity * TIME_DIFF;

                    // 窓の中に入っている
                    if (IsInWall(p.Location.X, p.Location.Y))
                    {

                    }

                    // 画面外に出ようとするのを戻す
                    if (p.Location.X < _simRect.X)
                    {
                        p.Location.X = _simRect.X + 2;
                        p.Velocity.X = 0;
                    }
                    if (p.Location.Y < _simRect.Y)
                    {
                        p.Location.Y = _simRect.Y + 2;
                        p.Velocity.Y = 0;
                    }
                    if (p.Location.X > _simRect.X + _simRect.Width)
                    {
                        p.Location.X = _simRect.X + _simRect.Width - 2;
                        p.Velocity.X = 0;
                    }
                    if (p.Location.Y > _simRect.Y + _simRect.Height)
                    {
                        p.Location.Y = _simRect.Y + _simRect.Height - 2;
                        p.Velocity.Y = 0;
                    }
                }
            }
        }

        public void Step()
        {
            long t1 = Environment.TickCount;
            Setup();

            long t2 = Environment.TickCount;
            CalcDensity();
            long t3 = Environment.TickCount;
            CalcPressure();
            long t4 = Environment.TickCount;
            CalcForce();
            long t5 = Environment.TickCount;

            Move();
            long t6 = Environment.TickCount;

            //Console.WriteLine("{0}  : setup {1}, density {2}, pressure {3}, force {4}, move {5}",
            //    _particles.Count, t2 - t1, t3 - t2, t4 - t3, t5 - t4, t6 - t5);
        }
    }
}

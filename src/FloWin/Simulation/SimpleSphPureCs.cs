using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace FloWin.Simulation
{
    using ParticlePair = KeyValuePair<Particle, Particle>;

    class SimpleSphPureCs
    {
        #region 定数

        private const double H = 30;
        private const double H2 = H * H;

        /// <summary>気体定数 8.314472 J / (K * mol), J = N * m</summary>
        private const double GAS_CONSTANT = 10;

        /// <summary>粘度(N*s/m^2) 25度の水</summary>
        private const double VISCOSITY_CONSTANT_WATER = 0.001;

        /// <summary>水の比重 1000 kg/m^3 = 9807 N/m^3</summary>
        private const double SPECIFIC_GRAVITY_WATER = 9807;
        
        /// <summary>重力加速度(m/s^2)</summary>
        private const double GRAVITATIONAL_ACCEL = 0.098;

        /// <summary>時間ステップ(s)</summary>
        private const double TIME_DIFF = 0.1;

        /// ピクセルとメートルのスケーリングが必要
        /// 1px / 1m = 0.00001 とか
        private const double SCALE = 0.00001;
        private const double SCALE2 = SCALE * SCALE;
        #endregion

        /// <summary>粒子</summary>
        private List<Particle> _particles = new List<Particle>();

        /// <summary>初期密度</summary>
        private double _initialDensity;

        public SimpleSphPureCs()
        {
            Init();
        }

        private void Init()
        {
            Kernel2D.Kernel2D.H = H;

            // 流体粒子
            for (int i = 0; i < 16; i++)
            {
                Particle p = new Particle();
                p.Velocity = new Vector(0, 0);
                p.Type = ParticleType.Fluid;
                p.Mass = 1.0; // SPECIFIC_GRAVITY_WATER / 16.0;
                p.Density = 0;

                double x = i % 4;
                double y = (i - x) / 4.0;
                p.Location = new Vector(x * 5 + 30, y * 5 + 30);

                _particles.Add(p);
            }

            CalcDensity();
            _initialDensity = 0;
            foreach (Particle p in _particles)
            {
                if (_initialDensity < p.Density) _initialDensity = p.Density;
            }
            _initialDensity = 4.0 * Kernel2D.Poly6.Func(0);

            // 固定境界粒子
            for (int i = 0; i < 50; i++)
            {
                Particle particle = new Particle();
                particle.Velocity = new Vector(0, 0);
                particle.Type = ParticleType.Boundary;
                particle.Mass = 4;
                particle.Location = new Vector(i * 2, 100);
                _particles.Add(particle);
                if (i == 8) the_particle = particle;

                Particle pLeft1 = particle.Clone();
                pLeft1.Location = new Vector(0, i * 2);
                _particles.Add(pLeft1);

                Particle pRight1 = particle.Clone();
                pRight1.Location = new Vector(100, i * 2);
                _particles.Add(pRight1);
            }
        }
        Particle the_particle;

        public IEnumerable<Point> ParticleLocations
        {
            get
            {
                foreach (Particle p in _particles)
                {
                   yield return new Point(p.Location.X, p.Location.Y);
                }
            }
        }

        private IEnumerable<ParticlePair> NeighborParticlePair
        {
            get
            {
                // 馬鹿探索
                for (int i = 0; i < _particles.Count; i++)
                {
                    Particle pi = _particles[i];
                    for (int j = i + 1; j < _particles.Count; j++)
                    {
                        Particle pj = _particles[j];
                        double r2 = (pi.Location - pj.Location).LengthSquared;
                        if (r2 < H2) yield return new ParticlePair(pi, pj);
                    }
                }
            }
        }

        /// <summary>ゼロクリア</summary>
        private void Setup()
        {
            foreach (Particle p in _particles)
            {
                p.Density = 2.0 * Kernel2D.Poly6.Func(0);
                p.Pressure = _initialDensity / 2;
                p.Force = new Vector(0, 0);
                p.ForceExt = new Vector(0, 0);
                p.ForcePressure = new Vector(0, 0);
                p.ForceViscosity = new Vector(0, 0);
            }
        }

        /// <summary>密度計算</summary>
        private void CalcDensity()
        {
            foreach (ParticlePair pp in NeighborParticlePair)
            {
                Particle pi = pp.Key; Particle pj = pp.Value;
                double r2 = (pj.Location - pi.Location).LengthSquared;
                double poly6 = Kernel2D.Poly6.Func(r2) * 100;
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
            // 外力(重力)
            foreach (Particle p in _particles)
            {
                p.ForceExt = (new Vector(0, GRAVITATIONAL_ACCEL)) * p.Density;
                p.Force += p.ForceExt;
            }

            foreach (ParticlePair pp in NeighborParticlePair)
            {
                Particle pi = pp.Key; Particle pj = pp.Value;

                Vector diffP = pj.Location - pi.Location;
                Vector diffV = pj.Velocity - pi.Velocity;

                if ((_particles[13] == pi && the_particle == pj) || (_particles[13] == pj && the_particle == pi))
                {
                    double hoge = 0;
                }

                // 圧力項
                if (pi.Density > 0.05 && pj.Density > 0.05)
                {
                    Vector fp = new Vector(0, 0);
                    Kernel2D.Spiky.Gradient(diffP, ref fp);
                    fp *= (pi.Pressure / (pi.Density * pi.Density) + pj.Pressure / (pj.Density * pj.Density));
                    pi.ForcePressure += pj.Mass * pj.Mass * fp;
                    pj.ForcePressure -= pi.Mass * pi.Mass * fp;
                    pi.Force += pj.Mass * pj.Mass * fp;
                    pj.Force -= pi.Mass * pi.Mass * fp;
                }

                // 粘性拡散項
                double lap = Kernel2D.Viscosity.Laplacian(diffP.Length);
                if (pi.Density > 0.05 && pj.Density > 0.05)
                {
                    Vector fv = -diffV * lap * VISCOSITY_CONSTANT_WATER;
                    pi.ForceViscosity += pj.Mass * fv / pj.Density;
                    pj.ForceViscosity -= pi.Mass * fv / pi.Density;
                    pi.Force += pj.Mass * fv / pj.Density;
                    pj.Force -= pi.Mass * fv / pi.Density;
                }
            }
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
                    p.Location += p.Velocity * TIME_DIFF;
                }
                else
                {
                    // 速度更新
                    //// とりあえずEular法
                    if (p.Density > 0.05)
                        p.Velocity += p.Force / p.Density * TIME_DIFF;
                    p.Location += p.Velocity * TIME_DIFF;
                }
            }
        }

        public void Step()
        {
            Setup();

            CalcDensity();
            CalcPressure();
            CalcForce();

            Move();
            if (_particles[0].Location.Y >= 60)
            {
                Particle _ = the_particle;
            }
        }

        public void Draw(DrawingContext dc)
        {
            Particle rb = _particles[13];
            String tval = String.Format("pos:{0}\nvel:{1}\nden:{2}\nprs:{3}\nfrcext:{4}\nfrcprs:{5}\nfrcvis:{6}\nfrc:{7}",
                rb.Location, rb.Velocity, rb.Density, rb.Pressure,
                rb.ForceExt, rb.ForcePressure, rb.ForceViscosity, rb.Force);
            FormattedText text = new FormattedText(tval,
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Normal),
                SystemFonts.MessageFontSize,
                Brushes.Red);
            dc.DrawText(text, new Point(10, 450));

            rb = the_particle;
            tval = String.Format("pos:{0}\nvel:{1}\nden:{2}\nprs:{3}\nfrcext:{4}\nfrcprs:{5}\nfrcvis:{6}\nfrc:{7}",
                rb.Location, rb.Velocity, rb.Density, rb.Pressure,
                rb.ForceExt, rb.ForcePressure, rb.ForceViscosity, rb.Force);
            text = new FormattedText(tval,
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Normal),
                SystemFonts.MessageFontSize,
                Brushes.Azure);
            dc.DrawText(text, new Point(10, 600));
        }
    }
}

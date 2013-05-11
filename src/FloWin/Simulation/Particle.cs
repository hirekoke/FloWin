using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace FloWin.Simulation
{
    public enum ParticleType
    {
        Fluid,
        Boundary,
    }

    /// <summary>粒子クラス</summary>
    public class Particle
    {
        private static long particleCnt = 0;

        /// <summary>粒子の種類</summary>
        public ParticleType Type;
        
        /// <summary>粒子番号</summary>
        public long Index;

        /// <summary>粒子座標</summary>
        public Point Location;

        /// <summary>粒子速度</summary>
        public Vector Velocity;

        /// <summary>質量</summary>
        public double Mass;

        /// <summary>粒子地点での密度 rho</summary>
        public double Density;

        /// <summary>粒子地点での圧力</summary>
        public double Pressure;

        /// <summary>受ける力</summary>
        public Vector Force;

        public Particle()
        {
            this.Index = particleCnt++;
        }

        public override int GetHashCode()
        {
            return (int)((Index >> 32) ^ Index);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Particle)) return false;
            return this.Index == ((Particle)obj).Index;
        }

        public override string ToString()
        {
            return string.Format("Particle[{0}] {1}", this.Index, this.Location.ToString());
        }
    }
}

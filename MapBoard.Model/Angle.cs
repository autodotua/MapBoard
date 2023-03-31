using System;
using System.Collections.Generic;
using System.Text;

namespace MapBoard.Model
{
    /// <summary>
    /// 角度
    /// </summary>
    public struct Angle : IEquatable<Angle>
    {
        private double degrees;

        private double radians;

        /// <summary>
        /// 初始化一个没有值的角
        /// </summary>
        public static Angle Empty => new Angle() { Degrees = double.NaN, Radians = double.NaN };

        /// <summary>
        /// 平角
        /// </summary>
        public static Angle Half => new Angle() { Degrees = 180, Radians = DegreeToRadians(180) };

        /// <summary>
        /// 零角
        /// </summary>
        public static Angle Zero => new Angle() { Degrees = 0, Radians = 0 };

        /// <summary>
        /// 角度值
        /// </summary>
        public double Degrees
        {
            get => degrees;
            set
            {
                degrees = value;
                radians = DegreeToRadians(value);
            }
        }

        /// <summary>
        /// 弧度值
        /// </summary>
        public double Radians
        {
            get => radians;
            set
            {
                radians = value;
                degrees = RadiansToDegree(value);
            }
        }
        
        /// <summary>
        /// 从角度值初始化
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Angle FromDegree(double d)
        {
            Angle angle = new Angle();
            angle.Degrees = d;
            angle.Radians = DegreeToRadians(d);
            return angle;
        }

        /// <summary>
        /// 从弧度值初始化
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Angle FromRadians(double r)
        {
            Angle angle = new Angle();
            angle.Radians = r;
            angle.Degrees = RadiansToDegree(r);
            return angle;
        }

        public static implicit operator Angle(double degrees)
        {
            return FromDegree(degrees);
        }

        public static Angle operator -(Angle lhs, Angle rhs)
        {
            return FromDegree(lhs.Degrees - rhs.Degrees);
        }

        public static bool operator !=(Angle lhs, Angle rhs)
        {
            return lhs.Degrees != rhs.Degrees;
        }

        public static Angle operator +(Angle lhs, Angle rhs)
        {
            return FromDegree(lhs.Degrees + rhs.Degrees);
        }

        public static bool operator <(Angle lhs, Angle rhs)
        {
            return lhs.Degrees < rhs.Degrees;
        }

        public static bool operator <=(Angle lhs, Angle rhs)
        {
            return lhs.Degrees <= rhs.Degrees;
        }

        public static bool operator ==(Angle lhs, Angle rhs)
        {
            return lhs.Degrees == rhs.Degrees;
        }

        public static bool operator >(Angle lhs, Angle rhs)
        {
            return lhs.Degrees > rhs.Degrees;
        }

        public static bool operator >=(Angle lhs, Angle rhs)
        {
            return lhs.Degrees >= rhs.Degrees;
        }

        public override bool Equals(object obj)
        {
            return obj is Angle angle && Equals(angle);
        }

        public bool Equals(Angle other)
        {
            return Degrees == other.Degrees &&
                   Radians == other.Radians;
        }

        public override int GetHashCode()
        {
            return (int)(Degrees * 1e5 + Radians * 1e6); //HashCode.Combine(Degrees, Radians);
        }

        public override string ToString()
        {
            return Degrees.ToString();
        }

        /// <summary>
        /// 角度转弧度
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private static double DegreeToRadians(double d)
        {
            return d * Math.PI / 180;
        }

        /// <summary>
        /// 弧度转角度
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private static double RadiansToDegree(double r)
        {
            return r * 180 / Math.PI;
        }
    }
}
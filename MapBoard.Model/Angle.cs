using System;
using System.Collections.Generic;
using System.Text;

namespace MapBoard.Model
{
    public struct Angle : IEquatable<Angle>
    {
        private double degrees;
        private double radians;

        public double Degrees
        {
            get => degrees;
            set
            {
                degrees = value;
                radians = DegreeToRadians(value);
            }
        }

        public double Radians
        {
            get => radians;
            set
            {
                radians = value;
                degrees = RadiansToDegree(value);
            }
        }

        private static double DegreeToRadians(double d)
        {
            return d * Math.PI / 180;
        }

        private static double RadiansToDegree(double r)
        {
            return r * 180 / Math.PI;
        }

        public static Angle FromDegree(double d)
        {
            Angle angle = new Angle();
            angle.Degrees = d;
            angle.Radians = DegreeToRadians(d);
            return angle;
        }

        public static Angle FromRadians(double r)
        {
            Angle angle = new Angle();
            angle.Radians = r;
            angle.Degrees = RadiansToDegree(r);
            return angle;
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

        public static Angle Empty => new Angle() { Degrees = double.NaN, Radians = double.NaN };
        public static Angle Zero => new Angle() { Degrees = 0, Radians = 0 };
        public static Angle Half => new Angle() { Degrees = 180, Radians = DegreeToRadians(180) };

        public static Angle operator +(Angle lhs, Angle rhs)
        {
            return FromDegree(lhs.Degrees + rhs.Degrees);
        }

        public static Angle operator -(Angle lhs, Angle rhs)
        {
            return FromDegree(lhs.Degrees - rhs.Degrees);
        }

        public static bool operator >(Angle lhs, Angle rhs)
        {
            return lhs.Degrees > rhs.Degrees;
        }

        public static bool operator >=(Angle lhs, Angle rhs)
        {
            return lhs.Degrees >= rhs.Degrees;
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

        public static bool operator !=(Angle lhs, Angle rhs)
        {
            return lhs.Degrees != rhs.Degrees;
        }

        public static implicit operator Angle(double degrees)
        {
            return FromDegree(degrees);
        }

        public override string ToString()
        {
            return Degrees.ToString();
        }
    }
}
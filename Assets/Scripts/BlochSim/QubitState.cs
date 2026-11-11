using System;
using System.Numerics;

namespace BlochSim.Model
{
    /// <summary>Point on the Bloch sphere (x, y, z), Unity-independent.</summary>
    public readonly struct BlochVector
    {
        public readonly double X, Y, Z;

        public BlochVector(double x, double y, double z) { X = x; Y = y; Z = z; }

        /// <summary>Polar angle from the +z axis, [0, pi].</summary>
        public double Theta => Math.Acos(Math.Max(-1.0, Math.Min(1.0, Z)));

        /// <summary>Azimuthal angle from the x axis, [0, 2pi).</summary>
        public double Phi
        {
            get
            {
                double p = Math.Atan2(Y, X);
                return p < 0 ? p + 2.0 * Math.PI : p;
            }
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public double Prob0 => Math.Max(0.0, Math.Min(1.0, (1.0 + Z) * 0.5));
        public double Prob1 => 1.0 - Prob0;

        /// <summary>
        /// Rotation around the (unit) axis by angle radians, right-hand rule.
        /// </summary>
        public BlochVector Rotated(BlochVector axis, double angle)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle);
            double dot = axis.X * X + axis.Y * Y + axis.Z * Z;
            double cx = axis.Y * Z - axis.Z * Y;
            double cy = axis.Z * X - axis.X * Z;
            double cz = axis.X * Y - axis.Y * X;

            return new BlochVector(
                X * c + cx * s + axis.X * dot * (1 - c),
                Y * c + cy * s + axis.Y * dot * (1 - c),
                Z * c + cz * s + axis.Z * dot * (1 - c));
        }

        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }

    /// <summary>
    /// Pure state of a qubit: alpha|0> + beta|1>.
    /// The constructor normalizes. Note: default(QubitState) is invalid (null vector),
    /// always use the constructor or the static members.
    /// </summary>
    public readonly struct QubitState
    {
        public Complex Alpha { get; }
        public Complex Beta { get; }

        public QubitState(Complex alpha, Complex beta)
        {
            double norm = Math.Sqrt(alpha.Magnitude * alpha.Magnitude + beta.Magnitude * beta.Magnitude);
            if (norm < 1e-12)
                throw new ArgumentException("A nullvektor nem érvényes qubit-állapot.");
            Alpha = alpha / norm;
            Beta = beta / norm;
        }

        public static QubitState Zero => new QubitState(Complex.One, Complex.Zero);
        public static QubitState One => new QubitState(Complex.Zero, Complex.One);
        public static QubitState Plus => new QubitState(Complex.One, Complex.One);
        public static QubitState Minus => new QubitState(Complex.One, -Complex.One);
        public static QubitState PlusI => new QubitState(Complex.One, Complex.ImaginaryOne);
        public static QubitState MinusI => new QubitState(Complex.One, -Complex.ImaginaryOne);

        /// <summary>cos(theta/2)|0> + e^{i phi} sin(theta/2)|1>.</summary>
        public static QubitState FromAngles(double theta, double phi)
        {
            return new QubitState(
                new Complex(Math.Cos(theta / 2.0), 0.0),
                Complex.FromPolarCoordinates(Math.Sin(theta / 2.0), phi));
        }

        public double Prob0 => Alpha.Magnitude * Alpha.Magnitude;
        public double Prob1 => Beta.Magnitude * Beta.Magnitude;

        public BlochVector ToBloch()
        {
            Complex ab = Complex.Conjugate(Alpha) * Beta;
            return new BlochVector(2.0 * ab.Real, 2.0 * ab.Imaginary, Prob0 - Prob1);
        }

        public override string ToString() => $"({Alpha.Real:0.###}{Alpha.Imaginary:+0.###;-0.###}i)|0> + ({Beta.Real:0.###}{Beta.Imaginary:+0.###;-0.###}i)|1>";
    }
}
using System;
using System.Numerics;

namespace BlochSim.Model
{
    /// <summary>Pont a Bloch-gömbön (x, y, z), Unity-független.</summary>
    public readonly struct BlochVector
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public BlochVector(double x, double y, double z) { X = x; Y = y; Z = z; }

        /// <summary>Polárszög a +z tengelytől, [0, pi].</summary>
        public double Theta => Math.Acos(Math.Max(-1.0, Math.Min(1.0, Z)));

        /// <summary>Azimutszög az x tengelytől, [0, 2pi).</summary>
        public double Phi
        {
            get
            {
                double p = Math.Atan2(Y, X);
                return p < 0 ? p + 2.0 * Math.PI : p;
            }
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }

    /// <summary>
    /// Egy qubit tiszta állapota: alpha|0> + beta|1>.
    /// A konstruktor normalizál. Megjegyzés: default(QubitState) érvénytelen (nullvektor),
    /// mindig a konstruktort vagy a statikus tagokat használd.
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

        /// <summary>|<this|other>|^2. 1, ha az állapotok globális fázistól eltekintve azonosak.</summary>
        public double Fidelity(QubitState other)
        {
            Complex overlap = Complex.Conjugate(Alpha) * other.Alpha + Complex.Conjugate(Beta) * other.Beta;
            return overlap.Magnitude * overlap.Magnitude;
        }

        public bool ApproxEquals(QubitState other, double tol = 1e-9) => Math.Abs(1.0 - Fidelity(other)) < tol;

        public override string ToString() => $"({Alpha.Real:0.###}{Alpha.Imaginary:+0.###;-0.###}i)|0> + ({Beta.Real:0.###}{Beta.Imaginary:+0.###;-0.###}i)|1>";
    }
}

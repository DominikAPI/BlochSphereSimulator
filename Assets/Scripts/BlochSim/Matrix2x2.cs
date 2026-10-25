using System;
using System.Numerics;

namespace BlochSim.Model
{
    /// <summary>2x2-es komplex mátrix (egy qubit kapuinak ábrázolására).</summary>
    public readonly struct Matrix2x2
    {
        public readonly Complex M00;
        public readonly Complex M01;
        public readonly Complex M10;
        public readonly Complex M11;


        public Matrix2x2(Complex m00, Complex m01, Complex m10, Complex m11)
        {
            M00 = m00; M01 = m01; M10 = m10; M11 = m11;
        }

        public static Matrix2x2 Identity =>
            new Matrix2x2(Complex.One, Complex.Zero, Complex.Zero, Complex.One);

        /// <summary>a * b: a vektorra előbb b hat, aztán a.</summary>
        public static Matrix2x2 operator *(Matrix2x2 a, Matrix2x2 b)
        {
            return new Matrix2x2(
                a.M00 * b.M00 + a.M01 * b.M10, a.M00 * b.M01 + a.M01 * b.M11,
                a.M10 * b.M00 + a.M11 * b.M10, a.M10 * b.M01 + a.M11 * b.M11);
        }

        public QubitState Apply(QubitState s)
        {
            return new QubitState(M00 * s.Alpha + M01 * s.Beta,
                                  M10 * s.Alpha + M11 * s.Beta);
        }

        /// <summary>Konjugált transzponált.</summary>
        public Matrix2x2 Dagger()
        {
            return new Matrix2x2(
                Complex.Conjugate(M00), Complex.Conjugate(M10),
                Complex.Conjugate(M01), Complex.Conjugate(M11));
        }

        public bool IsUnitary(double tol = 1e-9)
        {
            Matrix2x2 p = Dagger() * this;
            return (p.M00 - Complex.One).Magnitude < tol
                && p.M01.Magnitude < tol
                && p.M10.Magnitude < tol
                && (p.M11 - Complex.One).Magnitude < tol;
        }
    }
}

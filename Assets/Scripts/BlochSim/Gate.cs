using System;
using System.Globalization;
using System.Numerics;

namespace BlochSim.Model
{
    /// <summary>The gate's effect on the sphere: rotation around Axis (unit vector) by Angle radians (right-hand rule).</summary>
    public readonly struct RotationInfo
    {
        public readonly BlochVector Axis;
        public readonly double Angle;

        public RotationInfo(BlochVector axis, double angle) { Axis = axis; Angle = angle; }
    }

    public sealed class Gate
    {
        public string Name { get; }
        public Matrix2x2 Matrix { get; }

        /// <summary>For the animation: up to a global phase, the matrix corresponds to this rotation.</summary>
        public RotationInfo Rotation { get; }

        public Gate(string name, Matrix2x2 matrix, RotationInfo rotation)
        {
            Name = name; Matrix = matrix; Rotation = rotation;
        }

        public QubitState Apply(QubitState s) => Matrix.Apply(s);

        public Gate Dagger()
        {
            return new Gate(Name + "†", Matrix.Dagger(),
                new RotationInfo(Rotation.Axis, -Rotation.Angle));
        }

        public override string ToString() => Name;
    }

    /// <summary>Factory for the built-in gates.</summary>
    public static class Gates
    {
        private static readonly BlochVector AxisX = new BlochVector(1, 0, 0);
        private static readonly BlochVector AxisY = new BlochVector(0, 1, 0);
        private static readonly BlochVector AxisZ = new BlochVector(0, 0, 1);

        private static string Deg(double rad) =>
            (rad * 180.0 / Math.PI).ToString("0.##", CultureInfo.InvariantCulture) + "°";

        public static Gate I =>
            new Gate("I", Matrix2x2.Identity, new RotationInfo(AxisZ, 0.0));

        public static Gate X =>
            new Gate("X", new Matrix2x2(Complex.Zero, Complex.One, Complex.One, Complex.Zero),
                new RotationInfo(AxisX, Math.PI));

        public static Gate Y =>
            new Gate("Y", new Matrix2x2(Complex.Zero, new Complex(0, -1), new Complex(0, 1), Complex.Zero),
                new RotationInfo(AxisY, Math.PI));

        public static Gate Z =>
            new Gate("Z", new Matrix2x2(Complex.One, Complex.Zero, Complex.Zero, -Complex.One),
                new RotationInfo(AxisZ, Math.PI));

        public static Gate H
        {
            get
            {
                double s = 1.0 / Math.Sqrt(2.0);
                return new Gate("H",
                    new Matrix2x2(s, s, s, -s),
                    new RotationInfo(new BlochVector(s, 0, s), Math.PI));
            }
        }

        public static Gate Phase(double phi) => PhaseGate("P(" + Deg(phi) + ")", phi);

        public static Gate S => PhaseGate("S", Math.PI / 2.0);
        public static Gate T => PhaseGate("T", Math.PI / 4.0);
        public static Gate Sdg => S.Dagger();
        public static Gate Tdg => T.Dagger();

        private static Gate PhaseGate(string name, double phi) =>
            new Gate(name,
                new Matrix2x2(Complex.One, Complex.Zero, Complex.Zero, Complex.FromPolarCoordinates(1.0, phi)),
                new RotationInfo(AxisZ, phi));

        public static Gate Rx(double theta)
        {
            double c = Math.Cos(theta / 2.0), s = Math.Sin(theta / 2.0);
            return new Gate("Rx(" + Deg(theta) + ")",
                new Matrix2x2(c, new Complex(0, -s), new Complex(0, -s), c),
                new RotationInfo(AxisX, theta));
        }

        public static Gate Ry(double theta)
        {
            double c = Math.Cos(theta / 2.0), s = Math.Sin(theta / 2.0);
            return new Gate("Ry(" + Deg(theta) + ")",
                new Matrix2x2(c, -s, s, c),
                new RotationInfo(AxisY, theta));
        }

        public static Gate Rz(double theta)
        {
            return new Gate("Rz(" + Deg(theta) + ")",
                new Matrix2x2(Complex.FromPolarCoordinates(1.0, -theta / 2.0), Complex.Zero,
                              Complex.Zero, Complex.FromPolarCoordinates(1.0, theta / 2.0)),
                new RotationInfo(AxisZ, theta));
        }
    }
}

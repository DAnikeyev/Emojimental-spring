using System;

namespace Emojimental.GameEngine.Numbers;

public static class MathHelper
{
    private static readonly Random _random = new();

    /// <summary>
    /// Generates a random value from a Gamma distribution with shape k and scale theta.
    /// Uses Marsaglia and Tsang's method.
    /// </summary>
    public static double NextGamma(double k, double theta)
    {
        if (k <= 0) throw new ArgumentException("Shape k must be positive");
        if (theta <= 0) throw new ArgumentException("Scale theta must be positive");

        if (k < 1)
        {
            return NextGamma(k + 1, theta) * Math.Pow(_random.NextDouble(), 1.0 / k);
        }

        double d = k - 1.0 / 3.0;
        double c = 1.0 / Math.Sqrt(9.0 * d);

        while (true)
        {
            double x, v;
            do
            {
                x = NextGaussian();
                v = 1.0 + c * x;
            } while (v <= 0);

            v = v * v * v;
            double u = _random.NextDouble();
            if (u < 1.0 - 0.0331 * x * x * x * x)
            {
                return d * v * theta;
            }
            if (Math.Log(u) < 0.5 * x * x + d * (1.0 - v + Math.Log(v)))
            {
                return d * v * theta;
            }
        }
    }

    public static double NextGaussian()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}

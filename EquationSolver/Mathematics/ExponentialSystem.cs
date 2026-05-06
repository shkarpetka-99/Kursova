using System;

namespace EquationSolver.Mathematics;

public class ExponentialSystem(int n, double[] coefficients) : EquationSystem(n, coefficients, 4)
{
    public override double[] Evaluate(double[] x)
    {
        double[] f = new double[Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            double a = Coefficients[i * 4];
            double b = Coefficients[i * 4 + 1];
            double c = Coefficients[i * 4 + 2];
            double d = Coefficients[i * 4 + 3];

            int prev = (i - 1 + Dimension) % Dimension;
            int next = (i + 1) % Dimension;

            f[i] = a * Math.Exp(x[i]) + b * x[prev] + c * x[next] - d;
        }
        return f;
    }

    public override double[,] GetJacobian(double[] x)
    {
        double[,] jacobian = new double[Dimension, Dimension];
        for (int i = 0; i < Dimension; i++)
        {
            double a = Coefficients[i * 4];
            double b = Coefficients[i * 4 + 1];
            double c = Coefficients[i * 4 + 2];

            int prev = (i - 1 + Dimension) % Dimension;
            int next = (i + 1) % Dimension;

            jacobian[i, prev] += b;
            jacobian[i, i] = a * Math.Exp(x[i]);
            jacobian[i, next] += c;
        }
        return jacobian;
    }
}
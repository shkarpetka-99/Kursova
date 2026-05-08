using System;

namespace EquationSolver.Mathematics;

public abstract class EquationSystem
{
    public int Dimension { get; }
    public double[] Coefficients { get; }

    protected EquationSystem(int n, double[] coefficients, int coefficientsPerEquation)
    {
        if (n < 2 || n > 10)
            throw new ArgumentException("Кількість рівнянь повинна бути 2-10.");

        if (coefficients == null || coefficients.Length < n * coefficientsPerEquation)
            throw new ArgumentException($"Необхідно {n * coefficientsPerEquation} коєфіцієнтів.");

        Dimension = n;
        Coefficients = coefficients;
    }

    public abstract double[] Evaluate(double[] x);
    public abstract double[,] GetJacobian(double[] x);
}
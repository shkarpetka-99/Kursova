using System;

namespace EquationSolver.Mathematics;

public class GaussSolver
{
    private readonly double[,] matrix;
    private readonly double[] rightPart;
    public double[] Result { get; private set; }

    public GaussSolver(double[,] matrix, double[] rightPart)
    {
        this.matrix = (double[,])matrix.Clone();
        this.rightPart = (double[])rightPart.Clone();
    }

    public void Solve()
    {
        int n = rightPart.Length;
        double[,] a = matrix; 
        double[] b = rightPart;

        for (int i = 0; i < n; i++)
        {
            int pivot = i;
            for (int j = i + 1; j < n; j++)
            {
                if (Math.Abs(a[j, i]) > Math.Abs(a[pivot, i])) pivot = j;
            }

            for (int k = i; k < n; k++)
            {
                (a[i, k], a[pivot, k]) = (a[pivot, k], a[i, k]);
            }
            (b[i], b[pivot]) = (b[pivot], b[i]);

            if (Math.Abs(a[i, i]) < 1e-15)
                throw new InvalidOperationException("Матриця вироджена.");

            for (int j = i + 1; j < n; j++)
            {
                double factor = a[j, i] / a[i, i];
                b[j] -= factor * b[i];
                for (int k = i; k < n; k++) a[j, k] -= factor * a[i, k];
            }
        }
        
        double[] x = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = 0;
            for (int j = i + 1; j < n; j++) sum += a[i, j] * x[j];
            x[i] = (b[i] - sum) / a[i, i];
        }

        Result = x;
    }
}
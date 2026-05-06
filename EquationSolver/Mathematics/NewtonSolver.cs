using System;
using System.Collections.Generic;
using System.Linq;
using EquationSolver.Models;

namespace EquationSolver.Mathematics;

public class NewtonSolver : IEquationSolver
{
    public int MaxIterations { get; set; } = 10000;
    public double Tolerance { get; set; } = 1e-7;

    public SolvingResult Solve(EquationSystem system, double[] initialGuess)
    {
        double[] xk = (double[])initialGuess.Clone();
        var history = new List<double[]>();
        history.Add((double[])xk.Clone());

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            double[] fValues = system.Evaluate(xk);
            double[,] jacobian = system.GetJacobian(xk);

            double[] negativeF = fValues.Select(f => -f).ToArray();

            try
            {
                var gauss = new GaussSolver(jacobian, negativeF);
                gauss.Solve();
                double[] deltaX = gauss.Result;

                for (int i = 0; i < xk.Length; i++)
                {
                    xk[i] += deltaX[i];
                }

                history.Add((double[])xk.Clone());

                if (CalculateNorm(deltaX) < Tolerance)
                {
                    return new SolvingResult(xk, iter + 1, history, true);
                }
            }
            catch (InvalidOperationException ex)
            {
                return new SolvingResult(null, iter + 1, history, false, ex.Message);
            }
        }

        return new SolvingResult(xk, MaxIterations, history, false, "Досягнуто ліміт ітерацій без збіжності.");
    }
    
    private double CalculateNorm(double[] vector) => Math.Sqrt(vector.Sum(v => v * v));
}
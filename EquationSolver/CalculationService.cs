using System;
using EquationSolver.Enums;
using EquationSolver.Mathematics;
using EquationSolver.Models;

namespace EquationSolver;

public class CalculationService
{
    public static SolvingResult Run(SystemType sysType, MethodType methodType, int n, double[] coeffs, double[] initialGuess, double tolerance)
    {
        EquationSystem system = sysType switch
        {
            SystemType.Power => new PowerSystem(n, coeffs),
            SystemType.Trigonometric => new TrigonometricSystem(n, coeffs),
            SystemType.Exponential => new ExponentialSystem(n, coeffs),
            _ => throw new ArgumentException("Unknown system")
        };

        IEquationSolver solver = methodType switch
        {
            MethodType.Newton => new NewtonSolver(),
            MethodType.Secant => new SecantSolver(),
            _ => throw new ArgumentException("Unknown method")
        };
        solver.Tolerance = tolerance;
        return solver.Solve(system, initialGuess);
    }
}
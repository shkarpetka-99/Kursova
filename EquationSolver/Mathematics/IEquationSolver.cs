using EquationSolver.Models;

namespace EquationSolver.Mathematics;

public interface IEquationSolver
{
    double Tolerance { get; set; }
    int MaxIterations { get; set; }
    SolvingResult Solve(EquationSystem system, double[] initialGuess);
}
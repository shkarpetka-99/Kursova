using System.Collections.Generic;

namespace EquationSolver.Models;

public class SolvingResult
{
    public double[]? Solution { get; }
    public int Iterations { get; }
    public List<double[]> History { get; } = new();
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }

    public SolvingResult(double[]? solution, int iterations, List<double[]> history, bool success, string error = "")
    {
        Solution = solution;
        Iterations = iterations;
        History = history;
        IsSuccess = success;
        ErrorMessage = error;
    }
}
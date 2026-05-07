using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using EquationSolver.Enums;
using EquationSolver.Models;
using EquationSolver.Mathematics;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace EquationSolver.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string[] SystemTypeNames => new[] { "Степенева", "Тригонометрична", "Експоненціальна" };
        public string[] MethodTypeNames => new[] { "Метод Ньютона", "Метод січних" };

        public string SelectedSystemName
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    SelectedSystemType = value switch
                    {
                        "Степенева" => SystemType.Power,
                        "Тригонометрична" => SystemType.Trigonometric,
                        "Експоненціальна" => SystemType.Exponential,
                        _ => SystemType.Power
                    };
                    UpdateSystemFormulaRepresentation();
                }
            }
        }

        public string SelectedMethodName
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    SelectedMethodType = value switch
                    {
                        "Метод Ньютона" => MethodType.Newton,
                        "Метод січних" => MethodType.Secant,
                        _ => MethodType.Newton
                    };
                }
            }
        }

        public SystemType SelectedSystemType
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                    UpdateSystemFormulaRepresentation();
            }
        }

        public MethodType SelectedMethodType
        {
            get;
            set => SetProperty(ref field, value);
        }

        public decimal? Dimension
        {
            get;
            set
            {
                if (value == null)
                {
                    field = null;
                    OnPropertyChanged();
                    ValidationMessage = "Будь ласка, введіть число";
                    return;
                }

                if (SetProperty(ref field, value))
                {
                    ValidationMessage = "";
                    UpdateGraphVisibility();
                    UpdateEquationRows();
                }
            }
        } = 2;

        public string SystemFormulaRepresentation
        {
            get;
            set => SetProperty(ref field, value);
        } = "";

        public decimal? ToleranceExponent
        {
            get;
            set
            {
                if (value == null)
                {
                    field = null;
                    OnPropertyChanged();
                    ValidationMessage = "Будь ласка, введіть число";
                    return;
                }

                if (SetProperty(ref field, value))
                {
                    ValidationMessage = "";
                }
            }
        } = 7;

        public string? ValidationMessage
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string? SuccessMessage
        {
            get;
            set => SetProperty(ref field, value);
        }

        public int IterationsCount
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool IsGraphVisible
        {
            get;
            set => SetProperty(ref field, value);
        } = true;

        public PlotModel? PlotModel
        {
            get;
            set => SetProperty(ref field, value);
        }

        public ObservableCollection<IterationRow> IterationHistory { get; } = new();
        public ObservableCollection<EquationRowViewModel> EquationRows { get; } = new();

        public ICommand SolveCommand { get; }
        public ICommand SaveCommand { get; }

        private SolvingResult? _lastResult;

        public MainWindowViewModel()
        {
            SolveCommand = new RelayCommand(Solve);
            SaveCommand = new RelayCommand(SaveToFileAsync);
            SelectedSystemName = SystemTypeNames[0];
            SelectedMethodName = MethodTypeNames[0];
            
            SelectedSystemType = SystemType.Power;
            UpdateSystemFormulaRepresentation();
            UpdateEquationRows();
            UpdateGraphVisibility();
            InitializePlotModel();
        }

        private void UpdateSystemFormulaRepresentation()
        {
            SystemFormulaRepresentation = SelectedSystemType switch
            {
                SystemType.Power => "fᵢ(x) = A · x²ᵢ₋₁ + B · x³ᵢ + C · xᵢ₊₁ · xᵢ - D = 0",
                SystemType.Trigonometric => "fᵢ(x) = A · sin(xᵢ₋₁) + B · sin(xᵢ₋₁)cos(xᵢ) + C · cos²(xᵢ₊₁) - D = 0",
                SystemType.Exponential => "fᵢ(x) = A · eˣ₋ⁱ + B · xᵢ₋₁ + C · xᵢ₊₁ - D = 0",
                _ => ""
            };
        }

        private void UpdateEquationRows()
        {
            int targetDim = (int)Dimension;
            while (EquationRows.Count < targetDim)
            {
                EquationRows.Add(new EquationRowViewModel { Index = EquationRows.Count + 1 });
            }
            while (EquationRows.Count > targetDim)
            {
                EquationRows.RemoveAt(EquationRows.Count - 1);
            }
        }

        private void UpdateGraphVisibility()
        {
            IsGraphVisible = (int)Dimension == 2;
        }

        private void InitializePlotModel()
        {
            PlotModel = new PlotModel 
            { 
                Title = "Графік функцій та шлях ітерацій (n=2)", 
                TextColor = OxyColors.White, 
                PlotAreaBorderColor = OxyColors.White 
            };

            PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "x₁", MajorGridlineStyle = LineStyle.Solid, MajorGridlineColor = OxyColor.Parse("#444444") });
            PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "x₂", MajorGridlineStyle = LineStyle.Solid, MajorGridlineColor = OxyColor.Parse("#444444") });
        }

        private void Solve()
        {
            ValidationMessage = "";
            SuccessMessage = "";
            IterationHistory.Clear();
            PlotModel?.Series.Clear();

            try
            {
                int dim = (int)Dimension;
                double tolerance = Math.Pow(10, -(double)ToleranceExponent);
                int decPlaces = (int)ToleranceExponent;

                double[] coeffs = new double[dim * 4];
                double[] initialGuess = new double[dim];

                for (int i = 0; i < dim; i++)
                {
                    var row = EquationRows[i];
                    coeffs[i * 4] = (double)row.A;
                    coeffs[i * 4 + 1] = (double)row.B;
                    coeffs[i * 4 + 2] = (double)row.C;
                    coeffs[i * 4 + 3] = (double)row.D;
                    initialGuess[i] = (double)row.X0;
                }
                
                _lastResult = CalculationService.Run(SelectedSystemType, SelectedMethodType, dim, coeffs, initialGuess, tolerance);
                IterationsCount = _lastResult.Iterations;

                for (int i = 0; i < _lastResult.History.Count; i++)
                {
                    IterationHistory.Add(new IterationRow(i, _lastResult.History[i], decPlaces));
                }

                if (_lastResult.IsSuccess)
                {
                    Debug.Assert(_lastResult.Solution != null);
                    SuccessMessage = "Успішно знайдено розв'язок:\n" + string.Join(", ", _lastResult.Solution.Select(v => v.ToString($"F{decPlaces}")));
                }
                else
                {
                    ValidationMessage = "Метод не збігається. " + _lastResult.ErrorMessage;
                }

                if (dim == 2)
                {
                    EquationSystem systemLocal = SelectedSystemType switch
                    {
                        SystemType.Power => new PowerSystem(2, coeffs),
                        SystemType.Trigonometric => new TrigonometricSystem(2, coeffs),
                        SystemType.Exponential => new ExponentialSystem(2, coeffs),
                        _ => throw new Exception("Unknown system")
                    };
                    DrawGraph(_lastResult, systemLocal);
                }
            }
            catch (Exception ex)
            {
                ValidationMessage = "Помилка: " + (ex.Message == "Nullable object must have a value." ? "Всі поля повинні мати значення" : ex.Message);
            }
        }

        private void DrawGraph(SolvingResult result, EquationSystem system)
        {
            double centerX = result.History.Count > 0 ? result.History.Last()[0] : (double)EquationRows[0].X0;
            double centerY = result.History.Count > 0 ? result.History.Last()[1] : (double)EquationRows[1].X0;
            double spread = 5.0;
            
            int gridSize = 100;
            double[] xArray = GenerateLinearSpace(centerX - spread, centerX + spread, gridSize);
            double[] yArray = GenerateLinearSpace(centerY - spread, centerY + spread, gridSize);
            double[,] dataF1 = new double[gridSize, gridSize];
            double[,] dataF2 = new double[gridSize, gridSize];

            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    var fVals = system.Evaluate(new[] { xArray[i], yArray[j] });
                    dataF1[i, j] = fVals[0];
                    dataF2[i, j] = fVals[1];
                }
            }

            PlotModel?.Series.Add(new ContourSeries
            {
                ColumnCoordinates = xArray,
                RowCoordinates = yArray,
                Data = dataF1,
                ContourLevels = new[] { 0.0 },
                Color = OxyColors.Red,
                Title = "f₁ = 0",
                StrokeThickness = 2
            });

            PlotModel?.Series.Add(new ContourSeries
            {
                ColumnCoordinates = xArray,
                RowCoordinates = yArray,
                Data = dataF2,
                ContourLevels = new[] { 0.0 },
                Color = OxyColors.Lime,
                Title = "f₂ = 0",
                StrokeThickness = 2
            });

            var lineSeries = new LineSeries
            {
                Title = "Ітерації",
                Color = OxyColors.Cyan,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerStroke = OxyColors.White,
                MarkerFill = OxyColors.Blue
            };

            foreach (var point in result.History)
            {
                lineSeries.Points.Add(new DataPoint(point[0], point[1]));
            }
            PlotModel?.Series.Add(lineSeries);

            if (result.History.Count > 0)
            {
                var startPoint = new ScatterSeries { MarkerType = MarkerType.Circle, MarkerSize = 5, MarkerFill = OxyColors.Yellow, Title = "Старт" };
                startPoint.Points.Add(new ScatterPoint(result.History[0][0], result.History[0][1]));
                PlotModel?.Series.Add(startPoint);
            }
            
            if (result.IsSuccess && result.Solution != null)
            {
                var endPoint = new ScatterSeries { MarkerType = MarkerType.Star, MarkerSize = 8, MarkerFill = OxyColors.Magenta, Title = "Розв'язок" };
                endPoint.Points.Add(new ScatterPoint(result.Solution[0], result.Solution[1]));
                PlotModel?.Series.Add(endPoint);
            }

            PlotModel?.InvalidatePlot(true);
            OnPropertyChanged(nameof(PlotModel));
        }

        private double[] GenerateLinearSpace(double start, double end, int steps)
        {
            double[] result = new double[steps];
            double step = (end - start) / (steps - 1);
            for (int i = 0; i < steps; i++) result[i] = start + i * step;
            return result;
        }

        private async void SaveToFileAsync()
        {
            if (_lastResult == null) return;

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Зберегти результати",
                    DefaultExtension = "txt",
                    SuggestedFileName = "EquationSolverResults.txt"
                });

                if (file != null)
                {
                    var sb = new StringBuilder();
                    int dec = (int)ToleranceExponent;
                    sb.AppendLine("Результати розв'язання");
                    sb.AppendLine($"Метод: {SelectedMethodName}");
                    sb.AppendLine($"Система: {SelectedSystemName}");
                    sb.AppendLine(
                        SystemFormulaRepresentation = SelectedSystemType switch {
                        SystemType.Power => "fᵢ(x) = A · x²ᵢ₋₁ + B · x³ᵢ + C · xᵢ₊₁ · xᵢ - D = 0",
                        SystemType.Trigonometric => "fᵢ(x) = A · sin(xᵢ₋₁) + B · sin(xᵢ₋₁)cos(xᵢ) + C · cos²(xᵢ₊₁) - D = 0",
                        SystemType.Exponential => "fᵢ(x) = A · eˣ₋ⁱ + B · xᵢ₋₁ + C · xᵢ₊₁ - D = 0",
                        _ => ""
                    });
                    sb.AppendLine($"Розмірність: {Dimension}");
                    sb.AppendLine("Коєфіцієнти");
                    sb.AppendLine(string.Format("{0,-4} | {1,-8} | {2,-8} | {3,-8} | {4,-8} | {5,-8}", 
                        "i", "A", "B", "C", "D", "X0"));
                    sb.AppendLine(new string('-', 55));
                    foreach (var row in EquationRows)
                    {
                        sb.AppendLine(string.Format("{0,-4} | {1,-8:F2} | {2,-8:F2} | {3,-8:F2} | {4,-8:F2} | {5,-8:F2}", 
                            row.Index, row.A, row.B, row.C, row.D, row.X0));
                    }
                    sb.AppendLine($"Точність: 1e-{ToleranceExponent}");
                    sb.AppendLine($"Кількість ітерацій: {IterationsCount}");
                    sb.AppendLine();
                    sb.AppendLine("Статус: " + (_lastResult.IsSuccess ? "Успіх" : "Розбіжність/Помилка: " + _lastResult.ErrorMessage));
                    
                    if (_lastResult.Solution != null)
                        sb.AppendLine($"Знайдені корені: {string.Join(", ", _lastResult.Solution.Select(v => v.ToString($"F{dec}")))}");

                    sb.AppendLine("\nКроки ітерацій:");
                    for (int i = 0; i < _lastResult.History.Count; i++)
                    {
                        sb.AppendLine($"Крок {i}: {string.Join(", ", _lastResult.History[i].Select(v => v.ToString($"F{dec}")))}");
                    }

                    await using var stream = await file.OpenWriteAsync();
                    await using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(sb.ToString());
                }
            }
        }
    }

    public class IterationRow
    {
        public int Step { get; }
        public string Values { get; }

        public IterationRow(int step, double[] values, int decimalPlaces)
        {
            Step = step;
            Values = string.Join(",  ", values.Select(v => v.ToString($"F{decimalPlaces}")));
        }
    }
}
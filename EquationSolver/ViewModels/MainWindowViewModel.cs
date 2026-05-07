using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using EquationSolver.Enums;
using EquationSolver.Models;
using EquationSolver.Mathematics;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
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

        public IRelayCommand SolveCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand SaveGraphCommand { get; }

        private SolvingResult? _lastResult;

        public MainWindowViewModel()
        {
            SolveCommand = new RelayCommand(Solve);
            SaveCommand = new RelayCommand(SaveToFileAsync, () => _lastResult != null);
            SaveGraphCommand = new RelayCommand(SaveGraphToFileAsync,
                () => _lastResult != null && _lastResult.IsSuccess && Dimension == 2);
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

                _lastResult = CalculationService.Run(SelectedSystemType, SelectedMethodType, dim, coeffs, initialGuess,
                    tolerance);
                IterationsCount = _lastResult.Iterations;

                for (int i = 0; i < _lastResult.History.Count; i++)
                {
                    IterationHistory.Add(new IterationRow(i, _lastResult.History[i], decPlaces));
                }

                if (_lastResult.IsSuccess)
                {
                    Debug.Assert(_lastResult.Solution != null);
                    SuccessMessage = "Успішно знайдено розв'язок:\n" + string.Join(", ",
                        _lastResult.Solution.Select(v => v.ToString($"F{decPlaces}")));
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
                ValidationMessage = "Помилка: " + (ex.Message == "Nullable object must have a value."
                    ? "Всі поля повинні мати значення"
                    : ex.Message);
            }

            SaveCommand.NotifyCanExecuteChanged();
            SaveGraphCommand.NotifyCanExecuteChanged();
        }

        private void UpdateGraphVisibility()
        {
            IsGraphVisible = (int)Dimension == 2;
        }

        private void InitializePlotModel()
        {
            PlotModel = new PlotModel
            {
                Title = "Графіки функцій (n=2)",
                Background = OxyColors.White,
                TextColor = OxyColors.Black,
                PlotAreaBorderColor = OxyColors.Black,
            };

            var l = new Legend
            {
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Inside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
                LegendBorder = OxyColors.Black,
                LegendBorderThickness = 1,
                LegendFontSize = 12,
                LegendMargin = 10,
                LegendTitle = "Легенда"
            };

            PlotModel.Legends.Add(l);

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "x₁",
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.Parse("#E0E0E0"),
                TicklineColor = OxyColors.Black
            });

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "x₂",
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.Parse("#E0E0E0"),
                TicklineColor = OxyColors.Black
            });
        }
        
        
        private void DrawGraph(SolvingResult result, EquationSystem system)
        {
            double centerX = result.IsSuccess && result.Solution != null
                ? result.Solution[0]
                : (double)EquationRows[0].X0;
            double centerY = result.IsSuccess && result.Solution != null
                ? result.Solution[1]
                : (double)EquationRows[1].X0;
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

            PlotModel?.Series.Clear();

            PlotModel?.Series.Add(new ContourSeries
            {
                ColumnCoordinates = xArray,
                RowCoordinates = yArray,
                Data = dataF1,
                ContourLevels = new[] { 0.0 },
                Color = OxyColors.Red,
                Title = "f1(x1,x2) = 0",
                StrokeThickness = 2
            });

            PlotModel?.Series.Add(new ContourSeries
            {
                ColumnCoordinates = xArray,
                RowCoordinates = yArray,
                Data = dataF2,
                ContourLevels = new[] { 0.0 },
                Color = OxyColors.LimeGreen,
                Title = "f2(x1,x2) = 0",
                StrokeThickness = 2
            });

            if (result.IsSuccess && result.Solution != null)
            {
                var endPoint = new ScatterSeries
                {
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 6,
                    MarkerFill = OxyColors.Blue,
                    MarkerStroke = OxyColors.Black,
                    MarkerStrokeThickness = 1,
                    Title = $"Точка перетину (розв'язок) ({result.Solution[0]:F3}, {result.Solution[1]:F3})"
                };
                endPoint.Points.Add(new ScatterPoint(result.Solution[0], result.Solution[1]));
                PlotModel?.Series.Add(endPoint);
            }
        }
        
        private double[] GenerateLinearSpace(double start, double end, int steps)
        {
            double[] result = new double[steps];
            double step = (end - start) / (steps - 1);
            for (int i = 0; i < steps; i++) result[i] = start + i * step;
            return result;
        }
        
        private async void SaveGraphToFileAsync()
        {
            if (PlotModel == null || (int)Dimension != 2) return;

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Зберегти графік як зображення",
                    DefaultExtension = "png",
                    SuggestedFileName = "EquationGraph.png",
                    FileTypeChoices = new[] { new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } } }
                });

                if (file != null)
                {
                    try
                    {
                        var exporter = new OxyPlot.Avalonia.PngExporter
                        {
                            Width = 1000,
                            Height = 800,
                        };

                        await using var stream = await file.OpenWriteAsync();
                        exporter.Export(PlotModel, stream);

                        SuccessMessage = "Графік успішно збережено!";
                    }
                    catch (Exception ex)
                    {
                        ValidationMessage = "Помилка збереження графіка: " + ex.Message;
                    }
                }
            }
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
                        SystemFormulaRepresentation = SelectedSystemType switch
                        {
                            SystemType.Power => "fᵢ(x) = A · x²ᵢ₋₁ + B · x³ᵢ + C · xᵢ₊₁ · xᵢ - D = 0",
                            SystemType.Trigonometric =>
                                "fᵢ(x) = A · sin(xᵢ₋₁) + B · sin(xᵢ₋₁)cos(xᵢ) + C · cos²(xᵢ₊₁) - D = 0",
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
                        sb.AppendLine(string.Format(
                            "{0,-4} | {1,-8:F2} | {2,-8:F2} | {3,-8:F2} | {4,-8:F2} | {5,-8:F2}",
                            row.Index, row.A, row.B, row.C, row.D, row.X0));
                    }

                    sb.AppendLine($"Точність: 1e-{ToleranceExponent}");
                    sb.AppendLine($"Кількість ітерацій: {IterationsCount}");
                    sb.AppendLine();
                    sb.AppendLine("Статус: " + (_lastResult.IsSuccess
                        ? "Успіх"
                        : "Розбіжність/Помилка: " + _lastResult.ErrorMessage));

                    if (_lastResult.Solution != null)
                        sb.AppendLine(
                            $"Знайдені корені: {string.Join(", ", _lastResult.Solution.Select(v => v.ToString($"F{dec}")))}");

                    sb.AppendLine("\nКроки ітерацій:");
                    for (int i = 0; i < _lastResult.History.Count; i++)
                    {
                        sb.AppendLine(
                            $"Крок {i}: {string.Join(", ", _lastResult.History[i].Select(v => v.ToString($"F{dec}")))}");
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
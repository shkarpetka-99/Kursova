using System.Runtime.CompilerServices;

namespace EquationSolver.ViewModels;

public class EquationRowViewModel : ViewModelBase
{
    public int Index
    {
        get;
        set => SetProperty(ref field, value);
    }

    public decimal? A
    {
        get;
        set => SetCoefficient(ref field, value);
    } = 1;

    public decimal? B
    {
        get;
        set => SetCoefficient(ref field, value);
    } = 1;

    public decimal? C
    {
        get;
        set => SetCoefficient(ref field, value);
    } = 1;

    public decimal? D
    {
        get;
        set => SetCoefficient(ref field, value);
    } = 0;

    public decimal? X0
    {
        get;
        set => SetCoefficient(ref field, value);
    } = 1;

    public string RowError
    {
        get;
        set => SetProperty(ref field, value);
    } = "";

    private void SetCoefficient(ref decimal? field, decimal? value, [CallerMemberName] string propertyName = null)
    {
        if (value == null)
        {
            field = null;
            OnPropertyChanged(propertyName); 
            RowError = "Коефіцієнт не може бути порожнім!";
            return;
        }
        
        if (SetProperty(ref field, value, propertyName))
        {
            RowError = "";
        }
    }
}
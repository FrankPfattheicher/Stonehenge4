using System.Globalization;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Forms.ViewModels;

public class DatePickerMonth
{
    public string Name { get; private set; }
    public string Year { get; private set; }

    public DatePickerWeek[] Weeks { get; internal set; } = [];
    
    public DatePickerMonth(DateTime time)
    {
        Name = time.ToString("MMMM", CultureInfo.CurrentUICulture);
        Year = time.Year.ToString(CultureInfo.CurrentUICulture);
    }

    internal IEnumerable<DatePickerDay> AllDays() => Weeks.SelectMany(week => week.AllDays());
}
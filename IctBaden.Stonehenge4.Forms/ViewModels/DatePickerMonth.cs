using System.Globalization;

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

    public DatePickerDay? GetDay(DateTime day)
    {
        foreach (var week in Weeks)
        {
            var selected = week.GetDay(day);
            if (selected != null) return selected;
        }
        return null;
    }
}
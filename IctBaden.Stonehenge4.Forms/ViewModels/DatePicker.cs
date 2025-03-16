using System.Globalization;
using IctBaden.Stonehenge.ViewModel;

namespace IctBaden.Stonehenge.Forms.ViewModels;

public class DatePicker
{
    public int Year { get; private set; } = DateTime.Now.Year;
    public DatePickerMonth[] Months { get; private set; } = [];
    public string RangeText { get; private set; } = string.Empty;

    public int TotalColumns => Months.Length * 7;

    public DateTime Start = DateTime.MinValue;
    public DateTime End = DateTime.MinValue;

    private DateTime _first;

    public DatePicker()
    {
        _first = DateTime.Now;
        CreateCalendar();
    }

    private void CreateCalendar()
    {
        var month = new DatePickerMonth(_first);
        Months = [month];
        
        var time = new DateTime(_first.Year, _first.Month, 1);
        var start = time - TimeSpan.FromDays(time.DayOfWeek - DayOfWeek.Monday);
        var firstMonth = time.Month - 1;
        if(firstMonth == 0) firstMonth = 12;
        var lastMonth = time.Month;
        var weeks = new List<DatePickerWeek>();
        while (start.Month == firstMonth || start.Month == lastMonth)
        {
            weeks.Add(new DatePickerWeek(start, time.Month));
            start += TimeSpan.FromDays(7);
        }
        month.Weeks = weeks.ToArray();
        
        RangeChanged();

        SelectDay(Start);
    }

    public void RangeChanged()
    {
        if (Start == DateTime.MinValue) return;
        
        RangeText = Start.ToString("d", CultureInfo.CurrentCulture);
        if (!DatePickerWeek.SameDay(Start, End) && End > Start)
        {
            RangeText += " .. " + End.ToString("d", CultureInfo.CurrentCulture);
        }
    }

    private DatePickerDay GetDay(DateTime day)
    {
        foreach (var month in Months)
        {
            var selected = month.GetDay(day);
            if (selected != null) return selected;
        }
        return DatePickerDay.None;
    }
    
    
    public void SelectDay(DateTime day)
    {
        var selected = GetDay(day);
        selected.IsSelected = true;
        Start = day;
        RangeChanged();
    }

    [ActionMethod]
    public void PrevMonth()
    {
        _first -= TimeSpan.FromDays(30);
        CreateCalendar();
    }
    [ActionMethod]
    public void NextMonth()
    {
        _first += TimeSpan.FromDays(30);
        CreateCalendar();
    }

}
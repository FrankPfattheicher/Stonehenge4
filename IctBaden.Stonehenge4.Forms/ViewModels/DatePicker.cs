using System.Globalization;
using IctBaden.Stonehenge.Types;
using IctBaden.Stonehenge.ViewModel;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Forms.ViewModels;

public class DatePicker : StonehengeComponent
{
    public string[] WeekDays { get; private set; } = [];
    public DatePickerMonth[] Months { get; private set; } = [];
    public string RangeText { get; private set; } = string.Empty;

    public int TotalColumns => Months.Length * 7 + (ShowWeekNumbers ? 1 : 0);

    public DateTime Start = DateTime.MinValue;
    public DateTime End = DateTime.MinValue;
    public int WeekNumber;

    private DateTime _first = DateTime.Now;

    public string EmptyText { get; init; } = string.Empty;
    public bool ShowWeekNumbers { get; init; }
    public bool SelectWeek { get; init; }

    public override void OnLoad()
    {
        CreateCalendar();
    }

    private void CreateCalendar()
    {
        WeekDays = GetWeekDayNames();

        var month = new DatePickerMonth(_first);
        Months = [month];

        var time = new DateTime(_first.Year, _first.Month, 1);
        var start = time - TimeSpan.FromDays(time.DayOfWeek - DayOfWeek.Monday);
        var firstMonth = time.Month - 1;
        if (firstMonth == 0) firstMonth = 12;
        var lastMonth = time.Month;
        var weeks = new List<DatePickerWeek>();
        while (start.Month == firstMonth || start.Month == lastMonth)
        {
            weeks.Add(new DatePickerWeek(start, time.Month));
            start += TimeSpan.FromDays(7);
        }

        month.Weeks = weeks.ToArray();

        RangeChanged();
    }

    private static string[] GetWeekDayNames()
    {
        var weekDays = CultureInfo.CurrentUICulture.DateTimeFormat.AbbreviatedDayNames;
        var weekDayNames = new List<string>();

        var startIndex = Array.IndexOf(CultureInfo.InvariantCulture.DateTimeFormat.DayNames,
            CultureInfo.CurrentUICulture.DateTimeFormat.FirstDayOfWeek.ToString());
        for (var ix = 0; ix < 7; ix++)
        {
            weekDayNames.Add(weekDays[(startIndex + ix) % 7]);
        }

        return weekDayNames.ToArray();
    }

    public void RangeChanged()
    {
        UpdateSelection();

        if (Start == DateTime.MinValue)
        {
            RangeText = EmptyText;
            return;
        }

        RangeText = Start.ToString("d", CultureInfo.CurrentCulture);
        if (!DatePickerWeek.SameDay(Start, End) && End > Start)
        {
            RangeText += " .. " + End.ToString("d", CultureInfo.CurrentCulture);
        }

        if (SelectWeek && WeekNumber != 0)
        {
            RangeText = $"KW{WeekNumber} - " + RangeText;
        }
    }

    private IEnumerable<DatePickerDay> AllDays()
    {
        foreach (var month in Months)
        {
            foreach (var day in month.AllDays())
            {
                yield return day;
            }
        }
    }
    private DatePickerDay GetDay(DateTime day)
    {
        var selected = AllDays().FirstOrDefault(d => d.DateTime == day);
        return selected ?? DatePickerDay.None;
    }

    internal static int GetWeekNumber(DateTime day)
    {
        var currentCulture = CultureInfo.CurrentUICulture;
        var calendarWeekRule = currentCulture.DateTimeFormat.CalendarWeekRule;
        return CultureInfo.CurrentUICulture.Calendar
            .GetWeekOfYear(day, calendarWeekRule, currentCulture.DateTimeFormat.FirstDayOfWeek);
    }

    private void UpdateSelection()
    {
        foreach (var day in AllDays())
        {
            day.IsSelected = false;
        }

        if (Start == DateTime.MinValue) return;

        if (SelectWeek)
        {
            for (var ix = -7; ix <= 7; ix++)
            {
                var day = GetDay(Start + TimeSpan.FromDays(ix));
                if(day.IsNone) continue;
                day.IsSelected = GetWeekNumber(day.DateTime) == WeekNumber;
            }
        }
        else
        {
            var day = GetDay(Start);
            day.IsSelected = true;
        }
    }
    
    [ActionMethod]
    public void SelectDay(DateTime selectedDay)
    {
        if (selectedDay == DateTime.MinValue) return;
        
        var selected = GetDay(selectedDay);
        selected.IsSelected = true;
        
        if (SelectWeek)
        {
            Start = DateTime.MinValue;
            End = DateTime.MinValue;
            WeekNumber = GetWeekNumber(selectedDay);
            for (var ix = -7; ix <= 7; ix++)
            {
                var day = GetDay(selectedDay + TimeSpan.FromDays(ix));
                if(day.IsNone) continue;
                day.IsSelected = GetWeekNumber(day.DateTime) == WeekNumber;
                if (day.IsSelected)
                {
                    if (Start == DateTime.MinValue)
                        Start = day.DateTime;
                    else
                        End = day.DateTime;
                }
            }
        }
        else
        {
            Start = selectedDay;
        }
        
        RangeChanged();
    }

    [ActionMethod]
    public void PrevYear()
    {
        _first = new DateTime(_first.Year - 1, _first.Month, _first.Day);
        CreateCalendar();
    }

    [ActionMethod]
    public void NextYear()
    {
        _first = new DateTime(_first.Year + 1, _first.Month, _first.Day);
        CreateCalendar();
    }

    [ActionMethod]
    public void PrevMonth()
    {
        var currentMonth = _first.Month;
        while (_first.Month == currentMonth)
        {
            _first -= TimeSpan.FromDays(1);
        }
        CreateCalendar();
    }

    [ActionMethod]
    public void NextMonth()
    {
        var currentMonth = _first.Month;
        while (_first.Month == currentMonth)
        {
            _first += TimeSpan.FromDays(1);
        }
        CreateCalendar();
    }
}
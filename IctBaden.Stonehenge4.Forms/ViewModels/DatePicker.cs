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
    public bool ShowWeekNumbers { get; init; }
    public bool ShowTodayLink { get; init; }


    public string EmptyText = string.Empty;
    public ushort ShowMonthsCount = 1;
    public DatePickerSelection Selection = DatePickerSelection.Day;
    public DateOnly MinDate = DateOnly.MinValue;
    

    public DateOnly Start = DateOnly.MinValue;
    public DateOnly End = DateOnly.MinValue;
    public int TotalColumns => Months.Length * (1 + (ShowWeekNumbers ? 1 : 0));
    public int WeekNumber;

    private DateOnly _first = DateOnly.FromDateTime(DateTime.Now);

    public override void OnLoad()
    {
        CreateCalendar();
    }

    private void CreateCalendar()
    {
        WeekDays = GetWeekDayNames();

        var monthsCount = Math.Min((ushort)3, ShowMonthsCount);
        var first = new DateOnly(_first.Year, _first.Month, 1);
        var months = new List<DatePickerMonth>();
        for (var m = 1; m <= monthsCount; m++)
        {
            months.Add(new DatePickerMonth(first));
            first = first.AddMonths(1);
        }
        Months = months.ToArray();

        foreach (var month in Months)
        {
            var time = new DateOnly(month.Date.Year, month.Date.Month, 1);    
            var start = time.AddDays(DayOfWeek.Monday - time.DayOfWeek);
            var firstMonth = time.Month - 1;
            if (firstMonth == 0) firstMonth = 12;
            var lastMonth = time.Month;
            var weeks = new List<DatePickerWeek>();
            while (start.Month == firstMonth || start.Month == lastMonth)
            {
                weeks.Add(new DatePickerWeek(start, time.Month, MinDate));
                start = start.AddDays(7);
            }

            month.Weeks = weeks.ToArray();
        }
        
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

        if (Start == DateOnly.MinValue)
        {
            RangeText = EmptyText;
            return;
        }

        RangeText = Start.ToString("d", CultureInfo.CurrentCulture);
        if (!DatePickerWeek.SameDay(Start, End) && End > Start)
        {
            RangeText += " .. " + End.ToString("d", CultureInfo.CurrentCulture);
        }

        if (Selection == DatePickerSelection.Week && WeekNumber != 0)
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

    private void SetDaySelection(DateOnly day)
    {
        foreach (var datePickerDay in AllDays())
        {
            if (datePickerDay.Date == day)
            {
                datePickerDay.IsSelected = true;
            }
        }
    }

    private DatePickerDay GetDay(DateOnly date)
    {
        var day = AllDays().FirstOrDefault(d => d.Date == date);
        return day ?? DatePickerDay.None;
    }

    internal static int GetWeekNumber(DateOnly day)
    {
        var currentCulture = CultureInfo.CurrentUICulture;
        var calendarWeekRule = currentCulture.DateTimeFormat.CalendarWeekRule;
        return CultureInfo.CurrentUICulture.Calendar
            .GetWeekOfYear(new DateTime(day.Year, day.Month, day.Day), calendarWeekRule, currentCulture.DateTimeFormat.FirstDayOfWeek);
    }

    private void UpdateSelection()
    {
        foreach (var day in AllDays())
        {
            day.IsSelected = false;
        }

        if (Start == DateOnly.MinValue) return;

        switch (Selection)
        {
            case DatePickerSelection.Day:
            {
                SetDaySelection(Start);
                break;
            }
            case DatePickerSelection.Week:
            {
                for (var ix = -7; ix <= 7; ix++)
                {
                    var day = GetDay(Start.AddDays(ix));
                    if (day.IsNone) continue;
                    if (GetWeekNumber(day.Date) == WeekNumber)
                    {
                        SetDaySelection(day.Date);
                    }
                }

                break;
            }
            case DatePickerSelection.Range:
            {
                var day = Start;
                if (End == DateOnly.MinValue)
                {
                    SetDaySelection(day);
                }
                else
                {
                    while(day <= End)
                    {
                        SetDaySelection(day);
                        day = day.AddDays(1);
                    }
                }
                break;
            }
        }
    }

    [ActionMethod]
    public void SelectDay(DateTime selected)
    {
        var selectedDay = DateOnly.FromDateTime(selected);
        
        if (selectedDay == DateOnly.MinValue) return;
        if(selectedDay.Year <= 1000) 
        {
            CloseDropdown();
            return;
        }

        SetDaySelection(selectedDay);
        switch (Selection)
        {
            case DatePickerSelection.Day:
                Start = selectedDay;
                CloseDropdown();
                break;
            case DatePickerSelection.Week:
            {
                Start = DateOnly.MinValue;
                End = DateOnly.MinValue;
                WeekNumber = GetWeekNumber(selectedDay);
                for (var ix = -7; ix <= 7; ix++)
                {
                    var day = GetDay(selectedDay.AddDays(ix));
                    if (day.IsNone) continue;
                    day.IsSelected = GetWeekNumber(day.Date) == WeekNumber;
                    if (day.IsSelected)
                    {
                        if (Start == DateOnly.MinValue)
                            Start = day.Date;
                        else
                            End = day.Date;
                    }
                }
                CloseDropdown();
                break;
            }
            case DatePickerSelection.Range:
                if (End != DateOnly.MinValue)   // new range
                {
                    Start = DateOnly.MinValue;
                    End = DateOnly.MinValue;
                }
                if (Start == DateOnly.MinValue || selectedDay < Start)
                {
                    Start = selectedDay;
                }
                else if(selectedDay >= Start)
                {
                    End = selectedDay;
                    CloseDropdown();
                }
                break;
        }

        RangeChanged();
    }

    private void CloseDropdown()
    {
        if (Session.ViewModel is ActiveViewModel vm)
        {
            vm.ExecuteClientScript($"document.getElementById('{ComponentId}').classList.remove('show');");
        }
    }
    
    [ActionMethod]
    public void PrevYear()
    {
        _first = new DateOnly(_first.Year - 1, _first.Month, _first.Day);
        CreateCalendar();
    }

    [ActionMethod]
    public void NextYear()
    {
        _first = new DateOnly(_first.Year + 1, _first.Month, _first.Day);
        CreateCalendar();
    }

    [ActionMethod]
    public void PrevMonth()
    {
        var currentMonth = _first.Month;
        while (_first.Month == currentMonth)
        {
            _first = _first.AddDays(-1);
        }
        CreateCalendar();
    }

    [ActionMethod]
    public void NextMonth()
    {
        var currentMonth = _first.Month;
        while (_first.Month == currentMonth)
        {
            _first = _first.AddDays(1);
        }
        CreateCalendar();
    }

    [ActionMethod]
    public void GotoToday()
    {
        _first = DateOnly.FromDateTime(DateTime.Now);
        CreateCalendar();
    }
}
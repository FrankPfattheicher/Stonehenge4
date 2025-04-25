
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Forms.ViewModels;

public class DatePickerDay
{
    private readonly bool _today;
    internal readonly DateOnly Date;
    internal bool IsNone;

    public string Day { get; private set; }
    public bool OtherMonth { get; }
    public bool IsSelected { get; internal set; }
    public int Number { get; private set; }
    public string Title { get; private set; }


    public string Class
    {
        get
        {
            var css = "st-calendar-day";
            if(OtherMonth) css += " st-calendar-other-month";
            else if(_today) css += " st-calendar-today";
            if(IsSelected) css += " st-calendar-selected";
            return css;
        }
    }

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatePickerDay(DateOnly date, bool today, bool otherMonth, string title)
    {
        Date = date;
        _today = today;
        OtherMonth = otherMonth;
        Title = title;
        Day = date.ToString("O");
        Number = date.Day;
    }
    
    public static readonly DatePickerDay None = 
        new(DateOnly.MinValue, today: false, otherMonth: true, string.Empty) { IsNone = true };
    
}
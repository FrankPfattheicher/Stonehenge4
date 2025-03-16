using System.Globalization;

namespace IctBaden.Stonehenge.Forms.ViewModels;

public class DatePickerDay
{
    internal readonly DateTime _date;
    public string Date { get; private set; }
    public bool Today { get; private set; }
    public bool OtherMonth { get; private set; }
    public bool IsSelected { get; internal set; }
    public int Number { get; private set; }
    public string Title { get; private set; }

    public string Class
    {
        get
        {
            var css = string.Empty;
            if(OtherMonth) css = "st-calendar-other-month";
            else if(Today) css = "st-calendar-today";
            if(IsSelected) css += " st-calendar-selected";
            return css;
        }
    }

    public DatePickerDay(DateTime date, bool today, bool otherMonth, string title)
    {
        _date = date;
        Today = today;
        OtherMonth = otherMonth;
        Title = title;
        Date = date.ToString("O");
        Number = date.Day;
    }
    
    public static readonly DatePickerDay None = new DatePickerDay(DateTime.MinValue, false, true, string.Empty);
    
}
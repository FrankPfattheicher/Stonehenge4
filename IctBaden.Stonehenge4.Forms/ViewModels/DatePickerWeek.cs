using System.Globalization;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Forms.ViewModels;

public class DatePickerWeek
{
    public string WeekNumber { get; private set; }
    public DatePickerDay[] Days { get; }
    
    public static bool SameDay(DateTime a, DateTime b)
    {
        return a.Day == b.Day
               && a.Month == b.Month
               && a.Year == b.Year;
    }

    public DatePickerWeek(DateTime time, int month)
    {
        WeekNumber = DatePicker.GetWeekNumber(time)
            .ToString(CultureInfo.CurrentCulture);
        
        var days = new List<DatePickerDay>();
        for(var day = 0; day < 7; day++)
        {
            var today = SameDay(time, DateTime.Now);
            // var appointments = dates.Where(d => OnThatDays(time, d.TimeFrom, d.TimeTo)).ToList();
            var tipText = string.Empty; 
            // var tipText = appointments.Count > 0
            //     ? string.Join(Environment.NewLine, appointments.Select(a => a.TipText))
            //     : string.Empty;
            if (today)
            {
                if (!string.IsNullOrEmpty(tipText))
                {
                    tipText = "Heute - " + tipText;
                }
                else
                {
                    tipText = "Heute";
                }
            }

            days.Add(new DatePickerDay(time, today, time.Month != month, tipText));
            time += TimeSpan.FromDays(1);
        }
        Days = days.ToArray();
    }

    internal IEnumerable<DatePickerDay> AllDays()
    {
        return Days;
    }
}
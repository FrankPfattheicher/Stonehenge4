namespace IctBaden.Stonehenge.Forms.ViewModels;

public class DatePickerWeek
{
    public DatePickerDay[] Days { get; private set; }
    
    public static bool SameDay(DateTime a, DateTime b)
    {
        return a.Day == b.Day
               && a.Month == b.Month
               && a.Year == b.Year;
    }
    public static bool OnThatDays(DateTime day, DateTime from, DateTime to)
    {
        return day.Date >= from.Date && day.Date <= to.Date;
    }

    public DatePickerWeek(DateTime time, int month)
    {
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

    public DatePickerDay? GetDay(DateTime day)
    {
        foreach (var weekDay in Days)
        {
            if(SameDay(day, weekDay._date))
                return weekDay;
        }
        return null;
    }
}
using System.Globalization;
namespace InTheOfficeBot.Helpers;

public static class Helpers
{
  public static bool IsCurrentDayAndTime(DateTime configDateTime)
  {
    // Get the current DateTime
    DateTime currentDateTime = DateTime.Now;

    // Check if the current day of the week matches the config day of the week
    if (currentDateTime.DayOfWeek == configDateTime.DayOfWeek)
    {
      // Check if the current time matches the config time (hours and minutes)
      return currentDateTime.Hour == configDateTime.Hour && currentDateTime.Minute == configDateTime.Minute;
    }

    return false;
  }

  public static (string, int) GetWeekOrNextWeek()
  {
    var today = DateTime.Today;
    var currentWeek = ISOWeek.GetWeekOfYear(today);

    // Determine if it's Friday or later (Friday, Saturday, or Sunday)
    if (today.DayOfWeek == DayOfWeek.Friday || today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday)
    {
      // Check if it's the last week of the year, and handle wraparound to the next year
      if (currentWeek == 52 && ISOWeek.GetYear(today) == today.Year)
      {
        currentWeek = 1;  // Move to week 1 of the next year
        today = new DateTime(today.Year + 1, 1, 1);  // Move to next year
      }
      else
      {
        currentWeek++;  // Move to the next week
      }
    }

    // Get the year associated with the current ISO week number
    var weekYear = ISOWeek.GetYear(today);

    // Get the Monday and Friday of the calculated week
    var monday = ISOWeek.ToDateTime(weekYear, currentWeek, DayOfWeek.Monday);
    var friday = ISOWeek.ToDateTime(weekYear, currentWeek, DayOfWeek.Friday);

    // Format the start and end dates (Monday and Friday) to "dd MMM"
    var startDate = monday.ToString("dd MMM", CultureInfo.InvariantCulture);
    var endDate = friday.ToString("dd MMM", CultureInfo.InvariantCulture);

    return ($"{startDate} - {endDate}", currentWeek);
  }
  public static DateTime ParseDayAndTime(string input)
  {
    var parts = input.Split(", ");
    var dayOfWeek = Enum.Parse<DayOfWeek>(parts[0]);
    var timeParts = parts[1].Split(':');
    var hour = int.Parse(timeParts[0]);
    var minute = int.Parse(timeParts[1]);

    var today = DateTime.Today;
    var daysUntilNext = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
    var targetDate = today.AddDays(daysUntilNext);

    return new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, hour, minute, 0);
  }
}
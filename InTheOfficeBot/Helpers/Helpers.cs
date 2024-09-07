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

    if (today.DayOfWeek >= DayOfWeek.Friday || today.DayOfWeek == DayOfWeek.Sunday)
    {
      currentWeek++;
    }

    var monday = GetDateForSpecificDayOfWeek(today.Year, currentWeek, DayOfWeek.Monday);
    var friday = GetDateForSpecificDayOfWeek(today.Year, currentWeek, DayOfWeek.Friday);

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

  public static DateTime GetDateForSpecificDayOfWeek(int year, int weekOfYear, DayOfWeek dayOfWeek)
  {
    // Get the first day of the year
    var firstDayOfYear = new DateTime(year, 1, 1);

    // Find the first Monday of the year
    var daysOffset = DayOfWeek.Monday - firstDayOfYear.DayOfWeek;
    var firstMonday = firstDayOfYear.AddDays(daysOffset);

    // ISO 8601 week starts with the first Monday that belongs to that year
    // If the first Monday is before the 1st of January, move to the next week
    if (firstMonday.Year < year)
    {
      firstMonday = firstMonday.AddDays(7);
    }

    // Calculate the date of the desired day of the specified week
    var desiredDate = firstMonday.AddDays((weekOfYear - 1) * 7 + (int)dayOfWeek - (int)DayOfWeek.Monday);

    return desiredDate;
  }
}
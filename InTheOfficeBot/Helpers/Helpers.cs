using System.Globalization;
namespace InTheOfficeBot.Helpers;

public static class Helpers
{
  public static (string, int) GetWeekOrNextWeek()
  {
    var today = DateTime.Today;
    var currentWeek = ISOWeek.GetWeekOfYear(today);

    if (today.DayOfWeek >= DayOfWeek.Friday)
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
    int hour = int.Parse(timeParts[0]);
    int minute = int.Parse(timeParts[1]);

    DateTime today = DateTime.Today;
    int daysUntilNext = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
    DateTime targetDate = today.AddDays(daysUntilNext);

    return new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, hour, minute, 0);
  }
  internal static bool SendNow(DateTime sendDateTime)
  {
    return true;
  }

  public static DateTime GetDateForSpecificDayOfWeek(int year, int weekOfYear, DayOfWeek dayOfWeek)
  {
    // Get the first day of the year
    DateTime firstDayOfYear = new DateTime(year, 1, 1);

    // Find the first Monday of the year
    int daysOffset = DayOfWeek.Monday - firstDayOfYear.DayOfWeek;
    DateTime firstMonday = firstDayOfYear.AddDays(daysOffset);

    // ISO 8601 week starts with the first Monday that belongs to that year
    // If the first Monday is before the 1st of January, move to the next week
    if (firstMonday.Year < year)
    {
      firstMonday = firstMonday.AddDays(7);
    }

    // Calculate the date of the desired day of the specified week
    DateTime desiredDate = firstMonday.AddDays((weekOfYear - 1) * 7 + (int)dayOfWeek - (int)DayOfWeek.Monday);

    return desiredDate;
  }
}
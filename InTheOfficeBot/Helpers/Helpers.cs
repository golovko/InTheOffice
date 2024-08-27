using System.Globalization;
namespace InTheOfficeBot.Helpers;

public static class Helpers
{
  public static (string, int) GetNextWeek()
  {
    DateTime today = DateTime.Today;
    int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
    DateTime nextMonday = today.AddDays(daysUntilMonday);
    DateTime nextFriday = nextMonday.AddDays(4);

    string startDate = nextMonday.ToString("dd MMM", CultureInfo.InvariantCulture);
    string endDate = nextFriday.ToString("dd MMM", CultureInfo.InvariantCulture);

    return ($"{startDate} - {endDate}", ISOWeek.GetWeekOfYear(nextMonday) + 1);
  }

  public static DateTime ParseDayAndTime(string input)
  {
    string format = "dddd, HH:mm";

    if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedTime))
    {
      DateTime today = DateTime.Today;
      int daysUntilNext = ((int)parsedTime.DayOfWeek - (int)today.DayOfWeek + 7) % 7;
      
      DateTime targetDate = today.AddDays(daysUntilNext);

      return new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, parsedTime.Hour, parsedTime.Minute, 0);
    }
    else
    {
      throw new FormatException("Input string was not in the correct format.");
    }
  }
}
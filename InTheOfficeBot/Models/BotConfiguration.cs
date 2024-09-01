namespace InTheOfficeBot.Models;

public class BotConfiguration
{
    public string BotToken { get; set; } = default!;
    public DateTime SendDateTime { get; set; } = default!;
    public int Interval { get; set; }
}

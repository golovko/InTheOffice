using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InTheOfficeBot.Models;

public record Chat
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public long ChatId { get; set; }
    public string? ChatName { get; set; }
    public DateTime LastSuccessfullySentPoll { get; set; }
    public DayOfWeek SendPollOnDayOfWeek { get; set; }
    public TimeOnly SendPollAt { get; set; }
    public bool IsStopped { get; set; }
    public List<int>? AdminIds { get; private set; }
    public bool PinLatestPoll { get; set; }

    public void AddAdmin(User user)
    {
        AdminIds ??= new List<int>();
        if (AdminIds.Contains(user.Id))
        {
            return;
        }
        AdminIds.Add(user.Id);
    }
}
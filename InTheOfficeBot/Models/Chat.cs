using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InTheOfficeBot.Models;

public class Chat
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public long ChatId { get; set; }
    public DateTime LastSuccessfullySentPoll { get; set; }
    public DayOfWeek SendPollOnDayOfWeek { get; set; }
    public TimeOnly SendPollAt { get; set; }
}
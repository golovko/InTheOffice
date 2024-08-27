using InTheOfficeBot.Models;
namespace InTheOfficeBot;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private readonly Bot _bot;

    public Worker(ILogger<Worker> logger, BotConfiguration botConfiguration)
    {
       this._logger = logger;
       this._bot = new Bot(logger, botConfiguration);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this._bot.BotAction();
    }

}
using InTheOfficeBot.Models;
namespace InTheOfficeBot;

public class Worker : BackgroundService
{
    private readonly TimeSpan _interval;
    private readonly ILogger<Worker> _logger;
    private readonly Bot _bot;
    private readonly BotConfiguration _botConfiguration;

    public Worker(ILogger<Worker> logger, BotConfiguration botConfiguration)
    {
        _logger = logger;
        _botConfiguration = botConfiguration;
        _bot = new Bot(logger, botConfiguration);
        _interval = botConfiguration.Interval > 0 
            ? TimeSpan.FromDays(botConfiguration.Interval) 
            : TimeSpan.FromDays(7);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bot service is starting.");

        await _bot.BotAction();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (Helpers.Helpers.IsCurrentDayAndTime(_botConfiguration.SendDateTime))
                {
                    await _bot.SendPoll();

                    _botConfiguration.SendDateTime = DateTime.Now + _interval;

                    await Task.Delay(_interval, stoppingToken);
                }
                else
                {
                    var delayTime = _botConfiguration.SendDateTime - DateTime.Now;
                    await Task.Delay(delayTime, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Bot service is stopping due to cancellation.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the bot.");
            }
        }
        _logger.LogInformation("Bot service has stopped.");
    }
}
using InTheOfficeBot.Models;
using InTheOfficeBot.Repository;
namespace InTheOfficeBot;

public class Worker : BackgroundService
{
    private readonly TimeSpan _interval;
    private readonly ILogger<Worker> _logger;
    private Bot _bot;
    private readonly BotConfiguration _botConfiguration;

    public Worker(ILogger<Worker> logger, BotConfiguration botConfiguration, Repository.Repository repo)
    {
        _logger = logger;
        _botConfiguration = botConfiguration;
        _interval = botConfiguration.Interval > 0
            ? TimeSpan.FromDays(botConfiguration.Interval)
            : TimeSpan.FromDays(7);
        _bot = new Bot(_logger, _botConfiguration, repo);
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
                    _logger.LogInformation("Sending poll...");
                    await _bot.SendPoll();

                    _botConfiguration.SendDateTime += _interval;

                    await Task.Delay(_interval, stoppingToken);
                }
                else
                {
                    var delayTime = _botConfiguration.SendDateTime - DateTime.Now;

                    if (delayTime < TimeSpan.Zero)
                    {
                        delayTime = TimeSpan.FromMinutes(1);
                    }

                    await Task.Delay(TimeSpan.FromMinutes(Math.Min(1, delayTime.TotalMinutes)), stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Bot service is stopping due to cancellation.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the bot service.");
            }
        }

        _logger.LogInformation("Bot service has stopped.");
    }
}
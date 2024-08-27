using InTheOfficeBot;
using InTheOfficeBot.Helpers;
using InTheOfficeBot.Models;

var builder = Host.CreateApplicationBuilder(args);

var botConfiguration = new BotConfiguration{
    BotToken = builder.Configuration["BotConfiguration:BotToken"]!,
    SendDateTime = Helpers.ParseDayAndTime(builder.Configuration["BotConfiguration:SendDateTime"]!),
};
builder.Services.AddSingleton(botConfiguration);
builder.Services.AddHostedService<Worker>(serviceProvider =>
{
  var logger = serviceProvider.GetRequiredService<ILogger<Worker>>();
  var config = serviceProvider.GetRequiredService<BotConfiguration>(); 

  return new Worker(logger, config);
});
var host = builder.Build();
host.Run();

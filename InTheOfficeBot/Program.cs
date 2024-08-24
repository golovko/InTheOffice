using InTheOfficeBot;
using InTheOfficeBot.Repository;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
var botToken = builder.Configuration["BotToken"]!;
builder.Services.AddSingleton(botToken);
builder.Services.AddDbContext<Context>(options =>
    options.UseSqlite("Data Source=botdatabase.db"));
builder.Services.AddHostedService<Worker>(serviceProvider =>
{
  var token = serviceProvider.GetRequiredService<string>();
  var logger = serviceProvider.GetRequiredService<ILogger<Worker>>();
  return new Worker(logger, token);
});
var host = builder.Build();
host.Run();

using InTheOfficeBot;
using InTheOfficeBot.Helpers;
using InTheOfficeBot.Models;
using InTheOfficeBot.Repository;

var builder = Host.CreateApplicationBuilder(args);

var botConfiguration = new BotConfiguration
{
  BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN") 
               ?? builder.Configuration["BotConfiguration:BotToken"]!,
  SendDateTime = Helpers.ParseDayAndTime(builder.Configuration["BotConfiguration:SendDateTime"]!),
};
builder.Services.AddSingleton(botConfiguration);
builder.Services.AddTransient<SqLiteContext>();
builder.Services.AddTransient<AnswersRepository>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
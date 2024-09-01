# In The Office Bot

In The Office Bot is a Telegram bot designed to help teams manage who will be in the office on specific days. This can be especially useful for coordinating hybrid work schedules or organizing office attendance.

## Features

- **Polls for Office Attendance:** The bot sends polls to team members to gather information on who will be in the office on each day of the week.
- **Real-Time Updates:** Users can update their responses, and the bot will track changes, providing up-to-date information on office attendance.
- **Customizable Poll Schedule:** The bot can be configured to send out polls on specific days and times.
- **View Attendance Summary:** The bot provides a summary of who will be in the office on each day, making it easy for teams to plan accordingly.

## Setup and Installation

### 1. Register Your Bot with BotFather

To use this bot, you need to register a new bot on Telegram via BotFather:

1. Open a chat with [BotFather](https://core.telegram.org/bots#botfather) on Telegram.
2. Use the `/newbot` command to create a new bot.
3. Follow the instructions to get your bot token.

### 2. Configure the Bot

Once you have your bot token, you'll need to add it to the bot's configuration file.

- Create an `appsettings.json` file in your project directory if it doesn't already exist.
- Add your bot token to the configuration file:

```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "SendDateTime": "Tuesday, 11:00",
    "Interval": 7
  }
}
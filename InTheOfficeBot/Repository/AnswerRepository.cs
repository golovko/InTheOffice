using System.Text.Json;
using InTheOfficeBot.Models;

namespace InTheOfficeBot.Repository;
class AnswerRepository : IRepository
{
  private readonly string _folderPath;

  public AnswerRepository(string folderPath)
  {
    _folderPath = folderPath;
  }

  public IEnumerable<Answer> GetAnswersByUser(long chatId, long userId)
  {
    throw new NotImplementedException();
  }

  public IEnumerable<Answer> GetAnswersByWeek(long chatId, int weekOfTheYear)
  {
    List<Answer> results = new List<Answer>();

    try
    {
      // Enumerate all JSON files in the folder
      var files = Directory.EnumerateFiles(_folderPath, "*.json");

      foreach (var file in files)
      {
        // Read and deserialize the JSON file
        string json = File.ReadAllText(file);
        var answers = JsonSerializer.Deserialize<List<Answer>>(json);

        if (answers != null)
        {
          // Filter the answers by chatId and weekOfTheYear
          var matchingAnswers = answers.Where(a => a.ChatId == chatId && a.WeekOfTheYear == weekOfTheYear);
          results.AddRange(matchingAnswers);
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error reading JSON files: {ex.Message}");
    }

    return results;
  }

  public void SaveAnswer(Answer answer)
  {
    string json = JsonSerializer.Serialize(new List<Answer>() { answer }, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
    var files = Directory.EnumerateFiles(_folderPath, answer.ChatId + ".json");
    if(files is null){
      
    Helper.SaveJsonToFile(json, answer.ChatId.ToString() + ".json");
    }
  }
}
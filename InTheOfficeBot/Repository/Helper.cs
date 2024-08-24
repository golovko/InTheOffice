using System.Text.Json;
using InTheOfficeBot.Models;

namespace InTheOfficeBot.Repository;
static class Helper
{
  public static void SaveJsonToFile(string json, string filePath)
  {
    try
    {
      File.WriteAllText(filePath, json);
      Console.WriteLine($"JSON saved to {filePath} successfully.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error saving JSON to file: {ex.Message}");
    }
  }

  public static string ReadJsonFromFile(string filePath)
  {
    try
    {
      string json = File.ReadAllText(filePath);
      Console.WriteLine($"JSON read from {filePath} successfully.");
      return json;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error reading JSON from file: {ex.Message}");
      return null;
    }
  }

  public static void UpdateJsonFile<T>(string filePath, Func<List<T>, List<T>> updateAction) where T : Answer
  {
    try
    {
      // Read the existing JSON data
      string json = File.Exists(filePath) ? File.ReadAllText(filePath) : "[]";

      // Deserialize the JSON into a list of objects
      List<T> items = JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();

      // Update the list using the provided action
      items = updateAction(items);

      // Serialize the updated list back to JSON
      string updatedJson = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });

      // Write the updated JSON back to the file
      File.WriteAllText(filePath, updatedJson);

      Console.WriteLine($"JSON file at {filePath} updated successfully.");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error updating JSON file: {ex.Message}");
    }
  }
}
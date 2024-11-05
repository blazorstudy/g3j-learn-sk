using System.Text.Json;

public class Settings
{
    public string AzureOpenAIEndpoint { get; set; }
    public string AzureOpenAIApiKey { get; set; }
    public string GoogleGeminiKey { get; set; }
    public string HuggingFacePhiApiKey { get; set; }

    public static Settings LoadFromFile()
    {
        string filePath = "config/AppSetting.json";
        string json = System.IO.File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Settings>(json);
    }


}
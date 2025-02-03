#pragma warning disable SKEXP0001, SKEXP0050
// See https://aka.ms/new-console-template for more information
using System.Threading.Tasks;
using demo02;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.Web.Google;

//dotnet add package Microsoft.SemanticKernel
//dotnet add package Microsoft.SemanticKernel.Plugins.Web // preview

Console.WriteLine("Welcome, G3J!\n");

#region Configuration
IConfiguration config = new ConfigurationBuilder()
																.AddUserSecrets<Program>()
																.Build();

string deployName = config["AOAI:DeployName"];
string endPoint = config["AOAI:EndPoint"];
string key = config["AOAI:Key"];
string model = config["AOAI:Model"];

string searchEngineID = config["google:searchEngineId"];
string googleApiKey = config["google:apiKey"];

string bingKey = config["bing:apiKey"];
#endregion

string query = "G3J가 뭐에요?";

TextSearch search = new();

var task1 = search.demo01(searchEngineID, googleApiKey, query);
task1.Wait();

//var task2 = search.demo02(deployName, endPoint, key, model, searchEngineID, googleApiKey, query);
//task2.Wait();


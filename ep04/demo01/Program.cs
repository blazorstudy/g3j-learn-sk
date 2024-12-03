// See https://aka.ms/new-console-template for more information
using demo01;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;


#region Configuration
IConfiguration config = new ConfigurationBuilder()
                                .AddUserSecrets<Program>()
                                .Build();

string deployName = config["AOAI:DeployName"];
string endPoint = config["AOAI:EndPoint"];
string key = config["AOAI:Key"];
string model = config["AOAI:Model"];
#endregion

var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deployName, endPoint, key, model)
            .Build();

Console.WriteLine("Hello, Semantic Kernel!");

// 챗컴플리션???
// Chat Completion : Generate a response given a list of messages in a conversational context
// 주어진 메시지 목록(맥락이 있는)에서 답변을 생성하는 것

// Chat History
// System Message : 사용자가 변경할 수 없는 유용한 정보를 담는 곳. 페르소나, 그라운딩, 세이프티 메시지 등
// User Message : 사용자가 입력한 메시지
// Asisstant Message : LLM이 답변한 메시지

// 샘플 1 : 개요, 비스트리밍 챗 컴플리션
Console.WriteLine("\nSample1");
await Sample.showOverview(kernel);

// 샘플 2 : 챗히스토리 오프젝트, 스트리밍 챗 컴플리션
Console.WriteLine("\nSample2");
await Sample.showChatHistory(kernel);

// 샘플 3 : 페르소나
Console.WriteLine("\nSample3");
await Sample.showPersonas(kernel);

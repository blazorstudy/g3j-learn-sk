#pragma warning disable SKEXP0110
#pragma warning disable OPENAI001

using System.Text;

using OpenAIAssistantAgentDemo;

Console.OutputEncoding = Encoding.UTF8;

// OpenAI AssistantTool Code Interpreter
await CodeInterpreter.ExecuteAsync();

// OpenAI AssistantTool File Search
// await FileSearch.ExecuteAsync();
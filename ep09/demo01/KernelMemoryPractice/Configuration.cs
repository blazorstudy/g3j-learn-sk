using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KernelMemoryPractice
{
    public class Configuration
    {
        public static string AzureOpenAIEndpoint { get; set; }
        
        public static string AzureOpenAIApiKey { get; set; }

        public static string TextGenerationDeploymentName { get; set; } = "gpt-4o-mini";

        public static string TextEmbeddingDeploymentName { get; set; } = "text-embedding-3-large";
    }
}

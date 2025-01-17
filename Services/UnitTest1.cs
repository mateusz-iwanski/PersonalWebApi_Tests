using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalWebApi.Agent;
using System.Collections.Generic;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using PersonalWebApi.Tests.Controllers.Agent;
using System.Diagnostics.CodeAnalysis;

namespace PersonalWebApi.Tests.Services
{
    public class AgentTests
    {
        private readonly TestConfiguration _testConfig;

        [Experimental("SKEXP0050")]
        public AgentTests()
        {
            _testConfig = new TestConfiguration();
        }

        [Fact]
        public async Task Chat_ReturnsExpectedResult()
        {
            // Arrange
            string conversationUuid = "30f4373b-5b18-41fd-8b40-5953825b3c0d";
            int id = 1;
            var sessionId = Guid.NewGuid().ToString();

            // Ensure the file exists for the test
            string filePath = Path.Combine(AppContext.BaseDirectory, "bajka.docx");
            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, "This is a test document content.");
            }

            // Name of the plugin. This is the name you'll use in skPrompt, e.g. {{memory.ask ...}}
            var pluginName = "memory";

            TagCollection conversation_1_id_tags = new TagCollection();
            conversation_1_id_tags.Add("sessionUuid", Guid.NewGuid().ToString());

            var conversationId = Guid.Parse(conversationUuid);

            var memoryPlugin = _testConfig.Kernel.ImportPluginFromObject(
                new MemoryPlugin(_testConfig.Memory, waitForIngestionToComplete: true),
                pluginName);

            var skPrompt = """
                        Question to Memory: {{$input}}

                        Answer from Memory: {{memory.ask $input index=$index}}

                        If the answer is empty look forward. If you find answer say 'I haven't in memory but ai found the answer - <answer>' otherwise reply with a preview of the answer,
                        truncated to 15 words. Prefix with one emoji relevant to the content.
                        """;

            var f = new PromptExecutionSettings()
            {
                ExtensionData = new Dictionary<string, object>()
                {
                    { "conversationUuid", conversationUuid },
                    { "sessionUuid", sessionId },
                    { "shortMemory", true }
                }
            };

            OpenAIPromptExecutionSettings settings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };

            KernelArguments arguments = new KernelArguments(settings)
            {
                ["input"] = "Kto skręcił sobie nogę?",
                ["index"] = conversationId.ToString(),
                ["conversationUuid"] = conversationUuid,
                ["sessionUuid"] = sessionId
            };

            var prompts = _testConfig.Kernel.CreatePluginFromPromptDirectory(Path.Combine(AppContext.BaseDirectory, "../../../Agent/Prompts"));

            var chatResult = _testConfig.Kernel.InvokeStreamingAsync<StreamingChatMessageContent>(
              prompts["Complaint"],
              new()
              {
               { "request", "my pdf filter is faulty" },
               { "input" , "Kto skręcił sobie nogę?" },
               { "conversationUuid", conversationUuid },
               { "sessionUuid", sessionId }
              }
            );

            string message = "";

            await foreach (var chunk in chatResult)
            {
                if (chunk.Role.HasValue)
                {
                    Console.Write(chunk.Role + " > ");
                }
                message += chunk;
                Console.Write(chunk);
            }

            Console.WriteLine();

            var myFunction = _testConfig.Kernel.CreateFunctionFromPrompt(skPrompt, f);

            var answer = await myFunction.InvokeAsync(_testConfig.Kernel, arguments);

            // Assert
            Assert.NotNull(answer);
            Assert.True(answer.ToString().Length > 0);
            Assert.IsType<string>(answer.ToString());
        }
    }
}

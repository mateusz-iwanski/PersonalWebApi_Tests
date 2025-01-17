using Microsoft.Azure.ApplicationInsights.Query.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.Rest;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalWebApi.Tests.Controllers.Agent;
using PersonalWebApi.Utilities.Kql;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PersonalWebApi.Tests.Services.KernelMemory
{
    /// <summary>
    /// Comment PersonalWebApi.Services.Services.System.RetrieveCrucialUuid to use it
    /// or change uuid to conversationUuid which you want to access
    /// </summary>
    public class KernelMemoryBasicTests
    {
        private readonly TestConfiguration _testConfig;
        private readonly IConfiguration _configuration;

        [Experimental("SKEXP0050")]
        public KernelMemoryBasicTests()
        {
            _testConfig = new TestConfiguration();
            _configuration = _testConfig.Configuration;
        }

        /// <summary>
        /// Import a document to Kernel Memory and ask a question to memory.
        /// The answer from memory is used by assistant to reply to the user.
        /// Memory index is set on conversationUuid. Only read memory from this index.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ImportDocumentToMemory_AskQuestion_ReturnsAnswer()
        {
            // must be implemented
            var conversationUuid = Guid.NewGuid();
            var sessionUuid = Guid.NewGuid();

            var pluginName = "memory";

            // we will import to memory 
            string filePath = "https://personalblobstore.blob.core.windows.net/library/bajka.docx"; //Path.Combine(AppContext.BaseDirectory, "bajka.docx");

            // tag for documents
            // additional
            TagCollection tagForDocument = new TagCollection();

            // import document to memory
            // KernelMemoryWrapper.ImportDocumentAsync start
            await _testConfig.Memory.ImportDocumentAsync(
                filePath, 
                tags: tagForDocument, 
                index: conversationUuid.ToString(), 
                documentId: Guid.NewGuid().ToString()
                );

            // import plugin to the Kernel
            var memoryPlugin = _testConfig.Kernel.ImportPluginFromObject(
                new MemoryPlugin(_testConfig.Memory, waitForIngestionToComplete: true),
                pluginName);

            // prompt 
            var skPrompt = """
                        Question to Memory: {{$input}}

                        Answer from Memory: {{memory.ask $input index=$index}}

                        Just use answer from memory, if memory will not have data, don't use it.
                        """;

            // settings for the prompt
            // Extensions are additional data which will be saved in logs
            // sessionUuid - must exists
            var promptSettings = new PromptExecutionSettings()
            {
                ExtensionData = new Dictionary<string, object>()
                {
                    { "shortMemory", true },  // additional but important
                    
                }
            };

            // settings for the prompt execution
            OpenAIPromptExecutionSettings settings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };

            // INDEX - is a memory index, only read memory from this index
            // conversationUuid it's a index for memory
            KernelArguments arguments = new KernelArguments(settings)
            {
                ["input"] = "Kto skręcił sobie nogę?",
                ["index"] = conversationUuid.ToString(),  // MUST BE IMPLEMENTED
            };

            // RenderedPromptFilterHandler.OnPromptRenderAsync start
            var myFunction = _testConfig.Kernel.CreateFunctionFromPrompt(skPrompt, promptSettings);

            var answer = await myFunction.InvokeAsync(_testConfig.Kernel, arguments);

            Output.Write(answer.ToString());

            // Assert
            Xunit.Assert.NotNull(answer);
            Xunit.Assert.IsType<FunctionResult>(answer);
        }

        [Fact]
        public async Task ImportDocumentToMemoryAndSaveToBlobStorage()
        {
            // must be implemented
            var conversationUuid = Guid.NewGuid();
            var sessionUuid = Guid.NewGuid();

            var pluginName = "memory";

            // we will import to memory 
            string filePath = Path.Combine(AppContext.BaseDirectory, "bajka.docx");
            // "https://personalblobstore.blob.core.windows.net/library/bajka.docx"

            // import document to memory
            // KernelMemoryWrapper.ImportDocumentAsync start
            await _testConfig.Memory.ImportDocumentAsync(
                filePath,
                index: conversationUuid.ToString(),
                documentId: Guid.NewGuid().ToString()
                );

        }
    }
}

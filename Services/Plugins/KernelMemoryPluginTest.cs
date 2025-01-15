using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using PersonalWebApi.Agent.SemanticKernel.Plugins.StoragePlugins.AzureBlob;
using PersonalWebApi.Services.Azure;
using PersonalWebApi.Tests.Controllers.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PersonalWebApi.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using PersonalWebApi.Agent.Memory.Observability;
using PersonalWebApi.Agent.SemanticKernel.Plugins.KernelMemoryPlugin;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.KernelMemory;
using PersonalWebApi.Services.Services.History;
using NetTopologySuite.Utilities;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;

namespace PersonalWebApi.Tests.Services.Plugins
{
    /// <summary>
    /// Test class for KernelMemoryPlugin.
    /// </summary>
    public class KernelMemoryPluginTest
    {
        private readonly TestConfiguration _testConfig;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="KernelMemoryPluginTest"/> class.
        /// </summary>
        public KernelMemoryPluginTest()
        {
            _testConfig = new TestConfiguration();
            _configuration = _testConfig.Configuration;
        }

        /// <summary>
        /// Tests loading a blob file into kernel memory and querying the memory.
        /// 
        /// To work properly, the following configuration has add plugins:
        ///     kernelBuilder.Plugins.AddFromType<KernelMemoryPlugin>();
        ///     kernelBuilder.Plugins.AddFromType<AzureBlobPlugin>();
        /// ... add the plugins in TestConfiguration.cs.
        /// 
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task LoadBlobFileToMemoryAndAskQuestion()
        {
            // Initialize Kernel
            Kernel kernel = _testConfig.Kernel; //kernelBuilder.Build();

            // pluginName for memory, 
            var pluginName = "memory";
            var _memory = kernel.GetRequiredService<KernelMemoryWrapper>();

            // Import the MemoryPlugin into the kernel
            kernel.ImportPluginFromObject(
                new MemoryPlugin(_memory, waitForIngestionToComplete: true),
                pluginName);

            // Set up the OpenAI prompt execution settings
            OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

            // Invoke a prompt to (plugins raise):
            // 1. Retrieve a file URI from Azure Blob Storage with a name similar to 'bajka'.
            // 2. If the file is not found, list all files and find one with 'bajka' in its name.
            // 3. Load the found file into kernel memory.
            // 4. Query the kernel memory with the question "Who twisted their ankle?" and return the result.
            var result = await kernel.InvokePromptAsync(
                "Pobierz uri z blob storage pliku który nazywa się podobnie do bajka. Jeśli nie wiesz to wylistuj pliki znajdź coś z 'bajka' i wczytaj za pomoca plugin do kernel memory po uri/url, następnie zapytaj pamięci 'Kto skręcił sobie nogę?'",
                new(settings)
                );

            // Output the result
            Output.Write(result.ToString());
        }

        /// <summary>
        /// Tests importing a web page into kernel memory and querying the memory.
        /// 1. 
        ///     Html file
        ///     var url = "https://woblink.com/blog/wszystko-co-wiemy-o-nowym-wiedzminie/?srsltid=AfmBOorweKgrWkNL175p3WOlAdScaXQvd0QZGZ3KTfxh9kEfDNcnHyKE";
        ///     question - $"1.load page to the kernel memory {url}. 2. Zapytaj pamięci: kto napisał 'Wedźmin'",
        ///         answer - can't find
        /// 2. 
        ///     MD file
        ///     var url = "https://github.com/microsoft/kernel-memory/blob/main/README.md";
        ///     question - $"1.load page to the kernel memory {url}. 2. Ask memory: 'How KM is available?'.",
        ///         answer - find the answer
        ///         
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ImportWebPageToKernelMemoryAsync()
        {
            var url = "https://woblink.com/blog/wszystko-co-wiemy-o-nowym-wiedzminie/?srsltid=AfmBOorweKgrWkNL175p3WOlAdScaXQvd0QZGZ3KTfxh9kEfDNcnHyKE";

            // Initialize Kernel
            Kernel kernel = _testConfig.Kernel; //kernelBuilder.Build();

            // pluginName for memory, 
            var pluginName = "memory";
            var _memory = kernel.GetRequiredService<KernelMemoryWrapper>();

            // Import the MemoryPlugin into the kernel
            kernel.ImportPluginFromObject(
                new MemoryPlugin(_memory, waitForIngestionToComplete: true),
                pluginName);

            // Set up the OpenAI prompt execution settings
            OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

            // Invoke a prompt to (plugins raise):
            // 1. Retrieve a file URI from Azure Blob Storage with a name similar to 'bajka'.
            // 2. If the file is not found, list all files and find one with 'bajka' in its name.
            // 3. Load the found file into kernel memory.
            // 4. Query the kernel memory with the question "Who twisted their ankle?" and return the result.
            var result = await kernel.InvokePromptAsync(
                $"1.load page to the kernel memory {url}. 2. Ask memory: 'kto jest autorem 'Wedźmin'.",
                new(settings)
                );

            // Output the result
            Output.Write(result.ToString());
        }

        [Fact]
        public async Task ImportWebPageByFirecrawlAsMdToKernelMemoryAsyncandAsk()
        {
            var url = "https://woblink.com/blog/wszystko-co-wiemy-o-nowym-wiedzminie/?srsltid=AfmBOorweKgrWkNL175p3WOlAdScaXQvd0QZGZ3KTfxh9kEfDNcnHyKE";

            // Initialize Kernel
            Kernel kernel = _testConfig.Kernel; //kernelBuilder.Build();

            // pluginName for memory, 
            var pluginName = "memory";
            var _memory = kernel.GetRequiredService<KernelMemoryWrapper>();

            // Import the MemoryPlugin into the kernel
            kernel.ImportPluginFromObject(
                new MemoryPlugin(_memory, waitForIngestionToComplete: true),
                pluginName);

            // Set up the OpenAI prompt execution settings
            OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

            // Invoke a prompt to (plugins raise):
            // 1. Retrieve a file URI from Azure Blob Storage with a name similar to 'bajka'.
            // 2. If the file is not found, list all files and find one with 'bajka' in its name.
            // 3. Load the found file into kernel memory.
            // 4. Query the kernel memory with the question "Who twisted their ankle?" and return the result.
            var result = await kernel.InvokePromptAsync(
                $"1.Scrap page and import text to the kernel memory. 2. Ask memory: 'kto jest autorem 'Wedźmin'.",
                new(settings)
                );

            // Output the result
            Output.Write(result.ToString());
        }

        //[Fact]
        //public async Task LoadBlobFromOutsideUrlToMemory_uploadToBlobStorage_AndAskQuestion()
        //{
        //    // Initialize Kernel
        //    Kernel kernel = _testConfig.Kernel; //kernelBuilder.Build();

        //    // pluginName for memory, 
        //    var pluginName = "memory";
        //    var _memory = kernel.GetRequiredService<KernelMemoryWrapper>();

        //    // Import the MemoryPlugin into the kernel
        //    kernel.ImportPluginFromObject(
        //        new MemoryPlugin(_memory, waitForIngestionToComplete: true),
        //        pluginName);

        //    // Set up the OpenAI prompt execution settings
        //    OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

        //    var result = await kernel.InvokePromptAsync(
        //        "Pobierz uri z blob storage pliku który nazywa się podobnie do bajka. Jeśli nie wiesz to wylistuj pliki znajdź coś z 'bajka' i wczytaj za pomoca plugin do kernel memory po uri/url, następnie zapytaj pamięci 'Kto skręcił sobie nogę?'",
        //        new(settings)
        //        );

        //    // Output the result
        //    Output.Write(result.ToString());
        //}

        // write for import ImportWebPageToKernelMemoryAsync

    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalWebApi.Agent.Memory.Observability;
using PersonalWebApi.Agent.SemanticKernel.Plugins.KernelMemoryPlugin;
using PersonalWebApi.Agent.SemanticKernel.Plugins.StoragePlugins.AzureBlob;
using PersonalWebApi.Exceptions;
using PersonalWebApi.Services.FileStorage;
using PersonalWebApi.Services.NoSQLDB;
using PersonalWebApi.Services.Services.History;
using PersonalWebApi.Tests.Controllers.Agent;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalWebApi.Tests.Services.Plugins
{
    public class AzurePluginsTest
    {
        private readonly TestConfiguration _testConfig;
        private readonly IConfiguration _configuration;

        [Experimental("SKEXP0050")]
        public AzurePluginsTest()
        {
            _testConfig = new TestConfiguration();
            _configuration = _testConfig.Configuration;
        }

        [Fact]
        public async Task AzureBlobPlugin_downloadFile()
        {
            // kernel settings

            var apiKey = _configuration.GetSection("OpenAI:Access:ApiKey").Value ??
                    throw new SettingsException("OpenAi ApiKey not exists in appsettings");

            var defaultModelId = _configuration.GetSection("OpenAI:DefaultModelId").Value ??
                throw new SettingsException("OpenAi DefaultModelId not exists in appsettings");

            // Create a kernel with OpenAI chat completion
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddOpenAIChatCompletion(
                    modelId: defaultModelId,
                    apiKey: apiKey
                    );

            var configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.Nlog.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.OpenAi.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.Telemetry.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.Qdrant.json", optional: true, reloadOnChange: true)
               .AddJsonFile("appsettings.Azure.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<Program>()
               .AddEnvironmentVariables()
               .Build();

            kernelBuilder.Services.AddHttpContextAccessor();

            // DI
            kernelBuilder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
            kernelBuilder.Services.AddSingleton<IConfiguration>(configuration);

            IKernelMemory memory = new KernelMemoryBuilder()
                  .WithOpenAIDefaults(apiKey)
                  .Build<MemoryServerless>();

            kernelBuilder.Services.AddScoped<IKernelMemory>(_ => memory);
            kernelBuilder.Services.AddScoped<IAssistantHistoryManager, AssistantHistoryManager>();
            kernelBuilder.Services.AddScoped<INoSqlDbService, AzureCosmosDbService>();

            kernelBuilder.Services.AddScoped<KernelMemoryWrapper>(provider =>
            {
                var innerKernelMemory = provider.GetRequiredService<IKernelMemory>();
                var assistantHistoryManager = provider.GetRequiredService<IAssistantHistoryManager>();
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var blolStorageConnector = provider.GetRequiredService<IFileStorageService>();

                return new KernelMemoryWrapper(innerKernelMemory, assistantHistoryManager, httpContextAccessor, blolStorageConnector);
            });

            // add plugin

            kernelBuilder.Plugins.AddFromType<AzureBlobPlugin>();
            kernelBuilder.Plugins.AddFromType<KernelMemoryPlugin>();
            Kernel kernel = kernelBuilder.Build();

            // without plugin
            var result = await kernel.InvokePromptAsync("Pokaż jakie kontenery są na blob azure?");

            Output.Write(result.ToString());

            // with plugin
            OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
            var resultPlugin = await kernel.InvokePromptAsync("Pokaż jakie kontenery są na blob azure?", new(settings));
            Output.Write(resultPlugin.ToString());



            //var resultPluginfilelist = await kernel.InvokePromptAsync("Pokaż jakie są pliki na blob azure?", new(settings));
            //Output.Write(resultPluginfilelist.ToString());

            var resultPluginfilelist = await kernel.InvokePromptAsync("Znajdź na blob azure plik, nazywa się bajka, znajdź uri/url i tak wczytaj go do pamięci kernel, zapytaj pamięć kto złamał nogę?", new(settings));
            Output.Write(resultPluginfilelist.ToString());



        }
    }
}

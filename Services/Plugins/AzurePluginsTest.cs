using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalWebApi.Agent.SemanticKernel.Plugins.StoragePlugins.AzureBlob;
using PersonalWebApi.Exceptions;
using PersonalWebApi.Services.Azure;
using PersonalWebApi.Tests.Controllers.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalWebApi.Tests.Services.Plugins
{
    public class AzurePluginsTest
    {
        private readonly TestConfiguration _testConfig;
        private readonly IConfiguration _configuration;
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

            // DI
            kernelBuilder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
            kernelBuilder.Services.AddSingleton<IConfiguration>(configuration);

            // add plugin

            kernelBuilder.Plugins.AddFromType<AzureBlobPlugin>();
            Kernel kernel = kernelBuilder.Build();

            // without plugin
            var result = await kernel.InvokePromptAsync("Pokaż jakie kontenery są na blob azure?");

            Output.Write(result.ToString());

            // with plugin
            OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
            var resultPlugin = await kernel.InvokePromptAsync("Pokaż jakie kontenery są na blob azure?", new(settings));

            Output.Write(resultPlugin.ToString());
        }
    }
}

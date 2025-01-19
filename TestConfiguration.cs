using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalWebApi.Utilities.Utilities.DocumentReaders;
using PersonalWebApi.Services.Services.Qdrant;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using PersonalWebApi.Agent.SemanticKernel.Observability;
using PersonalWebApi.Exceptions;
using PersonalWebApi.Services.Services.Agent;
using PersonalWebApi.Services.System;
using PersonalWebApi.Utilities.Utilities.Qdrant;
using PersonalWebApi.Extensions;
using Microsoft.AspNetCore.Identity;
using PersonalWebApi.Utilities.Utilities.HttUtils;
using Microsoft.KernelMemory;
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Google.Protobuf.WellKnownTypes;
using PersonalWebApi.Services.Services.History;
using PersonalWebApi.Agent.Memory.Observability;
using PersonalWebApi.Agent.SemanticKernel.Plugins.KernelMemoryPlugin;
using PersonalWebApi.Agent.SemanticKernel.Plugins.StoragePlugins.AzureBlob;
using PersonalWebApi.Utilities.WebScrapper;
using PersonalWebApi.Utilities.WebScrappers;
using PersonalWebApi.Services.WebScrapper;
using PersonalWebApi.Services.FileStorage;
using PersonalWebApi.Services.NoSQLDB;
using PersonalWebApi.Services.Agent;
using System.Diagnostics.CodeAnalysis;
using PersonalWebApi.Agent.SemanticKernel.Plugins.DataGathererPlugin;

namespace PersonalWebApi.Tests.Controllers.Agent
{
    public class TestConfiguration
    {
        public ServiceProvider ServiceProvider { get; private set; }
        public Kernel Kernel { get; private set; }
        public KernelMemoryWrapper Memory { get; private set; }
        public IFileStorageService BlobStorage { get; private set; }
        public IDocumentReaderDocx DocumentReaderDocx { get; private set; }
        public IQdrantService Qdrant { get; private set; }
        public IConfiguration Configuration { get; private set; }
        public IAssistantHistoryManager AssistantHistoryManager { get; private set; }

        [Experimental("SKEXP0050")] //s semantic text chunker
        public TestConfiguration()
        {
            // Set up the service collection
            var serviceCollection = new ServiceCollection();

            // Add HttpContextAccessor and Session
            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddSession();

            serviceCollection.AddSingleton<IPersistentChatHistoryService, PersistentChatHistoryService>();

            // Add RequestDelegate factory
            serviceCollection.AddSingleton<RequestDelegate>(sp =>
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                return async context =>
                {
                    var httpContext = httpContextAccessor.HttpContext;
                    if (httpContext != null)
                    {
                        await context.Response.WriteAsync("RequestDelegate is working.");
                    }
                };
            });

            // Add SessionMiddleware
            serviceCollection.AddTransient<SessionMiddleware>();

            // Add configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Nlog.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.OpenAi.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Telemetry.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Qdrant.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Azure.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.FileStorage.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();
            serviceCollection.AddSingleton<IConfiguration>(configuration);

            // Manually register services
            serviceCollection.AddScoped<Kernel>(sp =>
            {
            var kernelBuilder = Kernel.CreateBuilder();

            var apiKey = configuration.GetSection("OpenAI:Access:ApiKey").Value ??
                throw new SettingsException("OpenAi ApiKey not exists in appsettings");

            var defaultModelId = configuration.GetSection("OpenAI:DefaultModelId").Value ??
                throw new SettingsException("OpenAi DefaultModelId not exists in appsettings");

            kernelBuilder.AddOpenAIChatCompletion(
                defaultModelId,
                apiKey
            );

            // Use the correct method to add logging
            kernelBuilder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
            });

            kernelBuilder.Services.AddSingleton<IConfiguration>(configuration);

            // Register IHttpContextAccessor early
            kernelBuilder.Services.AddHttpContextAccessor();


            kernelBuilder.Services.AddScoped<IPersistentChatHistoryService, PersistentChatHistoryService>();
            kernelBuilder.Services.AddScoped<INoSqlDbService, AzureCosmosDbService>();
            kernelBuilder.Services.AddScoped<IAssistantHistoryManager, AssistantHistoryManager>();
            kernelBuilder.Services.AddScoped<IPromptRenderFilter, RenderedPromptFilterHandler>();
            kernelBuilder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
            kernelBuilder.Services.AddScoped<IDocumentReaderDocx, DocumentReaderDocx>();
            kernelBuilder.Services.AddScoped<IWebScrapperClient, Firecrawl>();
            kernelBuilder.Services.AddScoped<IWebScrapperService, WebScrapperService>();
            kernelBuilder.Services.AddScoped<ITextChunker, SemanticKernelTextChunker>();
            kernelBuilder.Services.AddScoped<IQdrantService, QdrantService>();

            IKernelMemory memory = new KernelMemoryBuilder()
                .WithOpenAIDefaults(apiKey)
                .Build<MemoryServerless>();

            kernelBuilder.Services.AddScoped<IKernelMemory>(_ => memory);
            kernelBuilder.Services.AddScoped<KernelMemoryWrapper>(provider =>
            {
                var innerKernelMemory = provider.GetRequiredService<IKernelMemory>();
                var assistantHistoryManager = provider.GetRequiredService<IAssistantHistoryManager>();
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var blobStorageConnector = provider.GetRequiredService<IFileStorageService>();

                return new KernelMemoryWrapper(innerKernelMemory, assistantHistoryManager, httpContextAccessor, blobStorageConnector);
            });

            // add plugin
            //kernelBuilder.Plugins.AddFromType<KernelMemoryPlugin>();
            //kernelBuilder.Plugins.AddFromType<AzureBlobPlugin>();
            //kernelBuilder.Plugins.AddFromType<TagCollectorPlugin>();

                return kernelBuilder.Build();
            });

            serviceCollection.AddScoped<IKernelMemory>(sp =>
            {
                var apiKey = configuration.GetSection("OpenAI:Access:ApiKey").Value ?? throw new SettingsException("OpenAi ApiKey not exists in appsettings");
                return new KernelMemoryBuilder().WithOpenAIDefaults(apiKey).Build<MemoryServerless>();
            });

            serviceCollection.AddScoped<IAssistantHistoryManager, AssistantHistoryManager>();

            serviceCollection.AddScoped<KernelMemoryWrapper>(provider =>
            {
                var innerKernelMemory = provider.GetRequiredService<IKernelMemory>();
                var assistantHistoryManager = provider.GetRequiredService<IAssistantHistoryManager>();
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var blobStorageConnector = provider.GetRequiredService<IFileStorageService>();

                return new KernelMemoryWrapper(innerKernelMemory, assistantHistoryManager, httpContextAccessor, blobStorageConnector);
            });

            // Register other necessary services
            serviceCollection.AddScoped<IQdrantService, QdrantService>();
            serviceCollection.AddScoped<IAccountService, AccountService>();
            serviceCollection.AddScoped<IFileStorageService, AzureBlobStorageService>();
            serviceCollection.AddScoped<IDocumentReaderDocx, DocumentReaderDocx>();
            serviceCollection.AddScoped<IEmbedding, EmbeddingOpenAi>();
            serviceCollection.AddScoped<INoSqlDbService, AzureCosmosDbService>();
            serviceCollection.AddScoped<IApiClient, ApiClient>();

            // Build the service provider
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Resolve the dependencies
            Kernel = ServiceProvider.GetRequiredService<Kernel>();
            Memory = ServiceProvider.GetRequiredService<KernelMemoryWrapper>();
            BlobStorage = ServiceProvider.GetRequiredService<IFileStorageService>();
            DocumentReaderDocx = ServiceProvider.GetRequiredService<IDocumentReaderDocx>();
            Qdrant = ServiceProvider.GetRequiredService<IQdrantService>();
            Configuration = ServiceProvider.GetRequiredService<IConfiguration>();
            AssistantHistoryManager = ServiceProvider.GetRequiredService<IAssistantHistoryManager>();

            // Initialize HttpContext
            var context = new DefaultHttpContext();
            ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
        }
    }
}

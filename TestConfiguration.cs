using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalWebApi.Agent.MicrosoftKernelMemory;
using PersonalWebApi.Services.Azure;
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
using PersonalWebApi.Agent;
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Google.Protobuf.WellKnownTypes;

namespace PersonalWebApi.Tests.Controllers.Agent
{
    public class TestConfiguration
    {
        public ServiceProvider ServiceProvider { get; private set; }
        public Kernel Kernel { get; private set; }
        public KernelMemoryWrapper Memory { get; private set; }
        public IBlobStorageService BlobStorage { get; private set; }
        public IDocumentReaderDocx DocumentReaderDocx { get; private set; }
        public IQdrantFileService Qdrant { get; private set; }
        public IConfiguration Configuration { get; private set; }
        public IAssistantHistoryManager AssistantHistoryManager { get; private set; }

        public TestConfiguration()
        {
            // Set up the service collection
            var serviceCollection = new ServiceCollection();

            // Add HttpContextAccessor and Session
            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddSession();

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

                kernelBuilder.Services.AddScoped<ICosmosDbService, AzureCosmosDbService>();
                kernelBuilder.Services.AddScoped<IAssistantHistoryManager, AssistantHistoryManager>();
                kernelBuilder.Services.AddScoped<IPromptRenderFilter, RenderedPromptFilterHandler>();

                IKernelMemory memory = new KernelMemoryBuilder()
                    .WithOpenAIDefaults(apiKey)
                    .Build<MemoryServerless>();

                kernelBuilder.Services.AddScoped<IKernelMemory>(_ => memory);

                serviceCollection.AddScoped<KernelMemoryWrapper>(provider =>
                {
                    var innerKernelMemory = provider.GetRequiredService<IKernelMemory>();
                    var assistantHistoryManager = provider.GetRequiredService<IAssistantHistoryManager>();
                    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();

                    return new KernelMemoryWrapper(innerKernelMemory, assistantHistoryManager, httpContextAccessor);
                });

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

                return new KernelMemoryWrapper(innerKernelMemory, assistantHistoryManager, httpContextAccessor);
            });

            // Register other necessary services
            serviceCollection.AddScoped<IAccountService, AccountService>();
            serviceCollection.AddScoped<IBlobStorageService, AzureBlobStorageService>();
            serviceCollection.AddScoped<IDocumentReaderDocx, DocumentReaderDocx>();
            serviceCollection.AddScoped<IQdrantFileService, QdrantFileService>();
            serviceCollection.AddScoped<QdrantRestApiClient>();
            serviceCollection.AddScoped<IEmbedding, EmbeddingOpenAi>();
            serviceCollection.AddScoped<ICosmosDbService, AzureCosmosDbService>();
            serviceCollection.AddScoped<IApiClient, ApiClient>();

            // Build the service provider
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Resolve the dependencies
            Kernel = ServiceProvider.GetRequiredService<Kernel>();
            Memory = ServiceProvider.GetRequiredService<KernelMemoryWrapper>();
            BlobStorage = ServiceProvider.GetRequiredService<IBlobStorageService>();
            DocumentReaderDocx = ServiceProvider.GetRequiredService<IDocumentReaderDocx>();
            Qdrant = ServiceProvider.GetRequiredService<IQdrantFileService>();
            Configuration = ServiceProvider.GetRequiredService<IConfiguration>();
            AssistantHistoryManager = ServiceProvider.GetRequiredService<IAssistantHistoryManager>();

            // Initialize HttpContext
            var context = new DefaultHttpContext();
            ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
        }

    }
}

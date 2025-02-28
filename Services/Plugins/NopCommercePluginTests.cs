using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalWebApi.Agent.SemanticKernel.Plugins.NopCommerce;
using PersonalWebApi.Tests.Controllers.Agent;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalWebApi.Tests.Services.Plugins
{
    public class NopCommercePluginTests
    {
        private readonly TestConfiguration _testConfig;
        private readonly IConfiguration _configuration;

        [Experimental("SKEXP0050")]
        public NopCommercePluginTests()
        {
            _testConfig = new TestConfiguration();
            _configuration = _testConfig.Configuration;
        }

        [Fact]
        public async Task ParaphraseTitlePlugin()
        {
            // Arrange
            _testConfig.Kernel.Plugins.AddFromType<NopCommercePlugin>();
            
            // Act
            // with plugin
            OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
            var resultPlugin = await _testConfig.Kernel.InvokePromptAsync("Parafrazuj produkt BL1", new(settings));
            Output.Write(resultPlugin.ToString());
        }
    }
}

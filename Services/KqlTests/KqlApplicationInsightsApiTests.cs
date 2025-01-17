using Microsoft.Azure.ApplicationInsights.Query.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Rest;
using PersonalWebApi.Tests.Controllers.Agent;
using PersonalWebApi.Utilities.Kql;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PersonalWebApi.Tests.Services.KqlTests
{
    public class KqlApplicationInsightsApiTests
    {
        private readonly TestConfiguration _testConfig;
        private readonly IConfiguration _configuration;

        [Experimental("SKEXP0050")]
        public KqlApplicationInsightsApiTests()
        {
            _testConfig = new TestConfiguration();
            _configuration = _testConfig.Configuration;
        }

        [Fact]
        public async Task ExecuteQueryAsync_WithValidQuery_ReturnsExpectedSchema()
        {
            KqlApplicationInsightsApi kql = new KqlApplicationInsightsApi(_configuration);
            var answer = await kql.ExecuteQueryAsync("traces | getschema");

            var schema = JsonSerializer.Serialize(answer);

            // Assert
            Assert.NotNull(answer);
            Assert.IsType<HttpOperationResponse<QueryResults>>(answer);
        }
    }
}

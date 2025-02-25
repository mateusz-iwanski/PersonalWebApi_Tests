using iText.Commons.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;
using PersonalWebApi.Processes.Document.Models;
using PersonalWebApi.Processes.FileStorage.Processes;
using PersonalWebApi.Processes.NopCommerce.Models;
using PersonalWebApi.Processes.NopCommerce.Processes;
using PersonalWebApi.Processes.Qdrant.Pipelines;
using PersonalWebApi.Tests.Controllers.Agent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalWebApi.Tests.Services.Steps
{
    public class ProductNopComerceStepsTests
    {

        private readonly TestConfiguration _testConfig;

        [Experimental("SKEXP0080")]
        public ProductNopComerceStepsTests()
        {
            _testConfig = new TestConfiguration();
        }

        [Fact]
        [Experimental("SKEXP0080")]
        public async Task ProductParaphrase_SouldFillProductNopStepDto()
        {
            var productNopStepDto = new ProductCollectNopStepDto()
            {
                Sku = "BL1"
            };

            CollectDataProcess collectDataProcess = new CollectDataProcess();
            ProductNopPipelines pipeline = new ProductNopPipelines();
            await pipeline.CollectProductPipeline(ProductNopPipelines.PrepareKelnerForPipeline(_testConfig.Configuration), productNopStepDto);
        }

    }
}

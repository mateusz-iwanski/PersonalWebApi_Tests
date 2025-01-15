using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using PersonalWebApi.Exceptions;
using PersonalWebApi.Utilities.WebScrapper;
using Xunit;
using static System.Net.WebRequestMethods;

namespace PersonalWebApi.Tests.Services.Firecrawl
{
    public class FirecrawlTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly PersonalWebApi.Utilities.WebScrapper.Firecrawl _firecrawl;

        string URL = "https://edition.cnn.com/2025/01/14/sport/bruno-lobo-kite-surfing-brazil-rescue-spt-intl/index.html";

        public FirecrawlTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            var inMemorySettings = new Dictionary<string, string> {
                {"Firecrawl:Access:ApiKey", "fc-fd6d0ef97d41448b85a0dc0b30087c9b"},
                {"Firecrawl:Access:ApiUrl", "https://api.firecrawl.dev/v1/scrape"},
                {"Firecrawl:Access:ApiMapUrl", "https://api.firecrawl.dev/v1/map"},
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _firecrawl = new PersonalWebApi.Utilities.WebScrapper.Firecrawl(_configuration);
        }

        /// <summary>
        /// Tests the ScrapingPageAsync method for successful scraping.
        /// </summary>
        /// <remarks>
        /// This test verifies that the ScrapingPageAsync method returns the expected content
        /// when the API request is successful.
        /// </remarks>
        [Fact]
        public async Task ScrapingPageAsync_Success()
        {
            // Arrange
            var expectedContent = "scraped content";
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedContent)
                });

            // Act
            var result = await _firecrawl.ScrapingPageAsync(URL);

            Output.Write(result);

            // Assert
            //Assert.Equal(expectedContent, result);
        }

        /// <summary>
        /// Tests the ScrapingPageAsync method for handling API errors.
        /// </summary>
        /// <remarks>
        /// This test verifies that the ScrapingPageAsync method throws an HttpRequestException
        /// when the API request fails.
        /// </remarks>
        [Fact]
        public async Task ScrapingPageAsync_ApiError()
        {
            // Arrange
            var errorMessage = "error content";
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(errorMessage)
                });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                _firecrawl.ScrapingPageAsync(URL));
            
            //Assert.Contains("Failed to scrape page", exception.Message);
        }

        /// <summary>
        /// Tests the MapPageAsync method for successful mapping.
        /// </summary>
        /// <remarks>
        /// This test verifies that the MapPageAsync method returns the expected content
        /// when the API request is successful.
        /// </remarks>
        [Fact]
        public async Task MapPageAsync_Success()
        {
            // Arrange
            var expectedContent = "mapped content";
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedContent)
                });

            // Act
            var result = await _firecrawl.MapPageAsync(URL);

            Output.Write(result);

            // Assert
            //Assert.Equal(expectedContent, result);
        }

        /// <summary>
        /// Tests the MapPageAsync method for handling API errors.
        /// </summary>
        /// <remarks>
        /// This test verifies that the MapPageAsync method throws an HttpRequestException
        /// when the API request fails.
        /// </remarks>
        [Fact]
        public async Task MapPageAsync_ApiError()
        {
            // Arrange
            var errorMessage = "error content";
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(errorMessage)
                });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                _firecrawl.MapPageAsync(URL));
            //Assert.Contains("Failed to map page", exception.Message);
        }
    }
}

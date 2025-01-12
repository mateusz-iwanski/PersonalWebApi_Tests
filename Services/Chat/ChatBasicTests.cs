using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PersonalWebApi.Services.Services.History;
using PersonalWebApi.Tests.Controllers.Agent;
using System.Threading.Tasks;
using Xunit;

namespace PersonalWebApi.Tests.Services.Chat
{
    public class ChatBasicTests
    {
        private readonly TestConfiguration _testConfig;
        private readonly IConfiguration _configuration;
        //private readonly Mock<IPersistentChatHistoryService> _persistentChatHistoryServiceMock;
        private readonly IPersistentChatHistoryService _persistentChatHistoryService;

        public ChatBasicTests()
        {
            _testConfig = new TestConfiguration();
            _configuration = _testConfig.Configuration;
            //_persistentChatHistoryServiceMock = new Mock<IPersistentChatHistoryService>();
            _persistentChatHistoryService = _testConfig.ServiceProvider.GetRequiredService<IPersistentChatHistoryService>();
        }

        [Fact]
        public async Task AddPersistenceMessageAndSaveChat_ShouldInvokeMethods()
        {
            // Arrange
            //var chatService = _persistentChatHistoryServiceMock.Object;

            // Act
            _persistentChatHistoryService.AddMessage(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User, "Hi assistant");
            _persistentChatHistoryService.AddMessage(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.System, "Hi user");
            await _persistentChatHistoryService.SaveChatAsync();

            // Assert
            //_persistentChatHistoryServiceMock.Verify(service => service.SaveChatAsync(), Times.Once);
        }

        [Fact]
        public async Task LoadPersistenceHistoryByConversationUuid_ShouldReturnCorrectHistory()
        {
            // Arrange
            var conversationUuid = Guid.NewGuid();
            
            var actualHistory = await _persistentChatHistoryService.LoadConversationAsync();

            // Loop through the history and write each message to the console



            // Assert
            //Assert.Equal(expectedHistory, actualHistory);
        }
    }
}

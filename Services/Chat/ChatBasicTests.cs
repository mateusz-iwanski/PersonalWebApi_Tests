using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Moq;
using PersonalWebApi.Services.Services.History;
using PersonalWebApi.Tests.Controllers.Agent;
using System.Threading.Tasks;
using Xunit;
using DocumentFormat.OpenXml.Wordprocessing;
using LLama.Common;
using System.Diagnostics;
using System.Text.Json;

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

        /// <summary>
        /// This test verifies that the methods for adding persistent messages, interacting with the assistant, 
        /// and saving the chat history are invoked correctly. It performs the following steps:
        /// 1. Manually sets the conversation UUID in the ContextAccessorReader, as it is normally injected into the context.
        /// 2. Retrieves the current chat history.
        /// 3. Adds system and user messages to the chat history.
        /// 4. Interacts with the assistant to get a response.
        /// 5. Adds the assistant's response to the chat history.
        /// 6. Saves the chat history.
        /// 7. Prints the assistant's response for debugging purposes.
        /// </summary>
        [Fact]
        public async Task AddPersistentChatHistoryAskAssistantSaveChat_ShouldInvokeMethods()
        {
            // Act
            var chatHistory = _persistentChatHistoryService.GetChatHistory();

            // Add messages to the chat history object
            _persistentChatHistoryService.AddMessage(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.System, "Always say no.");
            _persistentChatHistoryService.AddMessage(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.User, "Hi assistant");

            // talk with assistant
            var chatCompletionService = _testConfig.Kernel.GetRequiredService<IChatCompletionService>();
            ChatMessageContent resuslts = await chatCompletionService.GetChatMessageContentAsync(
                _persistentChatHistoryService.GetChatHistory(),
                kernel: _testConfig.Kernel
            );
            
            // Add the final message to the chat history object
            chatHistory.Add(resuslts);

            await _persistentChatHistoryService.SaveChatAsync();

            // Print the final message
            Debug.WriteLine($"################ Debug| Assistant Response: {resuslts}");


            // Assert
            //_persistentChatHistoryServiceMock.Verify(service => service.SaveChatAsync(), Times.Once);
        }

        /// <summary>
        /// This test verifies that the method for loading persistent chat history by conversation UUID returns the correct history.
        /// It performs the following steps:
        /// 1. Manually sets the conversation UUID in the ContextAccessorReader, as it is normally injected into the context.
        /// 2. Calls the method to load the conversation history. 
        /// 3. Iterates through the loaded history and writes each message to the debug console for verification.
        /// 4. Asserts that the loaded history matches the expected history (commented out for now).
        /// </summary>
        [Fact]
        public async Task LoadPersistenChatHistoryByConversationUuid_ShouldReturnCorrectHistory()
        {
            // Arrange
            var conversationUuid = Guid.NewGuid();

            // Act
            var actualHistory = await _persistentChatHistoryService.LoadPersistanceConversationAsync();  // this is ChatHistory object
            var eventStorageHistory = await _persistentChatHistoryService.LoadStorageEventsAsync();  // this is ChatHistory object

            Debug.WriteLine($"################ Debug| Chat history schema\n: { JsonSerializer.Serialize(actualHistory) }");

            // Loop through the history and write each message to the console
            int i = 1;
            foreach (var message in actualHistory)
            {
                Debug.WriteLine($"################ Debug| Message history (step: {i}) | Role: {message.Role} | Message: {message}");
                i++;
            }

            // Assert
            //Assert.Equal(expectedHistory, actualHistory);
        }
    }
}

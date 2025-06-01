using NUnit.Framework;  
using Moq;
using AuthChannel.Data.Models;
using AuthChannel.Services.Hubs;
using AuthChannel.Services;
using AuthChannel.Data.Contacts;

namespace Auth.Tests
{
    public class AzureDataTableMessagesTests
    {
        private Mock<IMessageHandler> _mockMessageHandler;
        

        [SetUp]
        public void Setup()
        {
            _mockMessageHandler = new Mock<IMessageHandler>();
           
        }

        [Test]
        public async Task GetMessagesForPublicRoom_ReturnsMessages()
        {
            
            // Arrange  
            
            var expectedMessages = new List<Message>
                  {
                      new Message("Public", DateTime.UtcNow, "Welcome to Public!", "Sent"),
                      new Message("Public", DateTime.UtcNow, "Another message", "Sent")
                  };
            
            _mockMessageHandler
                .Setup(m => m.LoadHistoryMessageAsync("Public"))
                .ReturnsAsync(expectedMessages);

            // Act  
            var result = await _mockMessageHandler.Object.LoadHistoryMessageAsync("Public");

            // Assert  
            Assert.That(result, Is.Not.Null.And.Not.Empty, "Result should not be null or empty.");
            Assert.That(result.Count, Is.EqualTo(expectedMessages.Count), "Result count should match expected messages count.");
            Assert.That(result, Is.EquivalentTo(expectedMessages), "Result should match the expected messages.");
        }

        [Test]
        public async Task AddNewMessageAsync_InsertsMessage_ReturnsSequenceId()
        {
            // Arrange
            var message = new Message("Public", DateTime.UtcNow, "Test Insert", "Sent");
            var expectedSequenceId = "seq-123";
            _mockMessageHandler
                .Setup(m => m.AddNewMessageAsync("Public", message))
                .ReturnsAsync(expectedSequenceId);

            // Act
            var result = await _mockMessageHandler.Object.AddNewMessageAsync("Public", message);

            // Assert
            Assert.That(result, Is.EqualTo(expectedSequenceId), "SequenceId should match expected value.");
            _mockMessageHandler.Verify(m => m.AddNewMessageAsync("Public", message), Times.Once);
        }
    }
}

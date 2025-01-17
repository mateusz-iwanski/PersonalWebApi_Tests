using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using PersonalWebApi.Services.Services.Qdrant;
using PersonalWebApi.Tests.Controllers.Agent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalWebApi.Tests.Services.Qdrant
{
    public class QdrantTest
    {
        private readonly TestConfiguration _testConfig;

        [Experimental("SKEXP0050")]
        public QdrantTest()
        {
            _testConfig = new TestConfiguration();
        }

        [Fact]
        public async Task UploadFileAndAskQuestion()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "sourcetest/superman_and_me.docx");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");
            }

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                IFormFile formFile = new FormFile(stream, 0, stream.Length, null, Path.GetFileName(filePath))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document" // Set the appropriate content type
                };
                await _testConfig.Qdrant.AddAsync(formFile, Guid.NewGuid(), 100, 100);
            }



            // Ask a question
            var question = "Who is the author of Superman and Me?";


            var response = await _testConfig.Qdrant.SearchAsync(new List<string> { question }, null, 5);
            foreach (var result in response)
            {
                Debug.WriteLine($"Found result with ID: {result.Id}");
                Debug.WriteLine($"Score: {result.Score}");
                Debug.WriteLine($"Version: {result.Version}");
                Debug.WriteLine($"Payload.BlobUri: {result.Payload.BlobUri}");
                Debug.WriteLine($"Payload.Text: {result.Payload.Text}");
                Debug.WriteLine($"Payload.ConversationId: {result.Payload.ConversationId}");
                Debug.WriteLine($"Payload.EndPosition: {result.Payload.EndPosition}");
                Debug.WriteLine($"Payload.UploadedBy: {result.Payload.UploadedBy}");
                Debug.WriteLine($"Payload.EmbeddingModel: {result.Payload.EmbeddingModel}");
                Debug.WriteLine($"Payload.StartPosition: {result.Payload.StartPosition}");
                Debug.WriteLine($"Payload.Author: {result.Payload.Author}");
                Debug.WriteLine($"Payload.FileName: {result.Payload.FileName}");
                Debug.WriteLine($"Payload.CreatedAt: {result.Payload.CreatedAt}");
                Debug.WriteLine($"Payload.FileId: {result.Payload.FileId}");
                Debug.WriteLine($"Payload.Summary: {result.Payload.Summary}");
                Debug.WriteLine($"Payload.Tags: {result.Payload.Tags}");
                Debug.WriteLine($"Payload.Title: {result.Payload.Title}");
                Debug.WriteLine($"Payload.MimeType: {result.Payload.MimeType}");
                Debug.WriteLine($"Payload.DataType: {result.Payload.DataType}");

                Debug.WriteLine($"Sci: {result.Score}");
            }
        }

    }
}

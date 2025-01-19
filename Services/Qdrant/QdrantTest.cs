using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using PersonalWebApi.Processes.Document.Models;
using PersonalWebApi.Processes.FileStorage.Steps;
using PersonalWebApi.Processes.Qdrant.Pipelines;
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

        [Experimental("SKEXP0050")]  // for SemanticKernelTextChunker
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

                var fileUuid = Guid.NewGuid();

                QdrantPipelines qdrantPipelines = new QdrantPipelines();
                await qdrantPipelines.Add(
                    _testConfig.Kernel,
                    new DocumentStepDto(fileUuid, formFile, Guid.NewGuid(), Guid.NewGuid()) { Overwrite = true }
                    );

            }

            // Ask a question
            var question = "Who is the author of Superman and Me?";


            var response = await _testConfig.Qdrant.SearchAsync(new List<string> { question }, null, 5);

            // Deserialize and debug the response
            var jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
            Debug.WriteLine(jsonResponse);

            foreach (var result in response)
            {
                Debug.WriteLine($"Found result with ID: {result["Id"]}");
                Debug.WriteLine($"Score: {result["Score"]}");
                Debug.WriteLine($"Version: {result["Version"]}");

                if (result["Payload"] is Dictionary<string, object> payload)
                {
                    Debug.WriteLine($"Payload.BlobUri: {payload["BlobUri"]}");
                    Debug.WriteLine($"Payload.Text: {payload["Text"]}");
                    Debug.WriteLine($"Payload.ConversationId: {payload["ConversationId"]}");
                    Debug.WriteLine($"Payload.EndPosition: {payload["EndPosition"]}");
                    Debug.WriteLine($"Payload.UploadedBy: {payload["UploadedBy"]}");
                    Debug.WriteLine($"Payload.EmbeddingModel: {payload["EmbeddingModel"]}");
                    Debug.WriteLine($"Payload.StartPosition: {payload["StartPosition"]}");
                    Debug.WriteLine($"Payload.Author: {payload["Author"]}");
                    Debug.WriteLine($"Payload.FileName: {payload["FileName"]}");
                    Debug.WriteLine($"Payload.CreatedAt: {payload["CreatedAt"]}");
                    Debug.WriteLine($"Payload.FileId: {payload["FileId"]}");
                    Debug.WriteLine($"Payload.Summary: {payload["Summary"]}");
                    Debug.WriteLine($"Payload.Tags: {payload["Tags"]}");
                    Debug.WriteLine($"Payload.Title: {payload["Title"]}");
                    Debug.WriteLine($"Payload.MimeType: {payload["MimeType"]}");
                    Debug.WriteLine($"Payload.DataType: {payload["DataType"]}");
                }
            }

        }

    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;
using PersonalWebApi.Processes.Document.Models;
using PersonalWebApi.Processes.FileStorage.Events;
using PersonalWebApi.Processes.FileStorage.Steps;
using PersonalWebApi.Tests.Controllers.Agent;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PersonalWebApi.Tests.Services.Steps
{
    public class FileStorageStepsTests
    {
        private readonly TestConfiguration _testConfig;

        [Experimental("SKEXP0080")]
        public FileStorageStepsTests()
        {
            _testConfig = new TestConfiguration();
        }

        [Fact]
        [Experimental("SKEXP0080")]
        public async Task CollectAsync_ShouldCollectMetadataAndEmitEvent()
        {
            // Arrange
            ProcessBuilder process = new("StepCollectMetadataTest");
            var fileMetadataComposer = process.AddStepFromType<FileMetadataComposer>();
            process.OnInputEvent(FileEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(fileMetadataComposer, functionName: FileMetadataComposerFunction.Collect, parameterName: "documentStepDto"));

            var kernelProcess = process.Build();

            var document = new DocumentStepDto(Guid.NewGuid(), CreateFormFile(), Guid.NewGuid(), Guid.NewGuid())
            {
                Content = "This is a test content. It contains multiple sentences. It is used for testing."
            };

            // Act
            using var runningProcess = await kernelProcess.StartAsync(
                _testConfig.Kernel,
                new KernelProcessEvent()
                {
                    Id = FileEvents.StartProcess,
                    Data = document
                });

            // Assert
            Assert.NotNull(document.Metadata);
            Assert.Equal("dummy.docx", document.Metadata["name"]);
            Assert.Equal("application/octet-stream", document.Metadata["content_type"]);
            Assert.Equal("Dummy content".Length.ToString(), document.Metadata["length_bytes"]);
            Assert.Equal("dummy.docx", document.Metadata["file_name"]);
            Assert.Equal("form-data; name=dummy; filename=dummy.docx", document.Metadata["content_disposition"]);
            Assert.Equal("True", document.Metadata["i_form_file"]);
            Assert.NotNull(document.Metadata["sha256_hash"]);
            Assert.NotEqual("N/A", document.Metadata["flesch_reading_ease"]);
            Assert.NotEqual("N/A", document.Metadata["flesch_kincaid_grade_level"]);

            // Output metadata to console
            foreach (var metadata in document.Metadata)
            {
                Output.Write($"{metadata.Key}: {metadata.Value}");
            }

            // Verify emitted event
            var emittedEvent = await runningProcess.GetStateAsync();
            Assert.NotNull(emittedEvent);

            Output.Write($"Emitted event: {emittedEvent}");
            //Assert.Equal(FileEvents.MetadataCollected, emittedEvent.);
            //Assert.Equal(document, emittedEvent.Data);
        }

        [Fact]
        [Experimental("SKEXP0080")]
        public async Task UploadIFormFileAsync_ShouldUploadFileAndEmitEvent()
        {
            // Arrange
            ProcessBuilder process = new("StepUploadFileTest");
            var fileStorageStep = process.AddStepFromType<FileStorageStep>();
            process.OnInputEvent(FileEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(fileStorageStep, functionName: FileStorageFunctions.UploadIFormFile, parameterName: "documentStepDto"));

            var kernelProcess = process.Build();

            var document = new DocumentStepDto(Guid.NewGuid(), CreateFormFile(), Guid.NewGuid(), Guid.NewGuid())
            {
                Overwrite = true,
                Uri = new Uri("https://personalblobstore.blob.core.windows.net/personalagent/superman_and_me.docx")
            };

            // Act
            using var runningProcess = await kernelProcess.StartAsync(
                _testConfig.Kernel,
                new KernelProcessEvent()
                {
                    Id = FileEvents.StartProcess,
                    Data = document
                });

            // Assert
            Assert.NotNull(document.Uri);
            Output.Write($"File uploaded to: {document.Uri}");

            // Verify emitted event
            var emittedEvent = await runningProcess.GetStateAsync();
            Assert.NotNull(emittedEvent);
            Output.Write($"Emitted event: {emittedEvent}");
        }

        [Fact]
        [Experimental("SKEXP0080")]
        public async Task SaveActionLogAsync_ShouldSaveLogAndEmitEvent()
        {
            // Arrange
            ProcessBuilder process = new("StepSaveActionLogTest");
            var logFileActionStep = process.AddStepFromType<LogFileActionStep>();
            process.OnInputEvent(FileEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(logFileActionStep, functionName: LogFileActionStepFunctions.SaveActionLog, parameterName: "documentStepDto"));

            var kernelProcess = process.Build();

            var document = new DocumentStepDto(Guid.NewGuid(), CreateFormFile(), Guid.NewGuid(), Guid.NewGuid())
            {
                Uri = new Uri("https://personalblobstore.blob.core.windows.net/personalagent/superman_and_me.docx")
            };

            // Act
            using var runningProcess = await kernelProcess.StartAsync(
                _testConfig.Kernel,
                new KernelProcessEvent()
                {
                    Id = FileEvents.StartProcess,
                    Data = document
                });

            // Assert
            Output.Write($"Log saved for file: {document.FileId}");

            // Verify emitted event
            var emittedEvent = await runningProcess.GetStateAsync();
            Assert.NotNull(emittedEvent);
            Output.Write($"Emitted event: {emittedEvent}");
        }

        private IFormFile CreateFormFile()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("Dummy content");
            writer.Flush();
            stream.Position = 0;
            return new FormFile(stream, 0, stream.Length, "dummy", "dummy.docx")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream",
                ContentDisposition = "form-data; name=dummy; filename=dummy.docx"
            };
        }
    }
}


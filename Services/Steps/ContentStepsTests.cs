using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;
using PersonalWebApi.Processes.Document.Models;
using PersonalWebApi.Processes.Document.Steps;
using PersonalWebApi.Processes.Metadata.Steps;
using PersonalWebApi.Processes.Qdrant.Events;
using PersonalWebApi.Services.Qdrant.Processes.Steps;
using PersonalWebApi.Tests.Controllers.Agent;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace PersonalWebApi.Tests.Services.Steps
{
    /// <summary>
    /// Every steps which use agent has in appsettings.StepAgentMappings configuration
    /// </summary>
    public class ContentStepsTests
    {
        private readonly TestConfiguration _testConfig;

        public string Content = @"""
                in: Comic Book Characters, El Family, Kryptonians, and 3 more
                Superman
                Sign in to edit
                Superman
                Superman flight Secret Origins no6
                Debut	Action Comics #1 (April 1938)
                Created by	Jerry Siegel and Joe Shuster
                Portrayed by	see In other media
                Statistics
                AKA	Clark Kent, Kal-El
                Classification	Kryptonian
                Affiliation	Justice League, Daily Planet, Legion of Super-Heroes, Kryptonian Military Guild, Supermen of America, Superman Squad, Team Superman
                Relatives	House of El; Don-El (great-great-grandfather), Ter-El (great-grandfather), Charys-El (great-grandmother), Seyg-El (grandfather), Nyssa-Vex (grandmother), Jor-El (father), Lara Lor-Van (mother), H'El (adoptive brother), Nim-El (uncle), Zor-El (uncle), Alura, (aunt), Kara Zor-El (cousin)
                Kent Family; Jonathan Kent (adoptive father), Martha Kent (adoptive mother) Lois Lane (wife), Jonathan Samuel Kent (son), Superman Dynasty (decendents)

                Abilities	Kryptonian Powers
                For other uses, see Superman (disambiguation)
                Superman is a superhero published by DC Comics since 1938. An alien named Kal-El from the destroyed planet Krypton. He was sent to Earth and raised as Clark Kent by human foster parents, Martha and Jonathan Kent. As an adult, Superman became the protector of Earth, working at the Daily Planet as Clark Kent alongside his partner and wife Lois Lane.

                Biography
                Sent as a baby to Earth from the dying planet Krypton, Kal-El was adopted by Martha and Jonathan Kent of Smallville, Kansas. Growing up as Clark Kent, he devoted his life to helping others with the abilities he developed from Earth's sun. Moving to Metropolis, he became Superman, while still maintaining his secret identity as Clark Kent, who works at the Daily Planet newspaper.

                Superman's birthday is February 29th. In real life it is accepted that Action Comics #1 first came out in 18th April 1938, therefore it is celebrated as (one of) character's ""day"", another one is on June 12 declared by special committee in 2013 in Cleveland, Ohio the residence of Superman's creators Jerry Siegel and Joe Shuster.

                For detailed biographies by continuity, see:
                Superman's Biography (Pre-Crisis) - Superman's history from 1938 to Crisis on Infinite Earths. Original Earth-2 and Earth-1 version which became main one since 1945.
                Superman's Biography (Post-Crisis) - Superman's history from Crisis on Infinite Earths to Flashpoint (with a brief hiatus between 2011 and 2015) and his return in Rebirth.
                Superman's Biography (Post-Flashpoint) - Superman's biography from the post-Flashpoint universe (marketed as the New 52), Post-Flashpoint and Post-Crisis merged later on.
                Personality
                In the original Siegel and Shuster stories, Superman's personality is rough and aggressive. He was seen stepping in to stop wife beaters, profiteers, a lynch mob and gangsters, with rather rough edges and a looser moral code than we may be used to today. In later adventures, he became softer and had more of a sense of idealism and moral code of conduct. Although not as cold-blooded as the early Batman, the Superman featured in the comics of the 1930s is unconcerned about the harm his strength may cause, tossing villainous characters in such a manner that fatalities would presumably occur, although these were seldom shown explicitly on the page. This came to an end when Superman vowed never to take a life.

                Superman is an extremely moral person, believing it immoral to kill anyone under any circumstances, and will do whatever he can to avoid it. Clark's upbringing in the Midwest largely contributes to this, as his adoptive parents raised him to do the right thing.

                In Superman/Batman #3, Batman says, ""It is a remarkable dichotomy. In many ways, Clark is the most human of us all. Then...he shoots fire from the skies, and it is difficult not to think of him as a god. And how fortunate we all are that it does not occur to him.""

                Superman is also a bit of a loner, in that, for much of his life, he doesn't reveal his true identity and powers to anyone, not even his closest friends. Many times they come close to figuring it out on their own, but often he will arrange an elaborate deception to trick them into believing Clark Kent and Superman are entirely separate. He's known to collect mementos of his adventures and his life in the Fortress of Solitude, and has even been known to have wax statues of all his friends there.

                Powers and Abilities
                Superman possesses the ability to fly under his own power, incredible strength and near invulnerability, as he can only be harmed by the element Kryptonite. His eyes can emit bursts of heat, while vision ranges from the microscopic to the telescopic. His vision is also capable of a broader spectrum than human eyes, able to see x-rays and radio waves. He can hear faint sounds amongst a bustle of noises by concentrating. His lungs are capable of holding air for long periods of time in environments without oxygen, and the ability to compress this air and exhale it in a freezing capacity.

                Superman's powers vary over the years in various forms of media. For a more in-depth view on this range, see Superman's Powers and Abilities.
                """;

        [Experimental("SKEXP0050")]
        public ContentStepsTests()
        {
            _testConfig = new TestConfiguration();
        }

        /// <summary>
        /// Read file from URI and summarize file
        /// Kernel Memory is using in SummarizeStep
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Experimental("SKEXP0080")]
        public async Task StepsSummarizeText_returnSummarize()
        {

            ProcessBuilder process = new("StepSummarizeTest");
            var summarizeDocument = process.AddStepFromType<SummarizeStep>();
            process.OnInputEvent(QdrantEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(summarizeDocument, functionName: SummarizeStepFunctions.SummarizeText, parameterName: "documentStepDto"));

            var kernelProcess = process.Build();

            var document = new DocumentStepDto(Guid.NewGuid(), CreateFormFile(), Guid.NewGuid(), Guid.NewGuid()) { Id = Guid.NewGuid(), Uri = new Uri("https://personalblobstore.blob.core.windows.net/personalagent/superman_and_me.docx") };

            using var runningProcess = await kernelProcess.StartAsync(
                _testConfig.Kernel,
                new KernelProcessEvent()
                {
                    Id = QdrantEvents.StartProcess,
                    Data = document
                });


            // Assert that the summary is a text
            Assert.False(string.IsNullOrEmpty(document.Summary), "The document summary should not be null or empty.");
            Output.Write(document.Summary);
        }


        /// <summary>
        /// Use content (string) and return tags as List<string>
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Experimental("SKEXP0080")]
        public async Task StepsTagifyText_returnTags()
        {
            ProcessBuilder process = new("StepTagifyTest");
            var tagify = process.AddStepFromType<TagifyStep>();

            process.OnInputEvent(QdrantEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(tagify, functionName: TagifyStepFunctions.GenerateTags, parameterName: "documentStepDto"));

            var kernelProcess = process.Build();

            var document = new DocumentStepDto(Guid.NewGuid(), CreateFormFile(), Guid.NewGuid(), Guid.NewGuid()) { 
                Id = Guid.NewGuid(), 
                Uri = new Uri("https://personalblobstore.blob.core.windows.net/personalagent/superman_and_me.docx"),
                Content = Content
                
            };

            using var runningProcess = await kernelProcess.StartAsync(
                _testConfig.Kernel,
                new KernelProcessEvent()
                {
                    Id = QdrantEvents.StartProcess,
                    Data = document
                });

            // Assert that Tags is a List<string>
            Assert.IsType<List<string>>(document.Tags);

            foreach (var tag in document.Tags)
            {
                Output.Write(tag);
            }
        }

        // <summary>
        /// Test for ReadUriAsync method in DocumentReaderStep
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Experimental("SKEXP0080")]
        public async Task StepsDocumentReaderStepReadUriAsync_ShouldReadContentAndEmitEvent()
        {
            ProcessBuilder process = new("StepReadUriTest");
            var documentReaderStep = process.AddStepFromType<DocumentReaderStep>();
            process.OnInputEvent(QdrantEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(documentReaderStep, functionName: DocumentReaderStepFunctions.ReadUri, parameterName: "documentStepDto"));

            var kernelProcess = process.Build();

            var document = new DocumentStepDto(Guid.NewGuid(), CreateFormFile(), Guid.NewGuid(), Guid.NewGuid())
            {
                Id = Guid.NewGuid(),
                Uri = new Uri("https://personalblobstore.blob.core.windows.net/personalagent/superman_and_me.docx")
            };

            using var runningProcess = await kernelProcess.StartAsync(
                _testConfig.Kernel,
                new KernelProcessEvent()
                {
                    Id = QdrantEvents.StartProcess,
                    Data = document
                });

            // Assert that the content is not null or empty
            Assert.False(string.IsNullOrEmpty(document.Content), "The document content should not be null or empty.");
            Output.Write(document.Content);
        }

        /// <summary>
        /// Test for SpecifyDocumentLanguageAsync method in SpecifyDocumentLanguageStep
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Experimental("SKEXP0080")]
        public async Task StepsSpecifyDocumentLanguageAsync_ShouldSpecifyLanguageAndEmitEvent()
        {
            ProcessBuilder process = new("StepSpecifyDocumentLanguageTest");
            var specifyDocumentLanguageStep = process.AddStepFromType<SpecifyDocumentLanguageStep>();
            process.OnInputEvent(QdrantEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(specifyDocumentLanguageStep, functionName: DocumentLanguageStepFunctions.SpecifyDocumentLanguage, parameterName: "documentStepDto"));

            var kernelProcess = process.Build();

            var document = new DocumentStepDto(Guid.NewGuid(), CreateFormFile(), Guid.NewGuid(), Guid.NewGuid())
            {
                Id = Guid.NewGuid(),
                Content = Content
            };

            using var runningProcess = await kernelProcess.StartAsync(
                _testConfig.Kernel,
                new KernelProcessEvent()
                {
                    Id = QdrantEvents.StartProcess,
                    Data = document
                });

            // Assert that the language is specified
            Assert.False(string.IsNullOrEmpty(document.Language), "The document language should not be null or empty.");
            Output.Write(document.Language);
        }

        /// <summary>
        /// Test for SpecifyDocumentTypeAsync method in SpecifyDocumentTypeStep
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Experimental("SKEXP0080")]
        public async Task StepsSpecifyDocumentTypeAsync_ShouldSpecifyTypeAndEmitEvent()
        {
            ProcessBuilder process = new("StepSpecifyDocumentTypeTest");
            var specifyDocumentTypeStep = process.AddStepFromType<SpecifyDocumentTypeStep>();
            process.OnInputEvent(QdrantEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(specifyDocumentTypeStep, functionName: DocumentInfoStepFunctions.SpecifyDocumentType, parameterName: "documentStepDto"));

            var kernelProcess = process.Build();

            var document = new DocumentStepDto(Guid.NewGuid(), CreateFormFile(), Guid.NewGuid(), Guid.NewGuid())
            {
                Id = Guid.NewGuid(),
                Content = Content
            };

            using var runningProcess = await kernelProcess.StartAsync(
                _testConfig.Kernel,
                new KernelProcessEvent()
                {
                    Id = QdrantEvents.StartProcess,
                    Data = document
                });

            // Assert that the document type is specified
            Assert.NotEmpty(document.DocumentType);
            Output.Write(string.Join(", ", document.DocumentType));
        }

        /// <summary>
        /// Test for ChunkTextAsync method in TextChunkerStep
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Experimental("SKEXP0080")]
        public async Task StepsChunkTextAsync_ShouldChunkTextAndEmitEvent()
        {
            ProcessBuilder process = new("StepChunkTextTest");
            var textChunkerStep = process.AddStepFromType<TextChunkerStep>();
            process.OnInputEvent(QdrantEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(textChunkerStep, functionName: TextChunkerStepFunctions.ChunkText, parameterName: "documentStepDto"));

            var kernelProcess = process.Build();

            var document = new DocumentStepDto(Guid.NewGuid(), CreateFormFile(), Guid.NewGuid(), Guid.NewGuid())
            {
                Id = Guid.NewGuid(),
                Content = Content
            };

            using var runningProcess = await kernelProcess.StartAsync(
                _testConfig.Kernel,
                new KernelProcessEvent()
                {
                    Id = QdrantEvents.StartProcess,
                    Data = document
                });

            // Assert that the chunker collection is not null or empty
            Assert.NotNull(document.ChunkerCollection);
            Assert.NotEmpty(document.ChunkerCollection);

            foreach (var chunk in document.ChunkerCollection)
            {
                Output.Write(chunk.Content);
            }
        }


        private IFormFile CreateFormFile()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("Dummy content");
            writer.Flush();
            stream.Position = 0;
            return new FormFile(stream, 0, stream.Length, "dummy", "dummy.docx");
        }
    }
}

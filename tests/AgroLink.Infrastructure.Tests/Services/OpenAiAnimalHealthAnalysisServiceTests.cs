using System.Net;
using System.Text.Json;
using AgroLink.Application.Features.Animals.Models;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using AgroLink.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Shouldly;

namespace AgroLink.Infrastructure.Tests.Services;

[TestFixture]
public class OpenAiAnimalHealthAnalysisServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _storageMock = new Mock<IStorageService>();
        _config = BuildConfig("test-key");

        _storageMock
            .Setup(s => s.GetPresignedUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns("https://storage.googleapis.com/bucket/photo.jpg?sig=abc");
    }

    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<IStorageService> _storageMock = null!;
    private IConfiguration _config = null!;

    // ── helpers ───────────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig(string apiKey)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OpenAI:ApiKey"] = apiKey,
                    ["OpenAI:VisionModel"] = "gpt-4o",
                    ["OpenAI:ChatBaseUrl"] = "https://api.openai.com/v1/chat/completions",
                }
            )
            .Build();
    }

    private OpenAiAnimalHealthAnalysisService BuildService(IConfiguration? config = null)
    {
        return new OpenAiAnimalHealthAnalysisService(
            new HttpClient(_mockHandler.Object),
            _storageMock.Object,
            config ?? _config,
            NullLogger<OpenAiAnimalHealthAnalysisService>.Instance
        );
    }

    private static AnimalHealthAnalysisRequest DefaultRequest(
        Sex sex = Sex.Female,
        ProductionStatus production = ProductionStatus.Milking,
        ReproductiveStatus reproductive = ReproductiveStatus.Open,
        string? breed = "Brahman",
        int ageYears = 3
    )
    {
        return new AnimalHealthAnalysisRequest
        {
            AnimalName = "Rosita",
            Breed = breed,
            Sex = sex,
            BirthDate = DateTime.UtcNow.AddYears(-ageYears),
            ProductionStatus = production,
            ReproductiveStatus = reproductive,
            PhotoStorageKey = "farm/1/photo.jpg",
            PhotoContentType = "image/jpeg",
        };
    }

    private static string WrapInChatResponse(string aiJson)
    {
        return JsonSerializer.Serialize(
            new { choices = new[] { new { message = new { content = aiJson } } } }
        );
    }

    // Sets up the mock to always return the given AI JSON wrapped in Chat Completions format.
    private void SetupOpenAiResponse(string aiJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = status,
                    Content = new StringContent(WrapInChatResponse(aiJson)),
                }
            );
    }

    // Sets up the mock to capture every request body and return the given response.
    private List<string> SetupCapturingHandler(string aiJson)
    {
        var bodies = new List<string>();
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                async (req, ct) =>
                {
                    bodies.Add(await req.Content!.ReadAsStringAsync(ct));
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(WrapInChatResponse(aiJson)),
                    };
                }
            );
        return bodies;
    }

    // ── happy path ────────────────────────────────────────────────────────────────

    [Test]
    public async Task AnalyzeAsync_ValidResponse_ReturnsAllFields()
    {
        SetupOpenAiResponse(
            """{"bodyConditionScore":3.5,"hasAlert":true,"alertDescription":"Garrapatas visibles en lomo.","photoRejected":false,"rejectionReason":null}"""
        );

        var result = await BuildService().AnalyzeAsync(DefaultRequest());

        result.BodyConditionScore.ShouldBe(3.5);
        result.HasAlert.ShouldBeTrue();
        result.AlertDescription.ShouldBe("Garrapatas visibles en lomo.");
        result.PhotoRejected.ShouldBeFalse();
        result.RejectionReason.ShouldBeNull();
        result.RawAiResponse.ShouldNotBeEmpty();
    }

    [Test]
    public async Task AnalyzeAsync_NoAlerts_ReturnsFalseAndNullDescription()
    {
        SetupOpenAiResponse(
            """{"bodyConditionScore":4.0,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        var result = await BuildService().AnalyzeAsync(DefaultRequest());

        result.HasAlert.ShouldBeFalse();
        result.AlertDescription.ShouldBeNull();
        result.PhotoRejected.ShouldBeFalse();
    }

    [Test]
    public async Task AnalyzeAsync_RawAiResponse_ContainsFullChatCompletionEnvelope()
    {
        SetupOpenAiResponse(
            """{"bodyConditionScore":3.0,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        var result = await BuildService().AnalyzeAsync(DefaultRequest());

        result.RawAiResponse.ShouldContain("choices");
    }

    // ── photo rejected ────────────────────────────────────────────────────────────

    [Test]
    public async Task AnalyzeAsync_AISetsPhotoRejected_ReturnsRejectedResult()
    {
        SetupOpenAiResponse(
            """{"bodyConditionScore":0,"hasAlert":false,"alertDescription":null,"photoRejected":true,"rejectionReason":"Imagen no clara"}"""
        );

        var result = await BuildService().AnalyzeAsync(DefaultRequest());

        result.PhotoRejected.ShouldBeTrue();
        result.RejectionReason.ShouldBe("Imagen no clara");
    }

    // ── base64 fallback ───────────────────────────────────────────────────────────

    [Test]
    public async Task AnalyzeAsync_UrlCallThrows_FallsBackToBase64AndSucceeds()
    {
        var callCount = 0;
        var successBody = WrapInChatResponse(
            """{"bodyConditionScore":2.5,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                async (_, _) =>
                {
                    if (Interlocked.Increment(ref callCount) == 1)
                    {
                        throw new HttpRequestException("Simulated network error");
                    }

                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(successBody),
                    };
                }
            );

        _storageMock
            .Setup(s => s.GetFileBytesAsync("farm/1/photo.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0xFF, 0xD8, 0xFF });

        var result = await BuildService().AnalyzeAsync(DefaultRequest());

        callCount.ShouldBe(2);
        result.PhotoRejected.ShouldBeFalse();
        result.BodyConditionScore.ShouldBe(2.5);
    }

    [Test]
    public async Task AnalyzeAsync_UrlCallFails_SecondCallContainsBase64DataUrl()
    {
        var photoBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        var bodies = new List<string>();
        var callCount = 0;
        var successBody = WrapInChatResponse(
            """{"bodyConditionScore":3.0,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                async (req, ct) =>
                {
                    bodies.Add(await req.Content!.ReadAsStringAsync(ct));
                    if (Interlocked.Increment(ref callCount) == 1)
                    {
                        throw new HttpRequestException("URL unreachable");
                    }

                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(successBody),
                    };
                }
            );

        _storageMock
            .Setup(s => s.GetFileBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(photoBytes);

        await BuildService().AnalyzeAsync(DefaultRequest());

        bodies.Count.ShouldBe(2);
        var expectedDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(photoBytes)}";
        bodies[1].ShouldContain(expectedDataUrl);
    }

    [Test]
    public async Task AnalyzeAsync_UrlFailsAndNoBytesAvailable_ReturnsRejected()
    {
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Simulated error"));

        _storageMock
            .Setup(s => s.GetFileBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var result = await BuildService().AnalyzeAsync(DefaultRequest());

        result.PhotoRejected.ShouldBeTrue();
        result.RejectionReason.ShouldNotBeNullOrEmpty();
    }

    // ── API key guard ─────────────────────────────────────────────────────────────

    [Test]
    public async Task AnalyzeAsync_EmptyApiKey_ReturnsRejectedWithoutHttpCall()
    {
        var result = await BuildService(BuildConfig("")).AnalyzeAsync(DefaultRequest());

        result.PhotoRejected.ShouldBeTrue();
        _mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    // ── malformed / missing AI JSON fields ────────────────────────────────────────

    [Test]
    public async Task AnalyzeAsync_AiContentIsNotJson_ReturnsRejected()
    {
        SetupOpenAiResponse("not valid JSON {{{{}}}}");

        var result = await BuildService().AnalyzeAsync(DefaultRequest());

        result.PhotoRejected.ShouldBeTrue();
    }

    [Test]
    public async Task AnalyzeAsync_AiContentEmptyObject_DefaultsToZeroAndFalse()
    {
        SetupOpenAiResponse("{}");

        var result = await BuildService().AnalyzeAsync(DefaultRequest());

        result.BodyConditionScore.ShouldBe(0);
        result.HasAlert.ShouldBeFalse();
        result.PhotoRejected.ShouldBeFalse();
        result.AlertDescription.ShouldBeNull();
    }

    // ── presigned URL ─────────────────────────────────────────────────────────────

    [Test]
    public async Task AnalyzeAsync_GeneratesFreshPresignedUrlWithMinimumFiveMinuteExpiry()
    {
        SetupOpenAiResponse(
            """{"bodyConditionScore":3.0,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        await BuildService().AnalyzeAsync(DefaultRequest());

        _storageMock.Verify(
            s =>
                s.GetPresignedUrl(
                    "farm/1/photo.jpg",
                    It.Is<TimeSpan>(t => t >= TimeSpan.FromMinutes(5))
                ),
            Times.Once
        );
    }

    // ── bio packet content ────────────────────────────────────────────────────────

    [Test]
    [TestCase(Sex.Female, "hembra")]
    [TestCase(Sex.Male, "macho")]
    public async Task AnalyzeAsync_SexTranslatedToSpanishInPrompt(Sex sex, string expected)
    {
        var bodies = SetupCapturingHandler(
            """{"bodyConditionScore":3.0,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        await BuildService().AnalyzeAsync(DefaultRequest(sex));

        bodies[0].ShouldContain(expected);
    }

    [Test]
    [TestCase(ProductionStatus.Milking, "lactante")]
    [TestCase(ProductionStatus.Calf, "ternero")]
    [TestCase(ProductionStatus.Dry, "seca")]
    [TestCase(ProductionStatus.Bull, "semental")]
    [TestCase(ProductionStatus.Steer, "novillo")]
    [TestCase(ProductionStatus.Heifer, "vaquilla")]
    public async Task AnalyzeAsync_ProductionStatusTranslatedInPrompt(
        ProductionStatus status,
        string expectedFragment
    )
    {
        var bodies = SetupCapturingHandler(
            """{"bodyConditionScore":3.0,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        await BuildService().AnalyzeAsync(DefaultRequest(production: status));

        bodies[0].ShouldContain(expectedFragment);
    }

    [Test]
    [TestCase(ReproductiveStatus.Pregnant, "pre")] // "preñada"
    [TestCase(ReproductiveStatus.Open, "vac")] // "vacía"
    [TestCase(ReproductiveStatus.NotApplicable, "aplica")]
    public async Task AnalyzeAsync_ReproductiveStatusTranslatedInPrompt(
        ReproductiveStatus status,
        string expectedFragment
    )
    {
        var bodies = SetupCapturingHandler(
            """{"bodyConditionScore":3.0,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        await BuildService().AnalyzeAsync(DefaultRequest(reproductive: status));

        bodies[0].ShouldContain(expectedFragment);
    }

    [Test]
    public async Task AnalyzeAsync_NullBreed_UsesGenericBreedDescriptionInPrompt()
    {
        var bodies = SetupCapturingHandler(
            """{"bodyConditionScore":3.0,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        await BuildService().AnalyzeAsync(DefaultRequest(breed: null));

        bodies[0].ShouldContain("raza no especificada");
    }

    [Test]
    public async Task AnalyzeAsync_IncludesAgeYearsInPrompt()
    {
        var bodies = SetupCapturingHandler(
            """{"bodyConditionScore":3.0,"hasAlert":false,"alertDescription":null,"photoRejected":false,"rejectionReason":null}"""
        );

        await BuildService().AnalyzeAsync(DefaultRequest(ageYears: 2));

        bodies[0].ShouldContain("2 año");
    }
}

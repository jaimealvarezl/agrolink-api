using System.IO;
using System.Text;
using System.Text.Json;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Infrastructure.Services;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace AgroLink.Infrastructure.Tests.Services;

[TestFixture]
public class LambdaExternalApiWorkerClientTests
{
    private const string FunctionName = "agrolink-external-worker";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private Mock<IAmazonLambda> _lambdaMock = null!;
    private LambdaExternalApiWorkerClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _lambdaMock = new Mock<IAmazonLambda>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ExternalWorkers:WorkerFunctionName"] = FunctionName,
                }
            )
            .Build();

        _client = new LambdaExternalApiWorkerClient(
            _lambdaMock.Object,
            config,
            NullLogger<LambdaExternalApiWorkerClient>.Instance
        );
    }

    private static InvokeResponse BuildInvokeResponse(ExternalWorkerResponse response)
    {
        var json = JsonSerializer.Serialize(response, JsonOptions);
        return new InvokeResponse { Payload = new MemoryStream(Encoding.UTF8.GetBytes(json)) };
    }

    [Test]
    public async Task ExecuteAsync_InvokesLambdaWithCorrectFunctionNameAndPayload()
    {
        var request = new ExternalWorkerRequest(
            "corr-1",
            ExternalWorkerOperations.GetMedicationAdvice,
            JsonSerializer.SerializeToElement(new { })
        );

        var expectedResponse = new ExternalWorkerResponse("corr-1", request.Operation, true, null, null);
        InvokeRequest? capturedInvokeRequest = null;

        _lambdaMock
            .Setup(l => l.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvokeRequest, CancellationToken>((req, _) => capturedInvokeRequest = req)
            .ReturnsAsync(BuildInvokeResponse(expectedResponse));

        await _client.ExecuteAsync(request, CancellationToken.None);

        capturedInvokeRequest.ShouldNotBeNull();
        capturedInvokeRequest!.FunctionName.ShouldBe(FunctionName);
        capturedInvokeRequest.InvocationType.ShouldBe(InvocationType.RequestResponse);

        var sentRequest = JsonSerializer.Deserialize<ExternalWorkerRequest>(
            capturedInvokeRequest.Payload,
            JsonOptions
        );
        sentRequest!.CorrelationId.ShouldBe("corr-1");
        sentRequest.Operation.ShouldBe(ExternalWorkerOperations.GetMedicationAdvice);
    }

    [Test]
    public async Task ExecuteAsync_ReturnsDeserializedResponse()
    {
        var request = new ExternalWorkerRequest(
            "corr-2",
            ExternalWorkerOperations.TranscribeAudio,
            JsonSerializer.SerializeToElement(new { })
        );

        var expectedResponse = new ExternalWorkerResponse(
            "corr-2",
            ExternalWorkerOperations.TranscribeAudio,
            true,
            JsonSerializer.SerializeToElement(new { text = "hello world" }),
            null
        );

        _lambdaMock
            .Setup(l => l.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildInvokeResponse(expectedResponse));

        var response = await _client.ExecuteAsync(request, CancellationToken.None);

        response.CorrelationId.ShouldBe("corr-2");
        response.Success.ShouldBeTrue();
        response.Result!.Value.GetProperty("text").GetString().ShouldBe("hello world");
    }

    [Test]
    public async Task ExecuteAsync_WhenFunctionErrorSet_ThrowsInvalidOperationException()
    {
        var request = new ExternalWorkerRequest(
            "corr-3",
            ExternalWorkerOperations.SynthesizeSpeech,
            JsonSerializer.SerializeToElement(new { })
        );

        _lambdaMock
            .Setup(l => l.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse
            {
                FunctionError = "Unhandled",
                Payload = new MemoryStream(Encoding.UTF8.GetBytes("{\"errorMessage\":\"boom\"}")),
            });

        await Should.ThrowAsync<InvalidOperationException>(
            () => _client.ExecuteAsync(request, CancellationToken.None)
        );
    }

    [Test]
    public void Constructor_WhenFunctionNameMissing_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

        Should.Throw<InvalidOperationException>(() =>
            new LambdaExternalApiWorkerClient(
                _lambdaMock.Object,
                config,
                NullLogger<LambdaExternalApiWorkerClient>.Instance
            )
        );
    }

    [Test]
    public async Task ExecuteAsync_PassesCancellationTokenToLambda()
    {
        var request = new ExternalWorkerRequest(
            "corr-4",
            ExternalWorkerOperations.SendTelegramText,
            JsonSerializer.SerializeToElement(new { })
        );

        var expectedResponse = new ExternalWorkerResponse("corr-4", request.Operation, true, null, null);
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;

        _lambdaMock
            .Setup(l => l.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvokeRequest, CancellationToken>((_, ct) => capturedToken = ct)
            .ReturnsAsync(BuildInvokeResponse(expectedResponse));

        await _client.ExecuteAsync(request, cts.Token);

        capturedToken.ShouldBe(cts.Token);
    }
}

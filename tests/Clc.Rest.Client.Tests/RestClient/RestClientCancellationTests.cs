using System.Net;
using System.Net.Http;
using Clc.Rest.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Clc.Rest.Client.Tests.RestClientTestHelpers;

namespace Clc.Rest.Client.Tests;

[TestClass]
public class RestClientCancellationTests
{
    [TestMethod]
    public async Task ExecuteAsync_Passes_CancellationToken_To_HttpMessageHandler()
    {
        var tokenSource = new CancellationTokenSource();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await client.ExecuteAsync<string>(RestRequest.Get("/data"), cancellationToken: tokenSource.Token);

        Assert.IsTrue(handler.LastCancellationToken.CanBeCanceled);
        Assert.AreNotEqual(CancellationToken.None, handler.LastCancellationToken);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_Cancelled_Before_Send_Does_Not_Run_Authenticator_Or_Serializer()
    {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var authenticator = new TrackingAuthenticator();
        var serializer = new TrackingSerializer();
        var client = CreateClient(handler);
        var request = new RestRequest(HttpMethod.Post, "/data", body: new { Name = "Body" })
        {
            Authenticator = authenticator,
            Serializer = serializer
        };

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            async () => await client.ExecuteAsync<string>(request, tokenSource.Token));

        Assert.IsFalse(authenticator.WasCalled);
        Assert.IsFalse(serializer.WasCalled);
        Assert.IsNull(handler.LastRequest);
    }

    [TestMethod]
    public async Task ExecuteAsync_When_Cancelled_Before_Send_Propagates_OperationCanceledException()
    {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            async () => await client.ExecuteAsync<string>(
                RestRequest.Get("/data"),
                cancellationToken: tokenSource.Token));

        Assert.IsNull(handler.LastRequest);
    }

    [TestMethod]
    public async Task ExecuteAsync_With_Body_When_Cancelled_Before_Send_Propagates_OperationCanceledException()
    {
        var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{}"));
        var client = CreateClient(handler);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            async () => await client.ExecuteAsync<string>(
                new RestRequest(HttpMethod.Post, "/data", body: new { Name = "Body" }),
                tokenSource.Token));

        Assert.IsNull(handler.LastRequest);
    }
}

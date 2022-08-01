using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Loom.Azure.Functions.Extensions.Amazon.SQS;

internal class SqsListener : IListener
{
    private readonly Random _random = new();
    private readonly List<Task> _runningTasks = new();
    private readonly AmazonSQSClient _client;
    private readonly string _queueUrl;
    private readonly ITriggeredFunctionExecutor _executor;
    private readonly TimeSpan _maximumInterval = TimeSpan.FromSeconds(5);
    private bool _disposed = false;

    private SqsListener(
        AmazonSQSClient client,
        string queueUrl,
        ITriggeredFunctionExecutor executor)
    {
        _client = client;
        _queueUrl = queueUrl;
        _executor = executor;
    }

    public static SqsListener Create(
        string accessKeyId,
        string secretAccessKey,
        string? region,
        string queueUrl,
        ITriggeredFunctionExecutor executor)
    {
        AmazonSQSClient client = region == null
            ? new(accessKeyId, secretAccessKey)
            : new(accessKeyId, secretAccessKey, ResolveRegionEndpoint(region));
        return new(client, queueUrl, executor);
    }

    private static RegionEndpoint ResolveRegionEndpoint(string region)
        => RegionEndpoint.GetBySystemName(region);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Run();
        return Task.CompletedTask;
    }

    private async void Run()
    {
        using CancellationTokenSource cancellation = new();
        CancellationToken cancellationToken = cancellation.Token;
        int delayFactor = 1;

        while (cancellationToken.IsCancellationRequested == false)
        {
            ReceiveMessageResponse response = await _client
                .ReceiveMessageAsync(_queueUrl, cancellationToken)
                .ConfigureAwait(false);

            _runningTasks.AddRange(from message in response.Messages
                                   select Process(message, cancellationToken));

            while (_runningTasks.Any())
            {
                Task task = await Task.WhenAny(_runningTasks).ConfigureAwait(false);
                _runningTasks.Remove(task);
            }

            if (response.Messages.Any())
            {
                delayFactor = 1;
                continue;
            }
            else
            {
                double millisecondsInterval = 100.0 + (Math.Pow(2, delayFactor) * (1.0 + (_random.NextDouble() * 0.01)));
                var interval = TimeSpan.FromMilliseconds(millisecondsInterval);

                if (interval < _maximumInterval)
                {
                    delayFactor++;
                }
                else
                {
                    interval = _maximumInterval;
                }

                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task Process(Message message, CancellationToken cancellationToken)
    {
        var input = new TriggeredFunctionData { TriggerValue = message };

        await _executor
            .TryExecuteAsync(input, cancellationToken)
            .ConfigureAwait(false);

        await _client
            .DeleteMessageAsync(_queueUrl, message.ReceiptHandle, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(_runningTasks);
    }

    public void Cancel()
    {
    }

    public void Dispose()
    {
        if (_disposed == false)
        {
            _client.Dispose();
            _disposed = true;
        }
    }
}

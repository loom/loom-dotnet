using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Loom.Azure.Functions.Extensions.Amazon.SQS;

internal class SqsTriggerValueProvider : IValueProvider
{
    private readonly object _value;

    public SqsTriggerValueProvider(object value) => _value = value;

    public Type Type => typeof(Message);

    public Task<object> GetValueAsync() => Task.FromResult(_value);

    public string? ToInvokeString() => _value.ToString();
}

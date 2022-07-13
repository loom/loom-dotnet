using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;

namespace Loom.Azure.Functions.Extensions.Amazon.SQS;

public class SqsTriggerExtensionConfigProvider : IExtensionConfigProvider
{
    private readonly INameResolver _nameResolver;

    public SqsTriggerExtensionConfigProvider(INameResolver nameResolver)
    {
        _nameResolver = nameResolver;
    }

    public void Initialize(ExtensionConfigContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context
            .AddBindingRule<SqsTriggerAttribute>()
            .BindToTrigger(new SqsTriggerBindingProvider(_nameResolver));
    }
}

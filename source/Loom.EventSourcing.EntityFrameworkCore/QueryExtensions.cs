namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore.Extensions.Internal;

    internal static class QueryExtensions
    {
        public static async Task ForEach<TElement>(this IQueryable<TElement> query,
                                                   Func<TElement, Task> action,
                                                   CancellationToken cancellationToken = default)
        {
            IAsyncEnumerable<TElement> enumerable = query.AsAsyncEnumerable();
            IAsyncEnumerator<TElement> enumerator = enumerable.GetEnumerator();
            while (await enumerator.MoveNext(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                await action.Invoke(enumerator.Current).ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}

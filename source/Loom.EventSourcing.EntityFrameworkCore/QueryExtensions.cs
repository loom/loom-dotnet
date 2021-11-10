using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Loom.EventSourcing.EntityFrameworkCore
{
    internal static class QueryExtensions
    {
        public static async Task ForEach<TElement>(this IQueryable<TElement> query,
                                                   Func<TElement, Task> action,
                                                   CancellationToken cancellationToken = default)
        {
            IAsyncEnumerable<TElement> enumerable = query.AsAsyncEnumerable();
            IAsyncEnumerator<TElement> enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            while (await enumerator.MoveNextAsync().ConfigureAwait(continueOnCapturedContext: false))
            {
                await action.Invoke(enumerator.Current).ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}

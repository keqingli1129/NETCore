using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public class TestAsyncEnumerable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
{
    private readonly IEnumerable<T> _enumerable;
    private readonly IQueryable<T> _queryable;

    public TestAsyncEnumerable(IEnumerable<T> enumerable)
    {
        _enumerable = enumerable;
        _queryable = enumerable as IQueryable<T> ?? enumerable.AsQueryable();
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(_enumerable.GetEnumerator());
    }

    public Type ElementType => _queryable.ElementType;
    public System.Linq.Expressions.Expression Expression => _queryable.Expression;
    public IQueryProvider Provider => new TestAsyncQueryProvider<T>(_queryable.Provider);

    public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

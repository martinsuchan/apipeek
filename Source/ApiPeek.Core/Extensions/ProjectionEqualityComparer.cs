using System;
using System.Collections.Generic;

namespace ApiPeek.Core.Extensions
{
    public static class ProjectionEqualityComparer
    {
        public static ProjectionEqualityComparer<TSource, TKey>
            Create<TSource, TKey>(Func<TSource, TKey> projection)
        {
            return new ProjectionEqualityComparer<TSource, TKey>(projection);
        }

        public static ProjectionEqualityComparer<TSource, TKey>
            Create<TSource, TKey>(TSource ignored,
                Func<TSource, TKey> projection)
        {
            return new ProjectionEqualityComparer<TSource, TKey>(projection);
        }
    }

    public static class ProjectionEqualityComparer<TSource>
    {
        public static ProjectionEqualityComparer<TSource, TKey>
            Create<TKey>(Func<TSource, TKey> projection)
        {
            return new ProjectionEqualityComparer<TSource, TKey>(projection);
        }
    }

    public class ProjectionEqualityComparer<TSource, TKey>
        : IEqualityComparer<TSource>
    {
        readonly Func<TSource, TKey> projection;
        readonly IEqualityComparer<TKey> comparer;

        public ProjectionEqualityComparer(Func<TSource, TKey> projection)
            : this(projection, null)
        {
        }

        public ProjectionEqualityComparer(
            Func<TSource, TKey> projection,
            IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
            this.projection = projection;
        }

        public bool Equals(TSource x, TSource y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            return comparer.Equals(projection(x), projection(y));
        }

        public int GetHashCode(TSource obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            return comparer.GetHashCode(projection(obj));
        }
    }
}
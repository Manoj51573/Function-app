using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DoT.Infrastructure
{
    public interface ISpecification<T>
    {
        Expression<Func<T, object>> OrderBy { get; }
        Expression<Func<T, bool>> Criteria { get; }
        List<Expression<Func<T, object>>> Includes { get; }
        List<string> IncludeStrings { get; }
        bool OrderDescending { get; }
        public int? Take { get; }
        public int? Skip { get; }
    }
}
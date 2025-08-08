using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DoT.Infrastructure;

namespace eforms_middleware.Specifications
{
    public abstract class BaseQuerySpecification<T> : ISpecification<T>
    {
        protected BaseQuerySpecification(Expression<Func<T, bool>> criteria, 
            Expression<Func<T, object>> orderBy, int? take = null, int? skip = null,
            bool orderDescending = false)
        {
            OrderBy = orderBy;
            Criteria = criteria;
            OrderDescending = orderDescending;
            Take = take;
            Skip = skip;
        }

        public Expression<Func<T, object>> OrderBy { get; }

        public bool OrderDescending { get; }
        public int? Take { get; }
        public int? Skip { get; }

        public Expression<Func<T, bool>> Criteria { get; }

        public List<Expression<Func<T, object>>> Includes { get; } = new List<Expression<Func<T, object>>>();

        public List<string> IncludeStrings { get; } = new List<string>();

        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        // string-based includes allow for including children of children
        protected virtual void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }
    }
}
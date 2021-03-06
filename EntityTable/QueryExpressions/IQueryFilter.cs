﻿namespace EntityTableService.QueryExpressions
{
    public interface IQueryFilter<T> : IQueryFilter<T, object> { }
    public interface IQueryFilter<T, P>
    {
        IFilterOperator<T> AddFilterCondition(string comparison, P value);
    }

}
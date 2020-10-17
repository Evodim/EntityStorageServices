using Microsoft.Azure.Cosmos.Table;

using System;

namespace EntityTableService.ExpressionHelpers
{
    public class TableStorageQueryBuilder<T> : QueryExpressionBuilder<T>
    {
        public TableStorageQueryBuilder(IQueryExpression<T> expression) : base(expression, new TableStorageInstructions())
        {
        }

        private string GetInstruction(string instructionName) => InstructionsProvider.Get(instructionName);

        protected override string ExpressionFilterConverter(IQueryExpression<T> expression)
        {
            return
             expression.PropertyType == typeof(byte[]) ? TableQuery.GenerateFilterConditionForBinary(expression.PropertyName, GetInstruction(expression.Comparator), (byte[])expression.PropertyValue) :
            (expression.PropertyType == typeof(bool) || expression.PropertyType == typeof(bool?)) ? TableQuery.GenerateFilterConditionForBool(expression.PropertyName, GetInstruction(expression.Comparator), (bool)expression.PropertyValue) :
            (expression.PropertyType == typeof(DateTime) || expression.PropertyType == typeof(DateTime?)) ? TableQuery.GenerateFilterConditionForDate(expression.PropertyName, GetInstruction(expression.Comparator), (DateTime)expression.PropertyValue) :
            (expression.PropertyType == typeof(DateTimeOffset) || expression.PropertyType == typeof(DateTimeOffset?)) ? TableQuery.GenerateFilterConditionForDate(expression.PropertyName, GetInstruction(expression.Comparator), (DateTimeOffset)expression.PropertyValue) :
            (expression.PropertyType == typeof(double) || expression.PropertyType == typeof(double?)) ? TableQuery.GenerateFilterConditionForDouble(expression.PropertyName, GetInstruction(expression.Comparator), (double)expression.PropertyValue) :
            (expression.PropertyType == typeof(Guid) || expression.PropertyType == typeof(Guid?)) ? TableQuery.GenerateFilterConditionForGuid(expression.PropertyName, GetInstruction(expression.Comparator), (Guid)expression.PropertyValue) :
            (expression.PropertyType == typeof(int) || expression.PropertyType == typeof(int?)) ? TableQuery.GenerateFilterConditionForInt(expression.PropertyName, GetInstruction(expression.Comparator), (int)expression.PropertyValue) :
            (expression.PropertyType == typeof(long) || expression.PropertyType == typeof(long?)) ? TableQuery.GenerateFilterConditionForLong(expression.PropertyName, GetInstruction(expression.Comparator), (long)expression.PropertyValue) :
            TableQuery.GenerateFilterCondition(expression.PropertyName, GetInstruction(expression.Comparator), expression.PropertyValue.ToString());
        }
    }
}
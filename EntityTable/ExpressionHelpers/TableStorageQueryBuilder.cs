using Microsoft.Azure.Cosmos.Table;

using System;

namespace Evod.Toolkit.Azure.Storage
{
    public class TableStorageQueryBuilder<T> : QueryExpressionBuilder<T>
    {
        public TableStorageQueryBuilder(IQueryExpression<T> expression) : base(expression, new TableStorageInstructions())
        {
        }

        private string _getInstruction(string instructionName) => InstructionsProvider.Get(instructionName);

        protected override string ExpressionFilterConverter(IQueryExpression<T> expression)
        {
            return
             expression.PropertyType == typeof(byte[]) ? TableQuery.GenerateFilterConditionForBinary(expression.PropertyName, _getInstruction(expression.Comparator), (byte[])expression.PropertyValue) :
            (expression.PropertyType == typeof(bool) || expression.PropertyType == typeof(bool?)) ? TableQuery.GenerateFilterConditionForBool(expression.PropertyName, _getInstruction(expression.Comparator), (bool)expression.PropertyValue) :
            (expression.PropertyType == typeof(DateTime) || expression.PropertyType == typeof(DateTime?)) ? TableQuery.GenerateFilterConditionForDate(expression.PropertyName, _getInstruction(expression.Comparator), (DateTime)expression.PropertyValue) :
            (expression.PropertyType == typeof(DateTimeOffset) || expression.PropertyType == typeof(DateTimeOffset?)) ? TableQuery.GenerateFilterConditionForDate(expression.PropertyName, _getInstruction(expression.Comparator), (DateTimeOffset)expression.PropertyValue) :
            (expression.PropertyType == typeof(double) || expression.PropertyType == typeof(double?)) ? TableQuery.GenerateFilterConditionForDouble(expression.PropertyName, _getInstruction(expression.Comparator), (double)expression.PropertyValue) :
            (expression.PropertyType == typeof(Guid) || expression.PropertyType == typeof(Guid?)) ? TableQuery.GenerateFilterConditionForGuid(expression.PropertyName, _getInstruction(expression.Comparator), (Guid)expression.PropertyValue) :
            (expression.PropertyType == typeof(int) || expression.PropertyType == typeof(int?)) ? TableQuery.GenerateFilterConditionForInt(expression.PropertyName, _getInstruction(expression.Comparator), (int)expression.PropertyValue) :
            (expression.PropertyType == typeof(long) || expression.PropertyType == typeof(long?)) ? TableQuery.GenerateFilterConditionForLong(expression.PropertyName, _getInstruction(expression.Comparator), (long)expression.PropertyValue) :
            TableQuery.GenerateFilterCondition(expression.PropertyName, _getInstruction(expression.Comparator), expression.PropertyValue.ToString());
        }
    }
}
using EntityTableService.ExpressionFilter;
using EntityTableService.ExpressionFilter.Abstractions;
using EntityTableService.Tests.Helpers;
using EntityTableService.Tests.Models;
using FluentAssertions;
using System;

namespace EntityTableService.Tests
{
    public class DefaultExpressionBuilder<T> : BaseQueryExpressionBuilder<T>
    {
        public DefaultExpressionBuilder() : base(new FilterExpression<T>(), new DefaultInstructionsProvider())
        {
        }
    }

    public class QueryExpressionTests
    {
        [PrettyFact(DisplayName = nameof(ShouldBuildQueryExpressionWithDefaultInstructions))]
        public void ShouldBuildQueryExpressionWithDefaultInstructions()
        {
            var builder = new DefaultExpressionBuilder<PersonEntity>();
            builder
                    .Query
                        .Where(p => p.Rank).Equal(10)
                        .And(p => p.Address.City).NotEqual("Paris")
                        .And(p => p.Created).GreaterThan(DateTimeOffset.UtcNow)
                        .And(p => p.Enabled).NotEqual(true);
            builder.Query.NextOperation.Operator.Should().Be(nameof(IQueryInstructions.And));
            var result = builder.Build();
            result.Should().NotBeNullOrEmpty();
        }

        [PrettyFact(DisplayName = nameof(ShouldBuildHybridQueryExpressionWithDefaultInstructions))]
        public void ShouldBuildHybridQueryExpressionWithDefaultInstructions()
        {
            var builder = new DefaultExpressionBuilder<PersonEntity>();
            builder.Query
            .Where("Rowkey").Equal("$Id-%+c5JcwURUajaem4NtAapw")
            .And(p => p.Address.City).NotEqual("Paris")
            .And("Created").GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))
            .And(p => p.Enabled).NotEqual(true);
            var result = builder.Build();

            result.Should().Be("Rowkey Equal '$Id-%+c5JcwURUajaem4NtAapw' And City NotEqual 'Paris' And Created GreaterThan '21/04/2012 18:25:43 +00:00' And Enabled NotEqual 'True'");
        }

        [PrettyFact(DisplayName = nameof(ShouldBuildGroupQueryExpressionWithDefaultInstructions))]
        public void ShouldBuildGroupQueryExpressionWithDefaultInstructions()
        {
            var builder = new DefaultExpressionBuilder<PersonEntity>();

            builder.Query
           .Where(p => p.AccountId).Equal("10")
            .And(g => g
                .Where(p => p.Created).GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))
                .And(p => p.LastName).Equal("test")
                .Or(p => p.Created).LessThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z")))
            .Not(p => p.Enabled).Equal(true)
            .And(g => g
                    .Where(p => p.Created).GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))
                    .Or(p => p.Created).LessThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z")));
            var queryStr = builder.Build();

            queryStr.Trim()
                .Should()
                .Be("AccountId Equal '10' And (Created GreaterThan '21/04/2012 18:25:43 +00:00' And LastName Equal 'test' Or Created LessThan '21/04/2012 18:25:43 +00:00') Not Enabled Equal 'True' And (Created GreaterThan '21/04/2012 18:25:43 +00:00' Or Created LessThan '21/04/2012 18:25:43 +00:00')");
        }

        [PrettyFact(DisplayName = nameof(ShouldBuildTableStorageQueryExpression))]
        public void ShouldBuildTableStorageQueryExpression()
        {
            var builder = new TableStorageQueryBuilder<PersonEntity>(new FilterExpression<PersonEntity>());

            builder.Query
           .Where("PartitionKey").Equal("Account-1")
           .And(p => p.AccountId).Equal("10")
           .And(g => g
              .Where(p => p.Created).GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))
              .Or(p => p.Created).LessThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z")))
           .Not(p => p.Enabled).Equal(true);

            var queryStr = builder.Build();

            queryStr.Trim()
                .Should()
                .Be("PartitionKey eq 'Account-1' and AccountId eq '10' and (Created gt datetime'2012-04-21T18:25:43.0000000Z' or Created lt datetime'2012-04-21T18:25:43.0000000Z') not Enabled eq true");
        }
    }
}
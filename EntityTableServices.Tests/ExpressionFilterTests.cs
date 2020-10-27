﻿using EntityTableService.AzureClient;
using EntityTableService.QueryExpressions; 
using EntityTableService.Tests.Helpers;
using EntityTableService.Tests.Models;
using FluentAssertions;
using System;

namespace EntityTableService.Tests
{

    public class QueryExpressionTests
    {
        [PrettyFact(DisplayName = nameof(Should_Build_Query_Expression_With_Default_Instructions))]
        public void Should_Build_Query_Expression_With_Default_Instructions()
        {
            var builder = new MockedExpressionBuilder<PersonEntity>();
            builder
            .Query
                .Where(p => p.Rank).Equal(10)
                .And(p => p.Address.City).NotEqual("Paris")
                .And(p => p.Created).GreaterThan(DateTimeOffset.UtcNow)
                .And(p => p.Enabled).NotEqual(true);

            builder.Query.NextOperation.Operator.Should().Be("And");
            var result = builder.Build();
            result.Should().NotBeNullOrEmpty();
        }

        [PrettyFact(DisplayName = nameof(Should_Build_Mixed_Query_Selector_Expression_With_Default_Instructions))]
        public void Should_Build_Mixed_Query_Selector_Expression_With_Default_Instructions()
        {
            var builder = new MockedExpressionBuilder<PersonEntity>();
            builder.Query
            //RowKey is a native prop of Azyre storage ITableEntiy
            .Where("Rowkey").Equal("$Id-%+c5JcwURUajaem4NtAapw")
            .And(p => p.Address.City).NotEqual("Paris")

            //Created is an entity prop wish could be requested by string or prop selector
            .And("Created").GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))
            .And(p => p.Created).GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))

            //_MoreThanOneAddress is a dynamic prop
            .And("_MoreThanOneAddress").GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))
            .And(p => p.Enabled).NotEqual(true);
            var result = builder.Build();

            result.Should().Be("Rowkey Equal '$Id-%+c5JcwURUajaem4NtAapw' And City NotEqual 'Paris' And Created GreaterThan '21/04/2012 18:25:43 +00:00' And Created GreaterThan '21/04/2012 18:25:43 +00:00' And _MoreThanOneAddress GreaterThan '21/04/2012 18:25:43 +00:00' And Enabled NotEqual 'True'");
        }

        [PrettyFact(DisplayName = nameof(Should_BuildGroup_Query_Expression_With_DefaultInstructions))]
        public void Should_BuildGroup_Query_Expression_With_DefaultInstructions()
        {
            var builder = new MockedExpressionBuilder<PersonEntity>();

            builder.Query
           .Where(p => p.AccountId).Equal("10")
            .And(p => p
                .Where(p => p.Created).GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))
                .And(p => p.LastName).Equal("test")
                .Or(p => p.Created).LessThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z")))
            .Not(p => p.Enabled).Equal(true)
            .And(p => p
                    .Where(p => p.Created).GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))
                    .Or(p => p.Created).LessThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z")));
            var queryStr = builder.Build();

            queryStr.Trim()
                .Should()
                .Be("AccountId Equal '10' And (Created GreaterThan '21/04/2012 18:25:43 +00:00' And LastName Equal 'test' Or Created LessThan '21/04/2012 18:25:43 +00:00') Not Enabled Equal 'True' And (Created GreaterThan '21/04/2012 18:25:43 +00:00' Or Created LessThan '21/04/2012 18:25:43 +00:00')");
        }

        [PrettyFact(DisplayName = nameof(Should_Build_TableStorage_Query_Expression))]
        public void Should_Build_TableStorage_Query_Expression()
        {
            var builder = new TableStorageQueryBuilder<PersonEntity>(new FilterExpression<PersonEntity>());

            builder.Query
           .Where("PartitionKey").Equal("Account-1")
           .And(p => p.AccountId).Equal("10")
           .And(p => p
              .Where(p => p.Created).GreaterThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z"))
              .Or(p => p.Created).LessThan(DateTimeOffset.Parse("2012-04-21T18:25:43Z")))
           .Not(p => p.Enabled).Equal(true);

            var queryStr = builder.Build();

            queryStr.Trim()
                .Should()
                .Be("PartitionKey eq 'Account-1' and AccountId eq '10' and (Created gt datetime'2012-04-21T18:25:43.0000000Z' or Created lt datetime'2012-04-21T18:25:43.0000000Z') not Enabled eq true");
        }

        [PrettyFact(DisplayName = nameof(Should_Build_Table_Storage_Advanced_Query_Expression))]
        public void Should_Build_Table_Storage_Advanced_Query_Expression()
        {
            var builder = new TableStorageQueryBuilder<PersonEntity>(new FilterExpression<PersonEntity>());

            builder.Query
           .Where("PartitionKey").Equal("Account-1")
           .And(p => p.AccountId).Equal("10")
           .And(p => p.Genre).Equal(Genre.Female);

            var queryStr = builder.Build();

            queryStr.Trim()
                .Should()
                .Be("PartitionKey eq 'Account-1' and AccountId eq '10' and Genre eq 'Female'");
        }
    }
}
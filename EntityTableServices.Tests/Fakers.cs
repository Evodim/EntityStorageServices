using Bogus;
using EntityTableService.Tests.Models;
using System;

namespace EntityTableService.Tests
{
    public static class Fakers
    {
        public static Faker<PersonEntity> CreateFakedPerson()
        {
            var rankid = 0;

            var testPerson = new Faker<PersonEntity>()
            //Ensure all properties have rules. By default, StrictMode is false
            //Set a global policy by using Faker.DefaultStrictMode
            .StrictMode(true)
            //OrderId is deterministic
            .RuleFor(p => p.AccountId, f => f.PickRandom<string>("1", "2", "3", "4", "5", "6", "7", "8", "9"))
            .RuleFor(p => p.PersonId, f => Guid.NewGuid())
            .RuleFor(p => p.Rank, f => rankid++)
            .RuleFor(p => p.Address, f => FakedAddress())

            //A nullable int? with 80% probability of being null.
            //The .OrNull extension is in the Bogus.Extensions namespace.
            .RuleFor(p => p.OtherAddress, f => FakedAddress().Generate(5))
            .RuleFor(p => p.Created, f => f.Date.BetweenOffset(DateTime.UtcNow.AddYears(-4), DateTime.UtcNow))
            .RuleFor(p => p.Enabled, f => f.Random.Bool())
            .RuleFor(p => p.FirstName, f => f.Person.FirstName)
            .RuleFor(p => p.LastName, f => f.Person.LastName)
            .RuleFor(p => p.Longitude, f => f.Random.Double())
            .RuleFor(p => p.Latitude, f => f.Random.Double())
            .RuleFor(p => p.Distance, f => f.Random.Decimal()); 

            return testPerson;
        }

        public static Faker<Address> FakedAddress()
        {
            var addressTest = new Faker<Address>();
            addressTest.RuleFor(a => a.ZipCode, f => f.Address.ZipCode());
            addressTest.RuleFor(a => a.Street, f => $"{f.Address.StreetName()} {f.Address.StreetSuffix()} {f.Address.StreetAddress()}");
            addressTest.RuleFor(a => a.State, f => f.Address.State());
            addressTest.RuleFor(a => a.Country, f => f.Address.Country());
            addressTest.RuleFor(a => a.City, f => f.Address.City());
            addressTest.RuleFor(a => a.AdressType, f => f.PickRandom<AdressType>());
            return addressTest;
        }
    }
}
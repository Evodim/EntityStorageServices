using Bogus;
using Bogus.Extensions;
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
            
            .StrictMode(true) 
            .RuleFor(p => p.AccountId, f => f.PickRandom("1", "2", "3", "4", "5", "6", "7", "8", "9"))
            .RuleFor(p => p.PersonId, f => Guid.NewGuid())
            .RuleFor(p => p.Rank, f => rankid++)
            .RuleFor(p => p.Address, f => FakedAddress()) 
            .RuleFor(p => p.OtherAddress, f => FakedAddress().Generate(5))
            .RuleFor(p => p.Created, f => f.Date.BetweenOffset(DateTime.UtcNow.AddYears(-4), DateTime.UtcNow))
            .RuleFor(p => p.Updated, f => null)
            .RuleFor(p => p.Enabled, f => f.Random.Bool())
            .RuleFor(p => p.FirstName, f => f.Person.FirstName)
            .RuleFor(p => p.LastName, f => f.Person.LastName)
            .RuleFor(p => p.Longitude, f => f.Random.Double())
            .RuleFor(p => p.Latitude, f => f.Random.Double())
            .RuleFor(p => p.Distance, f => f.Random.Decimal())
            .RuleFor(p => p.Altitude, f => f.Random.Decimal().OrNull(f))
            .RuleFor(p => p.Genre, f => f.Random.Enum<Genre>())
            .RuleFor(p => p.Situation, f => f.Random.Enum<Situation>())
            .RuleFor(p => p.BankAmount, f => f.Random.Float())
            .RuleFor(p => p.ConsentDate, f => f.Date.Past())
            .RuleFor(p => p.BirthDate, f => f.Person.DateOfBirth);

            return testPerson;
        }

        public static Faker<Address> FakedAddress()
        {
            var addressTest = new Faker<Address>()
            .RuleFor(a => a.ZipCode, f => f.Address.ZipCode())
            .RuleFor(a => a.Street, f => $"{f.Address.StreetName()} {f.Address.StreetSuffix()} {f.Address.StreetAddress()}")
            .RuleFor(a => a.State, f => f.Address.State())
            .RuleFor(a => a.Country, f => f.Address.Country())
            .RuleFor(a => a.City, f => f.Address.City())
            .RuleFor(a => a.AdressType, f => f.PickRandom<AdressType>());
            return addressTest;
        }
    }
}
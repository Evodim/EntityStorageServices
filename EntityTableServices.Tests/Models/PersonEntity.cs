using System;
using System.Collections.Generic;

namespace EntityTableService.Tests.Models
{
    public class PersonEntity
    {
        public PersonEntity()
        {
        }

        public string AccountId { get; set; }

        public PersonEntity(Guid id, string name)
        {
            PersonId = id;
            FirstName = name;
        }

        public DateTimeOffset? Created { get; set; }
        public bool? Enabled { get; set; }
        public Address Address { get; set; }
        public List<Address> OtherAddress { get; set; }
        public Guid PersonId { get; set; }
        public string FirstName { get; set; }
        public int? Rank { get; set; }
        public string LastName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal Distance { get; set; }
        public string Type => nameof(PersonEntity);
    }
}
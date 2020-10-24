using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EntityTableService.Tests.Models
{
    public enum Genre { 
    Male,
    Female
    }

    public enum Situation
    {
        Single,
        Married,
        Divorced        
    }
    public struct GeoPosition
    { 
    public double Latitude { get; set; }
    public double Longitude { get; set; }
     }
    public class PersonEntity
    {
        public string AccountId { get; set; }
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
        public decimal? Precision  { get; set; }
        public float? BankAmount { get; set; }
        public string Type => nameof(PersonEntity);
        public Genre Genre { get; set; }
        public Situation? Situation { get; set; }        

    }
}
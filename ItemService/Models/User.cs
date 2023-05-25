using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class User
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public Int32 MobilNummer { get; set; }

    public Address? adress {get; set;}
    
}
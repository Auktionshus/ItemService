using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Address

{
    public string? SteetName {get; set;}
    public int HouseNumber {get; set;}
    public int Zipcode {get; set;}

    public string? City { get; set;}
}

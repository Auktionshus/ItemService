using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Bid
{
    public Guid Id { get; set; }
    public string? Bidder { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

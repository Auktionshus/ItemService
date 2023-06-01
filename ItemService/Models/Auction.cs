using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Auction
{
    public Guid Id { get; set; }
    public Item Item { get; set; }
    public User HighestBidder { get; set; }
    public List<Bid> Bids { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
}

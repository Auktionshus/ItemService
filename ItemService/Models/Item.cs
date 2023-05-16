using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Item
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public String Catagory { get; set; }
    public DateTime Date { get; set; }
}



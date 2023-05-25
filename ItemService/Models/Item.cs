using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Item
{
    public Guid Id { get; set; }

    public string? Title { get; set; }
    public string? Brand { get; set; }
    public string? Description { get; set; }

    public decimal StartingPrice { get; set; }

    public string? ImageFileId { get; set; }

    public Category? category { get; set; }
    public string? Location { get; set; }

    public User? user { get; set; }
}


using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class ImageRecord
{
    public Guid Id { get; set; }
    public Uri Location { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public string AddedBy { get; set; }
}
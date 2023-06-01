using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class ItemDTO
{
    public Guid Seller { get; set; }
    public string Title { get; set; }
    public string Brand { get; set; }
    public string Description { get; set; }
    public string CategoryCode { get; set; }
    public string Location { get; set; }
}

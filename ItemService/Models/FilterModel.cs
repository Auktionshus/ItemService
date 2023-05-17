using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class FilterModel
{
    public string Category { get; set; }
    public string Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
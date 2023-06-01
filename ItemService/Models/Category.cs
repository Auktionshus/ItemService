using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Category
{
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryDescription { get; set; }
}

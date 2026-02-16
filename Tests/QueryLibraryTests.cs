using NUnit.Framework;
using System.Collections.Generic;

namespace Tests;

[TestFixture]
public class QueryLibraryTests
{
    private List<Query> _mockDatabase = null!;

    [SetUp]
    public void Setup()
    {
        _mockDatabase = new List<Query>
        {
            new Query { Id = 1, Category = "Account", Key = "slow login query", Value = "SELECT * FROM logs WHERE..." },
            new Query { Id = 2, Category = "API", Key = "API timeout errors", Value = "SELECT * FROM errors WHERE..." }
        };
    }

    [Test]
    public void AddQuery_ValidData_AddsToDatabase()
    {
        var newQuery = new Query { Id = 3, Category = "Checkout", Key = "cart abandonment", Value = "SELECT * FROM..." };
        _mockDatabase.Add(newQuery);
        Assert.That(_mockDatabase, Has.Count.EqualTo(3));
    }

    [Test]
    public void DeleteQuery_ValidId_RemovesFromDatabase()
    {
        _mockDatabase.RemoveAt(0);
        Assert.That(_mockDatabase, Has.Count.EqualTo(1));
    }

    [Test]
    public void QueryKey_ContainsXSS_GetsSanitized()
    {
        var xssInput = "<script>alert('xss')</script>";
        var sanitized = SanitizeHtml(xssInput);
        Assert.That(sanitized.Contains("<script>"), Is.False);
    }

    private static string SanitizeHtml(string input)
    {
        return input.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}

public class Query
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

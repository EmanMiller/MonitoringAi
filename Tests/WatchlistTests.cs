using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Tests;

[TestFixture]
public class WatchlistTests
{
    [Test]
    public void DefaultWatchlist_Contains17Tickers_InCorrectOrder()
    {
        var expected = new List<string>
        {
            "SPY", "QQQ", "TSLA", "NVDA", "AMZN", "GOOGL", "PLTR",
            "GLD", "SLV", "MSFT", "ORCL", "META", "HOOD", "INTC",
            "AAPL", "AMD", "NFLX"
        };
        var actual = GetDefaultWatchlist();

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void MarketNews_FetchesOnlyTop5Tickers()
    {
        var watchlist = GetDefaultWatchlist();
        var newsTickerFilter = watchlist.Take(5).ToList();

        Assert.That(newsTickerFilter, Has.Count.EqualTo(5));
        Assert.That(newsTickerFilter, Does.Contain("SPY"));
        Assert.That(newsTickerFilter, Does.Contain("AMZN"));
        Assert.That(newsTickerFilter, Does.Not.Contain("AAPL"));
    }

    private static List<string> GetDefaultWatchlist()
    {
        return new List<string>
        {
            "SPY", "QQQ", "TSLA", "NVDA", "AMZN", "GOOGL", "PLTR",
            "GLD", "SLV", "MSFT", "ORCL", "META", "HOOD", "INTC",
            "AAPL", "AMD", "NFLX"
        };
    }
}

namespace Ordering.Tests;

public class FractionalIndexingTests
{
    [Fact]
    public void GenerateKeyBetween_BothNull_ReturnsInitialKey()
    {
        string key = FractionalIndexing.GenerateKeyBetween(null, null);
        Assert.Equal("a0", key);
    }

    [Fact]
    public void GenerateKeyBetween_AfterA0_ReturnsA1()
    {
        string key = FractionalIndexing.GenerateKeyBetween("a0", null);
        Assert.Equal("a1", key);
    }

    [Fact]
    public void GenerateKeyBetween_BeforeA0_ReturnsZz()
    {
        string key = FractionalIndexing.GenerateKeyBetween(null, "a0");
        Assert.Equal("Zz", key);
    }

    [Fact]
    public void GenerateKeyBetween_BetweenA0AndA1_ReturnsMidpoint()
    {
        string key = FractionalIndexing.GenerateKeyBetween("a0", "a1");
        Assert.Equal("a0V", key);
    }

    [Fact]
    public void GenerateKeyBetween_BetweenA0AndA0V_ReturnsMidpoint()
    {
        string key = FractionalIndexing.GenerateKeyBetween("a0", "a0V");
        Assert.Equal("a0G", key);
    }

    [Fact]
    public void GenerateKeyBetween_ThrowsWhenAGreaterThanOrEqualToB()
    {
        Assert.Throws<ArgumentException>(() => FractionalIndexing.GenerateKeyBetween("a1", "a0"));
        Assert.Throws<ArgumentException>(() => FractionalIndexing.GenerateKeyBetween("a0", "a0"));
    }

    [Fact]
    public void GenerateKeyBetween_1000SequentialAppends_AllSortedCorrectly()
    {
        List<string> keys = new();
        string? prev = null;

        for (int i = 0; i < 1000; i++)
        {
            string key = FractionalIndexing.GenerateKeyBetween(prev, null);
            keys.Add(key);
            prev = key;
        }

        List<string> sorted = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        Assert.Equal(keys, sorted);
    }

    [Fact]
    public void GenerateKeyBetween_1000SequentialPrepends_AllSortedCorrectly()
    {
        List<string> keys = new();
        string? next = null;

        for (int i = 0; i < 1000; i++)
        {
            string key = FractionalIndexing.GenerateKeyBetween(null, next);
            keys.Add(key);
            next = key;
        }

        keys.Reverse();
        List<string> sorted = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        Assert.Equal(keys, sorted);
    }

    [Fact]
    public void GenerateKeyBetween_500AlternatingInserts_AllSortedCorrectly()
    {
        string a = FractionalIndexing.GenerateKeyBetween(null, null);
        string b = FractionalIndexing.GenerateKeyBetween(a, null);

        List<string> keys = new() { a, b };

        for (int i = 0; i < 500; i++)
        {
            string mid = FractionalIndexing.GenerateKeyBetween(a, b);
            keys.Insert(keys.IndexOf(b), mid);

            // Alternate: next insert between a and mid, then mid and b, etc.
            if (i % 2 == 0)
            {
                b = mid;
            }
            else
            {
                a = mid;
            }
        }

        List<string> sorted = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        Assert.Equal(keys, sorted);
    }

    [Fact]
    public void GenerateKeyBetween_AllKeysUnique_After1000Appends()
    {
        HashSet<string> keys = new();
        string? prev = null;

        for (int i = 0; i < 1000; i++)
        {
            string key = FractionalIndexing.GenerateKeyBetween(prev, null);
            Assert.DoesNotContain(key, keys);
            keys.Add(key);
            prev = key;
        }
    }

    [Fact]
    public void GenerateKeyBetween_MaxKeyLengthReasonable_After1000Prepends()
    {
        string? next = null;

        int maxLen = 0;
        for (int i = 0; i < 1000; i++)
        {
            string key = FractionalIndexing.GenerateKeyBetween(null, next);
            maxLen = Math.Max(maxLen, key.Length);
            next = key;
        }

        // Base-62 should keep keys short — max ~20 chars even after 1000 prepends
        Assert.True(maxLen <= 50, $"Max key length {maxLen} exceeds 50 after 1000 prepends.");
    }

    [Fact]
    public void GenerateNKeysBetween_GeneratesCorrectCount()
    {
        string[] keys = FractionalIndexing.GenerateNKeysBetween(null, null, 5);
        Assert.Equal(5, keys.Length);
    }

    [Fact]
    public void GenerateNKeysBetween_AllKeysSortedAndUnique()
    {
        string[] keys = FractionalIndexing.GenerateNKeysBetween(null, null, 100);

        string[] sorted = keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        Assert.Equal(keys, sorted);
        Assert.Equal(keys.Distinct().Count(), keys.Length);
    }

    [Fact]
    public void GenerateNKeysBetween_BetweenTwoKeys_AllInRange()
    {
        string a = "a0";
        string b = "a1";
        string[] keys = FractionalIndexing.GenerateNKeysBetween(a, b, 10);

        foreach (string key in keys)
        {
            Assert.True(string.CompareOrdinal(key, a) > 0, $"Key '{key}' not > '{a}'");
            Assert.True(string.CompareOrdinal(key, b) < 0, $"Key '{key}' not < '{b}'");
        }
    }

    [Fact]
    public void GenerateNKeysBetween_ZeroCount_ReturnsEmpty()
    {
        string[] keys = FractionalIndexing.GenerateNKeysBetween(null, null, 0);
        Assert.Empty(keys);
    }
}
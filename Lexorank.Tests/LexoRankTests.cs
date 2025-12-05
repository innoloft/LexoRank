namespace LexoRank.Tests;

public class LexoRankTests
{
    [Fact]
    public void TestMin()
    {
        var min = LexoRank.Min();
        Assert.Equal("0|000000:", min.ToString());
    }

    [Fact]
    public void TestMax()
    {
        var max = LexoRank.Max();
        Assert.Equal("0|zzzzzz:", max.ToString());
    }

    [Fact]
    public void TestMiddle()
    {
        var middle = LexoRank.Middle();
        Assert.Equal("0|hzzzzz:", middle.ToString());
    }

    [Fact]
    public void TestNext()
    {
        var middle = LexoRank.Middle();
        var next = middle.GenNext();
        Assert.True(next.CompareTo(middle) > 0);
    }

    [Fact]
    public void TestPrev()
    {
        var middle = LexoRank.Middle();
        var prev = middle.GenPrev();
        Assert.True(prev.CompareTo(middle) < 0);
    }

    [Fact]
    public void TestBetween()
    {
        var min = LexoRank.Min();
        var max = LexoRank.Max();
        var between = min.Between(max);
        Assert.True(between.CompareTo(min) > 0);
        Assert.True(between.CompareTo(max) < 0);
    }

    [Fact]
    public void TestParse()
    {
        var rankStr = "0|hzzzzz:";
        var rank = LexoRank.Parse(rankStr);
        Assert.Equal(rankStr, rank.ToString());
    }

    [Fact]
    public void TestFromTimestamp()
    {
        var now = DateTime.UtcNow;
        var rank = LexoRank.FromTimestamp(now);

        Assert.NotNull(rank);
        Assert.Equal(LexoRankBucket.Bucket0, rank.Bucket);

        // Ensure order is preserved
        var later = now.AddSeconds(1);
        var laterRank = LexoRank.FromTimestamp(later);

        Assert.True(laterRank.CompareTo(rank) > 0);
    }

    [Fact]
    public void TestFromTimestampWithBucket()
    {
        var now = DateTime.UtcNow;
        var rank = LexoRank.FromTimestamp(now, LexoRankBucket.Bucket1);

        Assert.Equal(LexoRankBucket.Bucket1, rank.Bucket);
    }

    [Fact]
    public void TestCalculateBetween()
    {
        var min = LexoRank.Min();
        var max = LexoRank.Max();

        var between = LexoRank.CalculateBetween(min.ToString(), max.ToString());
        Assert.True(between.CompareTo(min) > 0);
        Assert.True(between.CompareTo(max) < 0);

        var start = LexoRank.CalculateBetween(null, max.ToString());
        Assert.True(start.CompareTo(max) < 0);

        var end = LexoRank.CalculateBetween(min.ToString(), null);
        Assert.True(end.CompareTo(min) > 0);

        var middle = LexoRank.CalculateBetween(null, null);
        Assert.Equal(LexoRank.Middle(), middle);
    }

    [Fact]
    public void TestParseInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => LexoRank.Parse(null!));
        Assert.Throws<ArgumentException>(() => LexoRank.Parse(""));
        Assert.Throws<ArgumentException>(() => LexoRank.Parse("   "));
        Assert.Throws<ArgumentException>(() => LexoRank.Parse("invalid"));
        Assert.Throws<ArgumentException>(() => LexoRank.Parse("0|invalid!"));
        Assert.Throws<ArgumentException>(() => LexoRank.Parse("3|000000:")); // Invalid bucket
    }

    [Fact]
    public void TestCalculateBetweenEdgeCases()
    {
        var min = LexoRank.Min();
        var max = LexoRank.Max();

        // Test Min/Min
        Assert.Throws<LexoException>(() => LexoRank.CalculateBetween(min.ToString(), min.ToString()));

        // Test Max/Max
        Assert.Throws<LexoException>(() => LexoRank.CalculateBetween(max.ToString(), max.ToString()));

        // Test before Min
        Assert.Throws<LexoException>(() => LexoRank.CalculateBetween(null, min.ToString()));

        // Test after Max
        Assert.Throws<LexoException>(() => LexoRank.CalculateBetween(max.ToString(), null));
    }

    [Fact]
    public void TestBucketOperations()
    {
        var rank = LexoRank.Middle();
        var bucket0 = LexoRankBucket.Bucket0;
        var bucket1 = LexoRankBucket.Bucket1;
        var bucket2 = LexoRankBucket.Bucket2;

        Assert.Equal(bucket0, rank.Bucket);

        var nextBucketRank = rank.InNextBucket();
        Assert.Equal(bucket1, nextBucketRank.Bucket);
        Assert.Equal(rank.Decimal, nextBucketRank.Decimal);

        var nextNextBucketRank = nextBucketRank.InNextBucket();
        Assert.Equal(bucket2, nextNextBucketRank.Bucket);

        var prevBucketRank = nextNextBucketRank.InPrevBucket();
        Assert.Equal(bucket1, prevBucketRank.Bucket);
    }

    [Fact]
    public void TestFromTimestampOverflow()
    {
        // DateTime.MaxValue ticks is 3155378975999999999
        // MaxDecimal is roughly 2176782336
        // This should not throw and return a valid rank
        var rank = LexoRank.FromTimestamp(DateTime.MaxValue);
        Assert.NotNull(rank);
        Assert.True(rank.CompareTo(LexoRank.Max()) <= 0);
        Assert.True(rank.CompareTo(LexoRank.Min()) >= 0);
    }

    [Fact]
    public async Task TestConcurrentBetweenOperations()
    {
        // Test that Between operations are thread-safe
        var min = LexoRank.Min();
        var max = LexoRank.Max();
        var tasks = new List<Task<LexoRank>>();
        var iterations = 50;

        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() => min.Between(max)));
        }

        await Task.WhenAll(tasks);

        // All results should be valid and identical (same inputs produce same output)
        var results = tasks.Select(t => t.Result).ToList();
        Assert.Equal(iterations, results.Count);
        Assert.All(results, rank =>
        {
            Assert.True(rank.CompareTo(min) > 0);
            Assert.True(rank.CompareTo(max) < 0);
        });

        // All results should be equal since inputs are the same
        var firstResult = results[0];
        Assert.All(results, rank => Assert.Equal(firstResult.ToString(), rank.ToString()));
    }

    [Fact]
    public async Task TestConcurrentParsing()
    {
        // Test that parsing is thread-safe
        var rankStrings = new[]
        {
                "0|000000:",
                "0|hzzzzz:",
                "0|zzzzzz:",
                "1|100000:",
                "2|y00000:"
            };

        var tasks = new List<Task<LexoRank>>();

        foreach (var rankStr in rankStrings)
        {
            for (int i = 0; i < 20; i++)
            {
                var str = rankStr; // Capture for closure
                tasks.Add(Task.Run(() => LexoRank.Parse(str)));
            }
        }

        await Task.WhenAll(tasks);

        // All tasks should complete successfully
        Assert.Equal(rankStrings.Length * 20, tasks.Count);
        Assert.All(tasks, task => Assert.NotNull(task.Result));
    }

    [Fact]
    public async Task TestConcurrentGenNextPrev()
    {
        // Test that GenNext and GenPrev are thread-safe
        var middle = LexoRank.Middle();
        var nextTasks = new List<Task<LexoRank>>();
        var prevTasks = new List<Task<LexoRank>>();
        var iterations = 50;

        for (int i = 0; i < iterations; i++)
        {
            nextTasks.Add(Task.Run(() => middle.GenNext()));
            prevTasks.Add(Task.Run(() => middle.GenPrev()));
        }

        await Task.WhenAll(nextTasks.Concat(prevTasks));

        // All tasks should complete successfully
        Assert.Equal(iterations, nextTasks.Count);
        Assert.Equal(iterations, prevTasks.Count);
        Assert.All(nextTasks, task => Assert.NotNull(task.Result));
        Assert.All(prevTasks, task => Assert.NotNull(task.Result));

        // Verify results are consistent
        var nextResults = nextTasks.Select(t => t.Result).ToList();
        var prevResults = prevTasks.Select(t => t.Result).ToList();

        // All GenNext results should be identical
        Assert.All(nextResults, rank => Assert.Equal(nextResults[0].ToString(), rank.ToString()));

        // All GenPrev results should be identical
        Assert.All(prevResults, rank => Assert.Equal(prevResults[0].ToString(), rank.ToString()));
    }

    [Fact]
    public async Task TestConcurrentCalculateBetween()
    {
        // Test that CalculateBetween is thread-safe
        var minStr = LexoRank.Min().ToString();
        var maxStr = LexoRank.Max().ToString();
        var tasks = new List<Task<LexoRank>>();
        var iterations = 50;

        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() => LexoRank.CalculateBetween(minStr, maxStr)));
            tasks.Add(Task.Run(() => LexoRank.CalculateBetween(null, maxStr)));
            tasks.Add(Task.Run(() => LexoRank.CalculateBetween(minStr, null)));
        }

        await Task.WhenAll(tasks);

        // All tasks should complete successfully
        Assert.Equal(iterations * 3, tasks.Count);
        Assert.All(tasks, task => Assert.NotNull(task.Result));
    }
}
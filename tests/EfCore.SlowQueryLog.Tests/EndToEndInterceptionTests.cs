using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// End-to-end integration tests that verify the SlowQueryInterceptor works correctly
/// within the Entity Framework Core pipeline.
/// </summary>
public class EndToEndInterceptionTests
{
    private class Blog
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
    }

    private class BlogContext : DbContext
    {
        public BlogContext(DbContextOptions options) : base(options) { }
        public DbSet<Blog> Blogs => Set<Blog>();
    }

    /// <summary>
    /// Verifies that the SlowQueryInterceptor executes within the EF Core pipeline
    /// and correctly tracks slow queries.
    /// </summary>
    [Fact]
    public void Interceptor_runs_inside_ef_pipeline()
    {
        // Threshold of zero would be rejected, so use the smallest allowed and treat
        // every query as slow to prove the pipeline wiring works end to end.
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromTicks(1),
        });

        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BlogContext>()
            .UseSqlite(connection)
            .UseSlowQueryLog(interceptor)
            .Options;

        using var ctx = new BlogContext(options);
        ctx.Database.EnsureCreated();
        ctx.Blogs.Add(new Blog { Title = "hello" });
        ctx.SaveChanges();

        _ = ctx.Blogs.Where(b => b.Title == "hello").ToList();

        Assert.True(interceptor.Ranking.Count > 0);
    }
}

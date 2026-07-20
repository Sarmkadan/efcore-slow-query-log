using System;
using Xunit;
using EfCore.SlowQueryLog.Options;

namespace EfCore.SlowQueryLog.Tests
{
    public class SlowQueryLogOptionsValidationTests
    {
        [Fact]
        public void ValidOptions_PassValidation()
        {
            var options = new SlowQueryLogOptions
            {
                Threshold = TimeSpan.FromMilliseconds(100),
                RankingCapacity = 10
            };

            // Validate should return no errors
            var errors = options.Validate();
            Assert.Empty(errors);

            // IsValid should be true
            Assert.True(options.IsValid());

            // EnsureValid should not throw
            var exception = Record.Exception(() => options.EnsureValid());
            Assert.Null(exception);
        }

        [Fact]
        public void NegativeOrZeroThreshold_Rejected()
        {
            var options = new SlowQueryLogOptions
            {
                Threshold = TimeSpan.Zero,
                RankingCapacity = 10
            };

            var errors = options.Validate();
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains(nameof(SlowQueryLogOptions.Threshold)));

            Assert.False(options.IsValid());

            Assert.Throws<ArgumentException>(() => options.EnsureValid());
        }

        [Fact]
        public void InvalidRankingCapacity_Rejected()
        {
            var options = new SlowQueryLogOptions
            {
                Threshold = TimeSpan.FromMilliseconds(100),
                RankingCapacity = 0
            };

            var errors = options.Validate();
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains(nameof(SlowQueryLogOptions.RankingCapacity)));

            Assert.False(options.IsValid());

            Assert.Throws<ArgumentException>(() => options.EnsureValid());
        }

        [Fact]
        public void DefaultOptions_AreValid()
        {
            var options = new SlowQueryLogOptions();

            var errors = options.Validate();
            Assert.Empty(errors);

            Assert.True(options.IsValid());

            var exception = Record.Exception(() => options.EnsureValid());
            Assert.Null(exception);
        }
    }
}

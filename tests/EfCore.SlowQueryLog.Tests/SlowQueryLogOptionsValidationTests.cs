using System;
using Xunit;
using EfCore.SlowQueryLog.Options;

namespace EfCore.SlowQueryLog.Tests
{
    /// <summary>
    /// Tests for validating <see cref="SlowQueryLogOptions"/> configuration.
    /// </summary>
    public class SlowQueryLogOptionsValidationTests
    {
        /// <summary>
        /// Verifies that a valid <see cref="SlowQueryLogOptions"/> instance passes validation,
        /// reports no errors, and can be ensured without throwing.
        /// </summary>
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

        /// <summary>
        /// Ensures that a zero or negative <see cref="SlowQueryLogOptions.Threshold"/> is rejected
        /// by validation and causes <see cref="SlowQueryLogOptions.EnsureValid"/> to throw an
        /// <see cref="ArgumentException"/>.
        /// </summary>
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

        /// <summary>
        /// Verifies that a non‑positive <see cref="SlowQueryLogOptions.RankingCapacity"/> is rejected
        /// by validation and causes <see cref="SlowQueryLogOptions.EnsureValid"/> to throw an
        /// <see cref="ArgumentException"/>.
        /// </summary>
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

        /// <summary>
        /// Confirms that the default constructor of <see cref="SlowQueryLogOptions"/> produces a
        /// configuration that passes validation and can be ensured without throwing.
        /// </summary>
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

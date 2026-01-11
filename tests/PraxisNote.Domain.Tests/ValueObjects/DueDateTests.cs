using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Domain.Tests.ValueObjects;

public class DueDateTests
{
    // Helper to get "today" in UTC (matching DueDate implementation)
    private static DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Constructor_WithDateOnly_SetsDate()
    {
        // Arrange
        var date = new DateOnly(2025, 12, 31);

        // Act
        var dueDate = new DueDate(date);

        // Assert
        Assert.Equal(date, dueDate.Date);
    }

    [Fact]
    public void FromDateTime_ExtractsDatePortion()
    {
        // Arrange
        var dateTime = new DateTime(2025, 12, 31, 14, 30, 0);

        // Act
        var dueDate = DueDate.FromDateTime(dateTime);

        // Assert
        Assert.Equal(new DateOnly(2025, 12, 31), dueDate.Date);
    }

    [Fact]
    public void IsOverdue_WhenDateInPast_ReturnsTrue()
    {
        // Arrange
        var yesterday = TodayUtc.AddDays(-1);
        var dueDate = new DueDate(yesterday);

        // Act & Assert
        Assert.True(dueDate.IsOverdue());
    }

    [Fact]
    public void IsOverdue_WhenDateIsToday_ReturnsFalse()
    {
        // Arrange
        var today = TodayUtc;
        var dueDate = new DueDate(today);

        // Act & Assert
        Assert.False(dueDate.IsOverdue());
    }

    [Fact]
    public void IsOverdue_WhenDateInFuture_ReturnsFalse()
    {
        // Arrange
        var tomorrow = TodayUtc.AddDays(1);
        var dueDate = new DueDate(tomorrow);

        // Act & Assert
        Assert.False(dueDate.IsOverdue());
    }

    [Fact]
    public void IsDueSoon_WhenDueWithinDefaultDays_ReturnsTrue()
    {
        // Arrange (default is 3 days)
        var inTwoDays = TodayUtc.AddDays(2);
        var dueDate = new DueDate(inTwoDays);

        // Act & Assert
        Assert.True(dueDate.IsDueSoon());
    }

    [Fact]
    public void IsDueSoon_WhenDueBeyondDays_ReturnsFalse()
    {
        // Arrange
        var inTenDays = TodayUtc.AddDays(10);
        var dueDate = new DueDate(inTenDays);

        // Act & Assert
        Assert.False(dueDate.IsDueSoon());
    }

    [Fact]
    public void IsDueSoon_WhenOverdue_ReturnsFalse()
    {
        // Arrange
        var yesterday = TodayUtc.AddDays(-1);
        var dueDate = new DueDate(yesterday);

        // Act & Assert
        Assert.False(dueDate.IsDueSoon());
    }

    [Fact]
    public void DaysUntilDue_WhenDueInFuture_ReturnsPositive()
    {
        // Arrange
        var inFiveDays = TodayUtc.AddDays(5);
        var dueDate = new DueDate(inFiveDays);

        // Act & Assert
        Assert.Equal(5, dueDate.DaysUntilDue());
    }

    [Fact]
    public void DaysUntilDue_WhenOverdue_ReturnsNegative()
    {
        // Arrange
        var threeDaysAgo = TodayUtc.AddDays(-3);
        var dueDate = new DueDate(threeDaysAgo);

        // Act & Assert
        Assert.Equal(-3, dueDate.DaysUntilDue());
    }

    [Fact]
    public void ToDisplayString_WhenToday_ReturnsToday()
    {
        // Arrange
        var today = TodayUtc;
        var dueDate = new DueDate(today);

        // Act & Assert
        Assert.Equal("Today", dueDate.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_WhenTomorrow_ReturnsTomorrow()
    {
        // Arrange
        var tomorrow = TodayUtc.AddDays(1);
        var dueDate = new DueDate(tomorrow);

        // Act & Assert
        Assert.Equal("Tomorrow", dueDate.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_WhenOverdue_ReturnsOverdueMessage()
    {
        // Arrange
        var twoDaysAgo = TodayUtc.AddDays(-2);
        var dueDate = new DueDate(twoDaysAgo);

        // Act & Assert
        Assert.Equal("Overdue by 2d", dueDate.ToDisplayString());
    }

    [Theory]
    [InlineData(2, "In 2d")]
    [InlineData(5, "In 5d")]
    [InlineData(7, "In 7d")]
    public void ToDisplayString_WhenDueWithinWeek_ReturnsInXdFormat(int daysFromNow, string expected)
    {
        // Arrange
        var futureDate = TodayUtc.AddDays(daysFromNow);
        var dueDate = new DueDate(futureDate);

        // Act & Assert
        Assert.Equal(expected, dueDate.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_WhenDueBeyondWeek_ReturnsMonthDayFormat()
    {
        // Arrange
        var farFuture = TodayUtc.AddDays(30);
        var dueDate = new DueDate(farFuture);

        // Act
        var result = dueDate.ToDisplayString();

        // Assert - should be format like "Jan 30" or "Feb 15"
        Assert.Equal(farFuture.ToString("MMM d"), result);
    }

    [Fact]
    public void Equality_SameDate_AreEqual()
    {
        // Arrange
        var date = new DateOnly(2025, 12, 31);
        var dueDate1 = new DueDate(date);
        var dueDate2 = new DueDate(date);

        // Act & Assert
        Assert.Equal(dueDate1, dueDate2);
    }

    [Fact]
    public void ToString_ReturnsIsoFormat()
    {
        // Arrange
        var dueDate = new DueDate(new DateOnly(2025, 12, 31));

        // Act & Assert
        Assert.Equal("2025-12-31", dueDate.ToString());
    }
}

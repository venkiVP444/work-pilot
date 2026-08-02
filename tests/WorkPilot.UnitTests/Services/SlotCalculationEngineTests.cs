using System;
using System.Collections.Generic;
using System.Linq;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Services;
using WorkPilot.Domain.Entities;
using Xunit;

namespace WorkPilot.UnitTests.Services;

public class SlotCalculationEngineTests
{
    [Fact]
    public void CalculateAvailableSlots_ShouldExcludeBusyIntervalsAndIncludeBuffer()
    {
        // Arrange: Business open 8:00 AM - 1:00 PM (13:00) on Saturday
        var targetDate = new DateTime(2026, 8, 8); // Saturday
        var rules = new List<AvailabilityRule>
        {
            new AvailabilityRule
            {
                DayOfWeek = DayOfWeek.Saturday,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(13, 0, 0),
                BufferMinutes = 15,
                IsActive = true
            }
        };

        // Busy interval: 10:00 AM - 11:30 AM
        var busyIntervals = new List<TimeIntervalDto>
        {
            new TimeIntervalDto(targetDate.AddHours(10), targetDate.AddHours(11).AddMinutes(30))
        };

        int duration = 60; // 60 minutes session

        // Act
        var slots = SlotCalculationEngine.CalculateAvailableSlots(rules, duration, busyIntervals, targetDate, "morning");

        // Assert
        Assert.NotEmpty(slots);
        foreach (var slot in slots)
        {
            var busyStart = targetDate.AddHours(10);
            var busyEnd = targetDate.AddHours(11).AddMinutes(30);

            bool overlaps = slot.StartTime < busyEnd && slot.EndTime.AddMinutes(15) > busyStart;
            Assert.False(overlaps, $"Slot {slot.StartTime} to {slot.EndTime} overlaps with busy interval!");
        }
    }

    [Fact]
    public void CalculateAvailableSlots_ShouldReturnEmpty_WhenBusinessClosed()
    {
        // Arrange
        var targetDate = new DateTime(2026, 8, 9); // Sunday
        var rules = new List<AvailabilityRule>
        {
            new AvailabilityRule
            {
                DayOfWeek = DayOfWeek.Saturday, // Only open Saturday
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsActive = true
            }
        };

        var slots = SlotCalculationEngine.CalculateAvailableSlots(rules, 60, new List<TimeIntervalDto>(), targetDate);

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void CalculateAvailableSlots_ShouldFilterByTimePreference_Evening()
    {
        // Arrange: Business open 8:00 AM - 9:00 PM on Monday
        var targetDate = new DateTime(2026, 8, 10); // Monday
        var rules = new List<AvailabilityRule>
        {
            new AvailabilityRule
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(21, 0, 0),
                BufferMinutes = 15,
                IsActive = true
            }
        };

        // Act
        var slots = SlotCalculationEngine.CalculateAvailableSlots(rules, 60, new List<TimeIntervalDto>(), targetDate, "evening");

        // Assert
        Assert.NotEmpty(slots);
        Assert.All(slots, slot => Assert.True(slot.StartTime.Hour >= 17, "Slot should be in the evening (>= 5 PM)"));
    }
}

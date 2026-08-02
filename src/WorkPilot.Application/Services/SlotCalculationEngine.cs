using System;
using System.Collections.Generic;
using System.Linq;
using WorkPilot.Application.DTOs;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Services;

public class SlotCalculationEngine
{
    public static List<CalendarSlotDto> CalculateAvailableSlots(
        List<AvailabilityRule> availabilityRules,
        int serviceDurationMinutes,
        List<TimeIntervalDto> busyIntervals,
        DateTime targetDate,
        string? timePreference = null)
    {
        var resultSlots = new List<CalendarSlotDto>();

        // Find availability rule for the target day of week
        var dayRule = availabilityRules.FirstOrDefault(r => r.DayOfWeek == targetDate.DayOfWeek && r.IsActive);
        if (dayRule == null)
        {
            return resultSlots; // Business is closed on this day
        }

        int bufferMinutes = dayRule.BufferMinutes;
        int totalSlotStepMinutes = serviceDurationMinutes + bufferMinutes;

        // Construct business opening and closing datetimes for the target date
        var windowStart = targetDate.Date.Add(dayRule.StartTime);
        var windowEnd = targetDate.Date.Add(dayRule.EndTime);

        // Ensure we don't propose past time if targetDate is today
        if (windowStart < DateTime.UtcNow)
        {
            // Round up to next 15-minute boundary
            var now = DateTime.UtcNow.AddMinutes(15);
            if (now > windowStart)
            {
                windowStart = now;
            }
        }

        var candidateStart = windowStart;

        while (candidateStart.AddMinutes(serviceDurationMinutes) <= windowEnd)
        {
            var candidateEnd = candidateStart.AddMinutes(serviceDurationMinutes);
            var candidateEndWithBuffer = candidateEnd.AddMinutes(bufferMinutes);

            // Check if slot + buffer overlaps with any busy interval
            bool isOccupied = busyIntervals.Any(busy =>
                candidateStart < busy.EndTime && candidateEndWithBuffer > busy.StartTime);

            if (!isOccupied)
            {
                string displayText = $"{candidateStart:ddd, MMM d, yyyy} @ {candidateStart:h:mm tt} - {candidateEnd:h:mm tt}";
                resultSlots.Add(new CalendarSlotDto(candidateStart, candidateEnd, displayText));
            }

            // Step forward by 30 mins or slot duration for nice slot intervals
            candidateStart = candidateStart.AddMinutes(30);
        }

        // Apply time preference filter if specified (e.g. morning: < 12 PM, afternoon: 12-5 PM, evening: > 5 PM)
        if (!string.IsNullOrWhiteSpace(timePreference))
        {
            var pref = timePreference.Trim().ToLowerInvariant();
            List<CalendarSlotDto>? filtered = null;
            if (pref.Contains("morning"))
            {
                filtered = resultSlots.Where(s => s.StartTime.Hour < 12).ToList();
            }
            else if (pref.Contains("afternoon"))
            {
                filtered = resultSlots.Where(s => s.StartTime.Hour >= 12 && s.StartTime.Hour < 17).ToList();
            }
            else if (pref.Contains("evening"))
            {
                filtered = resultSlots.Where(s => s.StartTime.Hour >= 17).ToList();
            }

            if (filtered != null && filtered.Any())
            {
                return filtered.Take(3).ToList();
            }
        }

        // Fallback: return top 3 available slots for the day
        return resultSlots.Take(3).ToList();
    }
}

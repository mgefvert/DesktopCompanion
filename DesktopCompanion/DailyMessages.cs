using System;
using System.Collections.Generic;

// ReSharper disable LocalizableElement

namespace DesktopCompanion;

public static class DailyMessages
{
    private static readonly Dictionary<DayOfWeek, string[]> Messages = new()
    {
        [DayOfWeek.Monday] = new[]
        {
            "What a wonderful Monday, Sir!",
            "Happy Monday, Sir!",
            "Delightful Monday, Sir!"
        },
        [DayOfWeek.Tuesday] = new[]
        {
            "A Tuesday is a great day to be alive, Sir!",
            "Tuesdays are for vacuuming and cleaning, Sir!",
            "Tuesday, remarkable day, Sir!"
        },
        [DayOfWeek.Wednesday] = new[]
        {
            "Ah, Wednesdays, the joy of the week, Sir!",
            "It's a good Wednesday today, Sir!",
            "Mid-week indeed, very good, Sir!"
        },
        [DayOfWeek.Thursday] = new[]
        {
            "Thursday - the most average day of the week, Sir!",
            "A capital Thursday it is today, Sir!",
            "Thursday - maybe a final sprint toward the weekend, Sir?"
        },
        [DayOfWeek.Friday] = new[]
        {
            "Today is Friday, Sir! A most excellent day!",
            "It's Friday today, Sir! You made it, another week!",
            "The weekend is nigh, Sir! Jolly good work!"
        },
        [DayOfWeek.Saturday] = new[]
        {
            "A day of relaxation and fun, Sir!",
            "Saturday, time for shopping and fun, Sir!",
            "",
        },
        [DayOfWeek.Sunday] = new[]
        {
            "A holy day, Sir, good for soul and spirit!",
            "A restful day, I hope, Sir?",
            "Don't mind me, I'm just watching the penguins, Sir."
        }
    };

    public static string GetDailyMessage()
    {
        var sayings = Messages[DateTime.Today.DayOfWeek];
        var weekNo = (int)DateTime.Today.ToOADate() / 7;

        return sayings[weekNo % sayings.Length];
    }
}
using System;

namespace Statecraft.Sim
{
    public enum TurnLength { Quarter, Month, Week }

    public static class SimTime
    {
        public static DateTime Advance(DateTime date, TurnLength len) => len switch
        {
            TurnLength.Quarter => date.AddMonths(3),
            TurnLength.Month => date.AddMonths(1),
            _ => date.AddDays(7),
        };

        /// <summary>Share of a year one turn represents (used to scale yearly rates).</summary>
        public static double YearFraction(TurnLength len) => len switch
        {
            TurnLength.Quarter => 0.25,
            TurnLength.Month => 1.0 / 12,
            _ => 7.0 / 365,
        };
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Statecraft.Sim
{
    public enum Mode { Historical, Open }

    /// <summary>All mutable game data. Systems never share references between states: Step works on Clone().</summary>
    public sealed class GameState
    {
        public DateTime Date;
        public ulong RngState;
        public Mode Mode;
        public int Turn;
        public Dictionary<string, double> Indicators = new Dictionary<string, double>();
        public Dictionary<string, double> Policies = new Dictionary<string, double>();
        public Dictionary<string, double> EffectContributions = new Dictionary<string, double>();
        /// <summary>Permanent per-indicator offsets from event choices, added on top of base and effects.</summary>
        public Dictionary<string, double> IndicatorShifts = new Dictionary<string, double>();
        public SortedSet<string> ActiveSituations = new SortedSet<string>(StringComparer.Ordinal);
        public Dictionary<string, double> Budget = new Dictionary<string, double>();
        public double Treasury;
        public double Debt;
        public double FxReserves;
        public Dictionary<string, double> Population = new Dictionary<string, double>();
        public int Rung = 1;
        public int CeasefireWeeksLeft;
        public double Mobilization;
        public double CoalitionStability = 1.0;
        public double Attention;
        public SortedSet<string> FiredEvents = new SortedSet<string>(StringComparer.Ordinal);
        public List<string> PendingEvents = new List<string>();
        /// <summary>Player appointments by office id; valid while the government in AppointedUnder is in office (Cabinet).</summary>
        public Dictionary<string, Appointment> Appointments = new Dictionary<string, Appointment>();
        public string AppointedUnder = "";
        public string? GameOver;

        public double TotalPopulation => Population.Values.Sum();

        public GameState Clone()
        {
            var c = (GameState)MemberwiseClone();
            c.Indicators = new Dictionary<string, double>(Indicators);
            c.Policies = new Dictionary<string, double>(Policies);
            c.EffectContributions = new Dictionary<string, double>(EffectContributions);
            c.IndicatorShifts = new Dictionary<string, double>(IndicatorShifts);
            c.ActiveSituations = new SortedSet<string>(ActiveSituations, StringComparer.Ordinal);
            c.Budget = new Dictionary<string, double>(Budget);
            c.Population = new Dictionary<string, double>(Population);
            c.FiredEvents = new SortedSet<string>(FiredEvents, StringComparer.Ordinal);
            c.PendingEvents = new List<string>(PendingEvents);
            c.Appointments = new Dictionary<string, Appointment>(Appointments);
            return c;
        }

        /// <summary>SHA-256 over a canonical text form (ordinal-sorted keys, doubles as IEEE bit patterns with -0.0
        /// written as 0.0), so hashes match across .NET, Mono and IL2CPP.</summary>
        public string Hash()
        {
            var sb = new StringBuilder();
            void Num(string k, double v) => sb.Append(k).Append('=').Append(BitConverter.DoubleToInt64Bits(v == 0.0 ? 0.0 : v).ToString("X16", CultureInfo.InvariantCulture)).Append(';');
            void Map(string name, Dictionary<string, double> m)
            {
                sb.Append(name).Append('{');
                foreach (var kv in m.OrderBy(k => k.Key, StringComparer.Ordinal)) Num(kv.Key, kv.Value);
                sb.Append('}');
            }
            sb.Append(Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';')
              .Append(RngState).Append(';').Append(Mode).Append(';').Append(Turn).Append(';');
            Map("ind", Indicators); Map("pol", Policies); Map("eff", EffectContributions); Map("shift", IndicatorShifts);
            Map("bud", Budget); Map("pop", Population);
            sb.Append("sit[").Append(string.Join(",", ActiveSituations)).Append(']');
            Num("treasury", Treasury); Num("debt", Debt); Num("fx", FxReserves);
            sb.Append("rung=").Append(Rung).Append(";cf=").Append(CeasefireWeeksLeft).Append(';');
            Num("mob", Mobilization); Num("stab", CoalitionStability); Num("att", Attention);
            sb.Append("fired[").Append(string.Join(",", FiredEvents)).Append(']');
            sb.Append("pending[").Append(string.Join(",", PendingEvents)).Append(']');
            sb.Append("appt{");
            foreach (var kv in Appointments.OrderBy(k => k.Key, StringComparer.Ordinal))
                sb.Append(kv.Key).Append('=').Append(kv.Value.Person).Append('@').Append(kv.Value.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';');
            sb.Append("}under=").Append(AppointedUnder).Append(';');
            sb.Append("over=").Append(GameOver ?? "");
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }
    }
}

using System.Globalization;
using Statecraft.Sim;

// Text front end for the headless sim.
//   dotnet run --project Statecraft.Cli -- [--content DIR] [--seed N] [--auto historical --turns N]
// Interactive commands: c EVENT OPTION | p POLICY LEVEL | m LEVEL | s DELTA | n (end turn) | q
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string? Arg(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

string FindContent()
{
    for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
        if (File.Exists(Path.Combine(d.FullName, "content", "people.yaml"))) return Path.Combine(d.FullName, "content");
    throw new DirectoryNotFoundException("content/ not found; pass --content DIR");
}

Content content;
try { content = ContentLoader.Load(Arg("--content") ?? FindContent()); }
catch (ContentException e) { Console.Error.WriteLine($"Content error: {e.Message}"); return 2; }

var state = Simulation.NewGame(content, ulong.Parse(Arg("--seed") ?? "1948"), Mode.Historical);
bool auto = Arg("--auto") == "historical";
int turns = int.Parse(Arg("--turns") ?? "1000");

for (int t = 0; t < turns && state.GameOver == null; t++)
{
    Briefing(content, state);
    var commands = auto ? HistoricalAI.Commands(content, state).ToList() : ReadCommands();
    if (commands == null) break;
    var result = Simulation.Step(content, state, commands);
    foreach (var r in result.Rejected) Console.WriteLine($"  rejected: {r.Reason}");
    foreach (var w in result.Why.Where(w => w.System != "effects" && w.System != "economy")) Console.WriteLine($"  {w}");
    state = result.State;
}
Console.WriteLine($"End {state.Date:yyyy-MM-dd}: population {state.TotalPopulation:N0}, level {state.Rung}, treasury {state.Treasury:0.0}, debt {state.Debt:0.0}, fx {state.FxReserves:0.0}, stability {state.CoalitionStability:0.00}{(state.GameOver != null ? ", GAME OVER: " + state.GameOver : "")}");
Console.WriteLine($"State hash {state.Hash()}");
return 0;

static void Briefing(Content c, GameState s)
{
    var gov = Offices.Government(c, s.Date);
    Console.WriteLine($"== {s.Date:yyyy-MM-dd} | turn {s.Turn} | {gov?.Name} | security level {s.Rung} | attention {s.Attention:0.0}");
    foreach (var id in s.PendingEvents)
    {
        var e = c.Events.First(x => x.Id == id);
        Console.WriteLine($"  EVENT {e.Id}: {Offices.Fill(c, e.Title, s.Date)} - {EventSystem.Render(c, e, s.Date)}");
        foreach (var o in e.Options) Console.WriteLine($"    {o.Id}: {Offices.Fill(c, o.Text, s.Date)}");
    }
}

static List<Command>? ReadCommands()
{
    var list = new List<Command>();
    while (true)
    {
        Console.Write("> ");
        var parts = (Console.ReadLine() ?? "q").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) continue;
        switch (parts[0])
        {
            case "q": return null;
            case "n": return list;
            case "c" when parts.Length == 3: list.Add(new ChooseEventOption(parts[1], parts[2])); break;
            case "p" when parts.Length == 3: list.Add(new SetPolicy(parts[1], double.Parse(parts[2]))); break;
            case "m" when parts.Length == 2: list.Add(new SetMobilization(double.Parse(parts[1]))); break;
            case "s" when parts.Length == 2: list.Add(new SecurityOrder(int.Parse(parts[1]))); break;
            default: Console.WriteLine("commands: c EVENT OPTION | p POLICY LEVEL | m LEVEL | s DELTA | n | q"); break;
        }
    }
}

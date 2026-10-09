namespace Statecraft.Sim
{
    /// <summary>One line of the "Why did this happen?" report.</summary>
    public sealed class Cause
    {
        public string System { get; }
        public string Target { get; }
        public double Delta { get; }
        public string Message { get; }
        public Cause(string system, string target, double delta, string message)
        { System = system; Target = target; Delta = delta; Message = message; }
        public override string ToString() => $"[{System}] {Message}";
    }
}

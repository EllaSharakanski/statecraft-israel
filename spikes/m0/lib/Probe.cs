using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SpikeLib
{
    public sealed class Minister
    {
        public string Name { get; set; } = "";
        public string Portfolio { get; set; } = "";
    }

    public static class Probe
    {
        public static List<Minister> ParseMinisters(string yaml) =>
            new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build()
                .Deserialize<List<Minister>>(yaml);
    }
}

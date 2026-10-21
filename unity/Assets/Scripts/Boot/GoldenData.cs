#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Statecraft.Game.Boot
{
    /// <summary>The golden manifest and scripted scenarios, shipped beside content so IL2CPP and WebGL run the same set.</summary>
    public sealed class GoldenData
    {
        public string Manifest = "";
        public Dictionary<string, string> Scenarios = new Dictionary<string, string>(StringComparer.Ordinal);

        public static GoldenData FromDisk(string root)
        {
            var manifest = File.ReadAllText(Path.Combine(root, "manifest.txt"));
            var scenarioDir = Directory.Exists(Path.Combine(root, "scenarios")) ? Path.Combine(root, "scenarios") : Path.Combine(Directory.GetParent(root)!.FullName, "scenarios");
            var scenarios = Directory.Exists(scenarioDir)
                ? Directory.GetFiles(scenarioDir, "*.yaml").ToDictionary(Path.GetFileNameWithoutExtension, File.ReadAllText, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal);
            return new GoldenData { Manifest = manifest, Scenarios = scenarios };
        }

        public static IReadOnlyList<(string name, string hash)> Parse(string text) => text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith("#", StringComparison.Ordinal)).Select(line => line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(parts => parts.Length == 2 ? (parts[0], parts[1]) : throw new InvalidDataException("golden manifest line must be: name hash")).ToList();
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Statecraft.Game.UI
{
    /// <summary>UI strings kept outside code (unity/Assets/UI/strings.en.yaml): key -> string.Format pattern.</summary>
    public sealed class UiText
    {
        /// <summary>U+200E LEFT-TO-RIGHT MARK: invisible, takes no room, and reads as a left-to-right letter to the bidi rules.</summary>
        public const char LeftToRightMark = '‎';

        // A format item ("{0}", "{1:N0}", "{2,-8}") or an escaped brace, as string.Format reads them.
        static readonly Regex FormatItem = new Regex(@"\{\{|\}\}|\{(\d+)((?:,\s*-?\d+)?(?::[^{}]*)?)\}", RegexOptions.CultureInvariant);

        static readonly char[] Braces = { '{', '}' };

        readonly Dictionary<string, string> map;
        readonly Dictionary<string, bool> hebrew = new Dictionary<string, bool>();
        readonly string file;
        UiText(Dictionary<string, string> map, string file) { this.map = map; this.file = file; }

        /// <summary>Parses the strings file. Throws InvalidOperationException naming the file when it is empty or not a map.</summary>
        public static UiText Parse(string yaml, string file = "strings.en.yaml")
        {
            Dictionary<string, string> m;
            try { m = new DeserializerBuilder().WithDuplicateKeyChecking().Build().Deserialize<Dictionary<string, string>>(yaml ?? ""); }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException($"{file}: {e.Message}", e); }
            if (m == null || m.Count == 0) throw new InvalidOperationException($"{file}: no strings");
            return new UiText(m, file);
        }

        /// <summary>True when the strings have <paramref name="key"/> (a lookup that must not throw, as GameStart's fallback).</summary>
        public bool Has(string key) => map.ContainsKey(key);

        public string this[string key] =>
            map.TryGetValue(key, out var v) ? v : throw new KeyNotFoundException($"{file} has no key '{key}'");

        /// <summary>string.Format over the key's pattern. In a Hebrew pattern (its own first letter, the format items left
        /// out, is Hebrew) each value with no Hebrew in it is put between two left-to-right marks (#543): in a right-to-left
        /// line the bidi rules would otherwise draw "-$4.2m" as "$4.2m-" and split "15 May 1948" into "May 1948 15". A value
        /// with Hebrew in it is left alone: a Hebrew word (a seat unit) between marks would make the space next to it left to
        /// right, and a value Format has already made from a Hebrew pattern has its own values marked. Every other pattern,
        /// so all of English, gives string.Format's text unchanged.</summary>
        public string Format(string key, params object[] args)
        {
            var pattern = this[key];
            if (!IsHebrew(key, pattern)) return string.Format(CultureInfo.InvariantCulture, pattern, args);
            // A brace left once the format items are taken out (a lone "}", "{x}") makes string.Format throw; so does this.
            if (FormatItem.Replace(pattern, "").IndexOfAny(Braces) >= 0)
                throw new FormatException($"{file}: '{key}' is not a valid format pattern: {pattern}");
            return FormatItem.Replace(pattern, m =>
            {
                if (!m.Groups[1].Success) return m.Value.Substring(1);              // "{{" or "}}"
                var value = string.Format(CultureInfo.InvariantCulture, "{0" + m.Groups[2].Value + "}", Arg(args, m.Groups[1].Value));
                return value.Length == 0 || UiDirection.HasRightToLeft(value) ? value : LeftToRightMark + value + LeftToRightMark;
            });
        }

        static object? Arg(object[] args, string index)
        {
            int i = int.Parse(index, CultureInfo.InvariantCulture);
            if (args == null || i >= args.Length) throw new FormatException("Index (zero based) must be greater than or equal to zero and less than the size of the argument list.");
            return args[i];
        }

        bool IsHebrew(string key, string pattern)
        {
            if (!hebrew.TryGetValue(key, out var h)) hebrew[key] = h = UiDirection.RightToLeft(FormatItem.Replace(pattern, ""));
            return h;
        }
    }
}

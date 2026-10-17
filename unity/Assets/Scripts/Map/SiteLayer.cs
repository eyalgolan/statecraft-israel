#nullable enable
using System.Collections.Generic;
using Statecraft.Sim;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>RED-FIRST STUB for #381 (replaced by the implementation in the next commit): the names the tests use, no behaviour.</summary>
    public sealed class SiteLayer : MonoBehaviour
    {
        public const string Prefix = "site_", ScaffoldSuffix = "_scaffold";
        readonly Dictionary<string, Bounds[]> columns = new Dictionary<string, Bounds[]>();
        public IReadOnlyDictionary<string, Bounds[]> Columns => columns;
        public void Render(IEnumerable<SiteWork> sites, Content content, MapScene map) { }
    }
}

#if UNITY_EDITOR
#nullable enable
using System;
using UnityEngine;

namespace Statecraft.Game.VehicleReview
{
    /// <summary>Only the owner's three generated vehicle candidates. Review labels are not sourced deployments.</summary>
    public sealed class VehicleReviewCatalog : ScriptableObject
    {
        public const string Folder = "Assets/Editor/Generated1948VehicleReview";
        public const string Path = Folder + "/Models.asset";
        public const string ScenePath = Folder + "/VehicleReview.unity";
        public static readonly string[] Ids = { "generated-1948-13-egyptian-spitfire", "generated-1948-14-syrian-renault-r35", "generated-1948-16-egyptian-dakota-c47" };
        [Serializable] public sealed class Entry
        {
            public string id = "", label = "";
            public GameObject model = null!;
            public float longSideM;
        }
        public Entry[] entries = Array.Empty<Entry>();
        public void Validate()
        {
            if (entries.Length != Ids.Length) throw new InvalidOperationException("Vehicle review needs exactly its three candidates.");
            for (int i = 0; i < entries.Length; i++)
                if (entries[i] == null || entries[i].id != Ids[i] || entries[i].model == null || entries[i].longSideM <= 0)
                    throw new InvalidOperationException("Incomplete vehicle review candidate " + Ids[i]);
        }
    }
}
#endif

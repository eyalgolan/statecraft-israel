#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>Person id -> portrait texture (tech notes section 7): imported assets referenced from a scene object, so
    /// they ship in the build's data file and need no runtime loading. Built by the editor tool PortraitImport from the
    /// public-domain, CC0, CC BY 4.0 and CC BY-SA 4.0 files people.yaml names (portrait.file, licence and source
    /// recorded there).</summary>
    [CreateAssetMenu(menuName = "Statecraft/Portrait Catalog")]
    public sealed class PortraitCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string personId; public Texture2D image;
            /// <summary>The face centre's height as a share of the picture (people.yaml portrait.focus_y).</summary>
            public float focusY;
        }

        public List<Entry> entries = new List<Entry>();

        /// <summary>The person's portrait, or null when there is none.</summary>
        public Texture2D? Get(string? personId) => Find(personId)?.image;

        /// <summary>The person's entry (picture and focal point), or null.</summary>
        public Entry? Find(string? personId)
        {
            if (personId == null) return null;
            foreach (var e in entries) if (e.personId == personId) return e;
            return null;
        }
    }
}

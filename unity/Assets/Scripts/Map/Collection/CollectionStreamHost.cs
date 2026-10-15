#nullable enable
using System.Collections;
using UnityEngine;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Runs the game's <see cref="CollectionStream"/> as a coroutine on a hidden object that outlives scene loads.</summary>
    public sealed class CollectionStreamHost : MonoBehaviour
    {
        public IEnumerator Run(CollectionStream stream) => stream.Run();
    }
}

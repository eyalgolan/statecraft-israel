#if UNITY_EDITOR
#nullable enable
using UnityEngine;
using UnityEditor;

namespace Statecraft.Game.VehicleReview
{
    /// <summary>Editor-only Play Mode entry; no runtime bootstrap or player-build dependency.</summary>
    public sealed class VehicleReviewScene : MonoBehaviour
    {
        public VehicleReviewStage Open()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VehicleReviewCatalog>(VehicleReviewCatalog.Path);
            if (catalog == null) throw new System.InvalidOperationException("Run VehicleReviewAssets.Import to create the review catalog.");
            return VehicleReviewStage.Create(catalog);
        }
        void Start() => Open();
    }
}
#endif

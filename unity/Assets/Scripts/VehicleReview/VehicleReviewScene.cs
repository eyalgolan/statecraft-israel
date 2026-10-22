#nullable enable
using UnityEngine;

namespace Statecraft.Game.VehicleReview
{
    /// <summary>The dedicated, playable review scene uses the same runtime environment as the opt-in player mode.</summary>
    public sealed class VehicleReviewScene : MonoBehaviour
    {
        public VehicleReviewStage Open()
        {
            var catalog = Resources.Load<VehicleReviewCatalog>(VehicleReviewCatalog.Resource);
            if (catalog == null) throw new System.InvalidOperationException("Run VehicleReviewAssets.Import to create the review catalog.");
            return VehicleReviewStage.Create(catalog);
        }
        void Start() => Open();
    }
}

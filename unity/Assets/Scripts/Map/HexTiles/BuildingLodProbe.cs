#nullable enable
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.HexTiles
{
    /// <summary>Opt-in, single-building experiment. A card replaces only the colour pass at the baked view;
    /// the original still casts its shadow. No map loader or production scene creates this component.</summary>
    [ExecuteAlways]
    public sealed class BuildingLodProbe : MonoBehaviour
    {
        public bool ExperimentEnabled;
        public bool UsingImpostor { get; private set; }
        [SerializeField] Renderer? source, card;
        [SerializeField] Camera? view;
        [SerializeField] bool sourceEnabled, cardEnabled;
        [SerializeField] ShadowCastingMode sourceShadows;
        [SerializeField] Matrix4x4 bakedTransform;
        [SerializeField] Quaternion bakedCameraRotation;
        [SerializeField] Vector3 bakedDirection, centre;
        [SerializeField] float minimumDistance, bakedDistance;
        [SerializeField] Light? sun;
        [SerializeField] bool hadSun;
        [SerializeField] Quaternion sunRotation;
        [SerializeField] Color sunColour, ambientSky, ambientEquator, ambientGround;
        [SerializeField] float sunIntensity;

        public void Configure(Renderer original, Renderer impostor, Camera camera, float distance, Light? lighting = null)
        {
            if (original == null || impostor == null || camera == null || original == impostor
                || float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0f)
                throw new ArgumentException("A distinct source, card, camera and finite positive distance are required.");
            Restore();
            source = original; card = impostor; view = camera;
            sourceEnabled = source.enabled; cardEnabled = card.enabled; sourceShadows = source.shadowCastingMode;
            bakedTransform = source.transform.localToWorldMatrix;
            bakedCameraRotation = view.transform.rotation;
            centre = source.bounds.center;
            bakedDirection = (centre - view.transform.position).normalized;
            bakedDistance = Vector3.Distance(view.transform.position, centre);
            minimumDistance = distance;
            sun = lighting;
            hadSun = sun != null;
            if (sun != null) { sunRotation = sun.transform.rotation; sunColour = sun.color; sunIntensity = sun.intensity; }
            ambientSky = RenderSettings.ambientSkyColor; ambientEquator = RenderSettings.ambientEquatorColor;
            ambientGround = RenderSettings.ambientGroundColor;
            Refresh();
        }

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (source == null || card == null) { Restore(); return; }
            bool use = isActiveAndEnabled && ExperimentEnabled && sourceEnabled && view != null && view.isActiveAndEnabled
                && (sourceShadows == ShadowCastingMode.On || sourceShadows == ShadowCastingMode.Off)
                && !view.orthographic && source.transform.localToWorldMatrix == bakedTransform
                && source.gameObject.activeInHierarchy && card.gameObject.activeInHierarchy
                && Vector3.Distance(view.transform.position, centre) >= minimumDistance
                && Mathf.Abs(Vector3.Distance(view.transform.position, centre) - bakedDistance) <= bakedDistance * 0.01f
                && Quaternion.Angle(view.transform.rotation, bakedCameraRotation) <= 0.1f
                && Vector3.Angle(centre - view.transform.position, bakedDirection) <= 0.1f
                && (!hadSun || sun != null && sun.isActiveAndEnabled && sun.transform.rotation == sunRotation && sun.color == sunColour && sun.intensity == sunIntensity)
                && RenderSettings.ambientSkyColor == ambientSky && RenderSettings.ambientEquatorColor == ambientEquator
                && RenderSettings.ambientGroundColor == ambientGround;
            UsingImpostor = use;
            // Keep the original shadow geometry: claiming two triangles for the whole frame would be wrong.
            bool castsShadow = sourceShadows == ShadowCastingMode.On || sourceShadows == ShadowCastingMode.TwoSided;
            source.enabled = sourceEnabled && (!use || castsShadow);
            source.shadowCastingMode = use && castsShadow ? ShadowCastingMode.ShadowsOnly : sourceShadows;
            card.enabled = use;
        }

        void OnDisable() => Restore();
        void OnDestroy() => Restore();
        void Restore()
        {
            if (source != null) { source.enabled = sourceEnabled; source.shadowCastingMode = sourceShadows; }
            if (card != null) card.enabled = cardEnabled;
            UsingImpostor = false;
        }
    }
}

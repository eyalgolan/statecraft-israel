#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Statecraft.HexTiles
{
    /// <summary>One member of a unit: a model (a rigged figure or an animated vehicle), its place in the formation (metres
    /// in the unit's frame: x to the right, z forward, as shares of S), its height share of S, and how far it travels per
    /// cycle of its move clip (a walk cycle's stride, a wheel's turn), also as a share of S.</summary>
    public sealed class HexUnitMember
    {
        public string Model = ""; public StandIn StandIn = StandIn.Figure; public Vector2 Offset; public float HeightShare = 0.18f;
        public float CycleShare = 0.15f; public bool ByWidth;
        /// <summary>Degrees to turn this model so its front faces the direction of travel (vehicles often face +X).</summary>
        public float YawOffset;
    }

    /// <summary>Unit sizes, larger than buildings and spread over much of the hex as Civ V draws units (Tal, 18 Oct): a
    /// character unit is exactly
    /// <see cref="Characters"/> figures and nothing else; a vehicle unit is one vehicle. Shares of S.</summary>
    public static class HexUnitScale
    {
        public const int Characters = 8;
        public const float FigureHeight = 0.38f, FigureSpacing = 0.25f, VehicleLength = 0.55f;
        /// <summary>Distance per walk cycle as a share of figure height (a stride of about 1.5 m for 1.75 m tall).</summary>
        public const float StridePerHeight = 0.86f;
    }

    /// <summary>Marks a unit type with a unique id (lower_snake_case). Units live in Units/, one file each.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class HexUnitTypeAttribute : Attribute
    {
        public string Id { get; }
        public HexUnitTypeAttribute(string id) => Id = id;
    }

    public interface IHexUnitType
    {
        string Title { get; }
        HexUnit Create();
    }

    public static class HexUnitTypes
    {
        static Dictionary<string, Type>? all;

        public static IReadOnlyDictionary<string, Type> All => all ??= AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; } })
            .Where(t => t != null && !t.IsAbstract && typeof(IHexUnitType).IsAssignableFrom(t))
            .Select(t => (t, attr: (HexUnitTypeAttribute?)Attribute.GetCustomAttribute(t, typeof(HexUnitTypeAttribute))))
            .Where(p => p.attr != null)
            .ToDictionary(p => p.attr!.Id, p => p.t!, StringComparer.Ordinal);

        public static IHexUnitType Create(string id) =>
            All.TryGetValue(id, out var t) ? (IHexUnitType)Activator.CreateInstance(t)! : throw new KeyNotFoundException($"no hex unit type '{id}' (known: {string.Join(", ", All.Keys)})");

        /// <summary>HexUnitScale.Characters figures of one <paramref name="model"/> (Tal, 19 October 2026: a character unit is
        /// one character type, never a mix): rows of 3, 2, 3 (a wedge), spaced HexUnitScale.FigureSpacing, all facing
        /// forward.</summary>
        public static HexUnit Squad(string title, string model)
        {
            var unit = new HexUnit { Title = title };
            var slots = new[] { (-1f, 1f), (0f, 1.15f), (1f, 1f), (-0.5f, 0f), (0.5f, 0f), (-1f, -1f), (0f, -0.85f), (1f, -1f) };
            for (int i = 0; i < HexUnitScale.Characters; i++)
                unit.Members.Add(new HexUnitMember
                {
                    Model = model, StandIn = StandIn.Figure,
                    Offset = new Vector2(slots[i].Item1, slots[i].Item2) * HexUnitScale.FigureSpacing,
                    HeightShare = HexUnitScale.FigureHeight, CycleShare = HexUnitScale.FigureHeight * HexUnitScale.StridePerHeight,
                });
            return unit;
        }
    }

    /// <summary>A unit: the members that move together across hexes. Units are their own layer, never part of a tile's
    /// art: the tile draws the land, the unit layer draws who stands on it, so a unit can leave one tile and enter the next.</summary>
    public sealed class HexUnit
    {
        public string Title = ""; public List<HexUnitMember> Members = new List<HexUnitMember>();
    }

    /// <summary>Moves a unit along a path of hex centres. Presentation only: the simulation decides the move (A to B);
    /// this glides the group between centres, turning smoothly at each, and drives each member's clip by distance
    /// travelled (stride-locked: the feet and wheels never slide), with the idle clip when it stands. Tripo clips are
    /// exported in place (no root motion), so position is the code's and every move ends exactly on a hex centre.</summary>
    public sealed class HexUnitMotion
    {
        sealed class Live { public HexUnitMember M = null!; public GameObject Go = null!, Rig = null!; public AnimationClip? Move, Idle; public float Scale; }

        readonly List<Live> live = new List<Live>();
        readonly List<Vector3> path;
        readonly IHexSurface surface;
        readonly float size, length;
        /// <summary>Degrees the models' front is turned from +Z (Tripo exports through Blender face -Y, imported +Z: 0).</summary>
        public float ModelYaw = 0f;

        readonly float facing;
        public IReadOnlyList<GameObject> Objects => live.Select(l => l.Go).ToList();

        /// <summary><paramref name="facingDeg"/> is the heading (degrees from north) of a unit standing on a one-hex path.</summary>
        public HexUnitMotion(HexUnit unit, IReadOnlyList<(int q, int r)> hexes, IHexSurface surface, Transform parent, float sizeM = HexScale.HexM, float facingDeg = 0f)
        {
            this.surface = surface; size = sizeM; facing = facingDeg;
            path = hexes.Select(h => HexGeometry.Centre(h.q, h.r, sizeM)).ToList();
            for (int i = 1; i < path.Count; i++) length += Vector3.Distance(path[i - 1], path[i]);
            foreach (var m in unit.Members)
            {
                var prefab = HexModels.Prefab(m.Model);
                GameObject go;
                if (prefab != null)
                {
                    go = Object.Instantiate(prefab, parent);
                }
                else
                {
                    go = GameObject.CreatePrimitive(m.StandIn == StandIn.Box ? PrimitiveType.Cube : PrimitiveType.Capsule);
                    go.transform.SetParent(parent, false);
                    Object.DestroyImmediate(go.GetComponent<Collider>());
                }
                var b = HexModels.Bounds(go);
                float scale = m.ByWidth ? m.HeightShare * sizeM / Mathf.Max(b.size.x, b.size.z) : m.HeightShare * sizeM / Mathf.Max(b.size.y, 1e-4f);
                go.transform.localScale = Vector3.one * scale;
                var animator = go.GetComponentInChildren<Animator>();
                var clips = animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.animationClips : Array.Empty<AnimationClip>();
                live.Add(new Live
                {
                    M = m, Go = go, Rig = animator != null ? animator.gameObject : go, Scale = scale,
                    Move = clips.FirstOrDefault(c => c.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0) ?? clips.FirstOrDefault(c => c.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) < 0),
                    Idle = clips.FirstOrDefault(c => c.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0),
                });
            }
        }

        /// <summary>Total path length in metres.</summary>
        public float Length => length;

        /// <summary>Poses the unit <paramref name="distance"/> metres along the path (clamped), moving or standing.</summary>
        public Vector3 Pose(float distance, bool moving)
        {
            distance = Mathf.Clamp(distance, 0f, length);
            var (centre, heading) = Along(distance);
            var turn = Quaternion.Euler(0f, heading, 0f);
            foreach (var l in live)
            {
                var p = centre + turn * new Vector3(l.M.Offset.x * size, 0f, l.M.Offset.y * size);
                p.y = surface.Height(p);
                // Board space: the units' parent sits in the board root (or at the origin for a board on its own).
                l.Go.transform.localPosition = p; l.Go.transform.localRotation = Quaternion.Euler(0f, heading + ModelYaw + l.M.YawOffset, 0f);
                var clip = moving ? l.Move ?? l.Idle : l.Idle ?? l.Move;
                if (clip == null) continue;
                float cycle = l.M.CycleShare * size;
                float t = moving ? Mathf.Repeat(distance / cycle, 1f) * clip.length : 0f;
                clip.SampleAnimation(l.Rig, t);   // clips are authored on the Animator's object
            }
            return new Vector3(centre.x, surface.Height(centre), centre.z);
        }

        /// <summary>The point and heading (degrees from north) at a distance along the path; headings blend over the last
        /// and first 0.35 S of each leg, so the unit turns smoothly at a hex centre instead of snapping.</summary>
        (Vector3 at, float heading) Along(float d)
        {
            for (int i = 1; i < path.Count; i++)
            {
                float leg = Vector3.Distance(path[i - 1], path[i]);
                if (d <= leg || i == path.Count - 1)
                {
                    float u = Mathf.Clamp01(d / leg);
                    var at = Vector3.Lerp(path[i - 1], path[i], u);
                    float h = Bearing(path[i - 1], path[i]);
                    float blend = 0.35f * size;
                    if (i + 1 < path.Count && leg - d < blend)
                        h = Mathf.LerpAngle(h, Bearing(path[i], path[i + 1]), 0.5f * (1f - (leg - d) / blend));
                    if (i > 1 && d < blend)
                        h = Mathf.LerpAngle(Bearing(path[i - 2], path[i - 1]), h, 0.5f + 0.5f * d / blend);
                    return (at, h);
                }
                d -= leg;
            }
            return (path[0], facing);
        }

        static float Bearing(Vector3 a, Vector3 b) => Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg;
    }
}

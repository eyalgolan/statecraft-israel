#nullable enable
using System.Collections.Generic;
using Statecraft.Game.Map.Units;
using UnityEngine;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>The train's cars as painted miniatures (MiniatureMesh and the units' palette), in real metres, facing +Z,
    /// origin on the rail at the car's middle: a tender locomotive (boiler, smokebox, chimney, dome, cab, red buffer beam),
    /// its tender and four coaches (olive-brown sides, a window band and a light grey roof), after the promo's train. Generic
    /// period shapes for the map's far zoom, not a model of a particular Palestine Railways class: the close zoom's
    /// authored models are the AI-3D track's (owner ruling, 2026-10-11). Pure geometry: the same calls give the same mesh.</summary>
    public static class TrainModels
    {
        public const float LocoLengthM = 17f, TenderLengthM = 7.5f, CoachLengthM = 20f, GapM = 1f;
        public const int Coaches = 4;

        /// <summary>The consist from the front: (name, mesh, length in real metres).</summary>
        public static List<(string name, Mesh mesh, float lengthM)> Consist()
        {
            var cars = new List<(string, Mesh, float)>
            {
                ("Locomotive", Locomotive(), LocoLengthM),
                ("Tender", Tender(), TenderLengthM),
            };
            for (int i = 0; i < Coaches; i++) cars.Add(($"Coach{i}", Coach($"TrainCoach{i}"), CoachLengthM));
            return cars;
        }

        static Matrix4x4 Box(float x, float y, float z, float w, float h, float l) =>
            Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.identity, new Vector3(w, h, l));

        public static Mesh Locomotive()
        {
            var m = new MiniatureMesh();
            m.Box(Box(0, 0.7f, 0, 2.2f, 1.2f, 14f), UnitPalette.Waterline);         // wheels and frames, dark
            m.Box(Box(0, 1.4f, 0.4f, 2.9f, 0.3f, 16f), UnitPalette.PropBlack);      // running board
            m.Rod(new Vector3(0, 2.7f, -3.5f), new Vector3(0, 2.7f, 6.6f), 0.95f, 0.95f, UnitPalette.PropBlack, 14, rounded: false);
            m.Rod(new Vector3(0, 2.7f, 6.6f), new Vector3(0, 2.7f, 7.8f), 1.0f, 1.0f, UnitPalette.Funnel, 14, rounded: false);   // smokebox
            m.Rod(new Vector3(0, 3.4f, 6.9f), new Vector3(0, 4.5f, 6.9f), 0.32f, 0.38f, UnitPalette.PropBlack, 10, rounded: false); // chimney
            m.Ellipsoid(Matrix4x4.TRS(new Vector3(0, 3.6f, 1.8f), Quaternion.identity, new Vector3(0.5f, 0.45f, 0.65f)), UnitPalette.PropBlack, 10, 6);
            m.Box(Box(0, 2.85f, -5.6f, 2.9f, 2.7f, 3.2f), UnitPalette.PropBlack);    // cab
            m.Box(Box(0, 4.3f, -5.6f, 3.1f, 0.25f, 3.6f), UnitPalette.Funnel);       // cab roof
            m.Box(Box(0, 3.1f, -5.6f, 2.95f, 0.7f, 1.6f), UnitPalette.Glass);        // cab windows
            m.Box(Box(0, 1.2f, 8.3f, 2.9f, 0.5f, 0.3f), UnitPalette.MarkingRed);     // buffer beam
            return m.Build("TrainLocomotive");
        }

        public static Mesh Tender()
        {
            var m = new MiniatureMesh();
            m.Box(Box(0, 0.75f, 0, 2.3f, 1.1f, 6.8f), UnitPalette.Waterline);
            m.Box(Box(0, 2.15f, 0, 2.8f, 2.3f, 7.2f), UnitPalette.PropBlack);
            m.Box(Box(0, 3.35f, 0.3f, 2.4f, 0.25f, 5.6f), UnitPalette.Waterline);    // coal
            return m.Build("TrainTender");
        }

        public static Mesh Coach(string name)
        {
            var m = new MiniatureMesh();
            m.Box(Box(0, 0.75f, 0, 2.3f, 1.1f, 18f), UnitPalette.Waterline);        // bogies and underframe
            m.Box(Box(0, 2.35f, 0, 2.9f, 2.5f, 19.4f), UnitPalette.Trousers);       // body: olive-brown, as the promo's
            m.Box(Box(0, 2.85f, 0, 2.96f, 0.7f, 17.4f), UnitPalette.Glass);         // window band
            m.Ellipsoid(Matrix4x4.TRS(new Vector3(0, 3.6f, 0), Quaternion.identity, new Vector3(1.55f, 0.35f, 9.9f)), UnitPalette.Underside, 12, 6);   // roof: light grey
            return m.Build(name);
        }
    }
}

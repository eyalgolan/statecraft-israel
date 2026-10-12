using System;
using System.Collections.Generic;

namespace Statecraft.Game.Prototype
{
    // THROWAWAY PLAYTEST ONLY. Deliberately independent of Statecraft.Sim and content.
    // A fixed-step, seedless toy mission, not a historical reconstruction or campaign rule set.
    public enum CommandKind { Brigade, Scout, Worker, Convoy, Immigrants, Threat, Depot, Camp, Settlement }
    public enum CommandOutcome { Playing, Won, Lost }
    public readonly struct CommandPoint
    {
        public readonly float X, Y;
        public CommandPoint(float x, float y) { X = x; Y = y; }
        public static float Distance(CommandPoint a, CommandPoint b) {
            float x = a.X - b.X, y = a.Y - b.Y; return (float)Math.Sqrt(x * x + y * y);
        }
        public static CommandPoint Towards(CommandPoint a, CommandPoint b, float step) {
            float d = Distance(a, b); if (d <= step || d < .0001f) return b;
            return new CommandPoint(a.X + (b.X - a.X) * step / d, a.Y + (b.Y - a.Y) * step / d);
        }
    }
    public sealed class CommandUnit
    {
        public int Id; public CommandKind Kind; public CommandPoint Position, Destination;
        public float Health, MaxHealth, BuildRemaining, Cooldown, HarvestClock;
        public bool Moving, Gathering, Rescued;
        public int TargetId = -1, Cargo;
        public bool Alive => Health > 0;
        public bool Selectable => Alive && (Kind == CommandKind.Brigade || Kind == CommandKind.Scout || Kind == CommandKind.Worker || Kind == CommandKind.Convoy);
        public string Name => Kind == CommandKind.Scout ? "Palmach scouts" : Kind == CommandKind.Depot ? "Coastal depot" :
            Kind == CommandKind.Threat ? "Roadblock patrol" : Kind == CommandKind.Immigrants ? "Immigrant group" : Kind.ToString();
    }
    public sealed class CommandModel
    {
        public static readonly CommandPoint Base = new CommandPoint(24, 80), Grove = new CommandPoint(43, 73),
            Outpost = new CommandPoint(60, 20), RescueOne = new CommandPoint(18, 58), RescueTwo = new CommandPoint(71, 47);
        public readonly List<CommandUnit> Units = new List<CommandUnit>();
        public const double StepSeconds = .05, Deadline = 480;
        public int Money { get; private set; } = 140;
        public int Supplies { get; private set; } = 100;
        public int Deliveries { get; private set; }
        public int RescuedGroups { get; private set; }
        public int Wave { get; private set; }
        public double Elapsed { get; private set; }
        public bool Paused;
        public CommandOutcome Outcome { get; private set; }
        public string Report { get; private set; } = "Dispatch: build a camp, harvest the grove, and escort supplies south.";
        public int ReportVersion { get; private set; }
        public bool CampReady => HasReady(CommandKind.Camp);
        public bool SettlementReady => HasReady(CommandKind.Settlement);
        public event Action<CommandPoint, string> Feedback;
        double accumulator; int nextId;
        public CommandModel()
        {
            Spawn(CommandKind.Brigade, new CommandPoint(31, 77));
            Spawn(CommandKind.Scout, new CommandPoint(24, 70));
            Spawn(CommandKind.Convoy, new CommandPoint(30, 84)).Cargo = 30;
            Spawn(CommandKind.Worker, new CommandPoint(21, 84));
            Spawn(CommandKind.Depot, Base);
            Spawn(CommandKind.Immigrants, RescueOne);
            Spawn(CommandKind.Immigrants, RescueTwo);
            Spawn(CommandKind.Threat, new CommandPoint(72, 31));
            Spawn(CommandKind.Threat, new CommandPoint(83, 56));
        }
        CommandUnit Spawn(CommandKind kind, CommandPoint point)
        {
            float health = kind == CommandKind.Depot ? 500 : kind == CommandKind.Brigade ? 210 : kind == CommandKind.Threat ? 75 : 120;
            var unit = new CommandUnit { Id = nextId++, Kind = kind, Position = point, Destination = point, Health = health, MaxHealth = health };
            Units.Add(unit); return unit;
        }
        public CommandUnit Find(int id) { foreach (var u in Units) if (u.Id == id) return u; return null; }
        bool HasReady(CommandKind kind) { foreach (var u in Units) if (u.Alive && u.Kind == kind && u.BuildRemaining <= 0) return true; return false; }
        bool Has(CommandKind kind) { foreach (var u in Units) if (u.Alive && u.Kind == kind) return true; return false; }
        public void Announce(string text) { Report = text; ReportVersion++; }
        public bool Order(int id, CommandPoint point, int target = -1)
        {
            var u = Find(id);
            if (Outcome != CommandOutcome.Playing || u == null || !u.Selectable) return false;
            if (float.IsNaN(point.X) || float.IsNaN(point.Y) || float.IsInfinity(point.X) || float.IsInfinity(point.Y)) return false;
            u.Destination = new CommandPoint(Math.Max(3, Math.Min(97, point.X)), Math.Max(3, Math.Min(97, point.Y)));
            u.TargetId = target; u.Moving = true; u.Gathering = u.Kind == CommandKind.Worker && CommandPoint.Distance(point, Grove) < 8;
            u.HarvestClock = 0;
            Announce(u.Gathering ? "Workers assigned to citrus exports. Income begins on arrival." : u.Name + ": order acknowledged.");
            return true;
        }
        /// <summary>Assigns selected living workers to the grove without disturbing other units' orders.</summary>
        public int Harvest(IEnumerable<int> selectedIds)
        {
            int assigned = 0;
            foreach (int id in selectedIds) {
                var worker = Find(id);
                if (worker != null && worker.Alive && worker.Kind == CommandKind.Worker && Order(id, Grove)) assigned++;
            }
            return assigned;
        }
        public bool Build(int workerId, CommandKind kind)
        {
            if (Outcome != CommandOutcome.Playing) return false;
            var worker = Find(workerId);
            if (worker == null || !worker.Alive || worker.Kind != CommandKind.Worker) { Announce("Select a worker to build."); return false; }
            if (kind != CommandKind.Camp && kind != CommandKind.Settlement) return false;
            if (Has(kind)) { Announce("That building is already underway or complete."); return false; }
            var at = kind == CommandKind.Camp ? Base : Outpost;
            if (CommandPoint.Distance(worker.Position, at) > 11) { Announce(kind == CommandKind.Camp ? "Move a worker to the depot before building the camp." : "Move a worker to the southern outpost before founding the settlement."); return false; }
            int cost = kind == CommandKind.Camp ? 35 : 80, supplies = kind == CommandKind.Camp ? 20 : 50;
            if (Money < cost || Supplies < supplies) { Announce("Insufficient resources. Assign workers to the citrus grove."); return false; }
            Money -= cost; Supplies -= supplies;
            var building = Spawn(kind, new CommandPoint(at.X - 6, at.Y + 2));
            building.BuildRemaining = kind == CommandKind.Camp ? 8 : 15;
            worker.Moving = false; worker.Gathering = false;
            Announce(kind == CommandKind.Camp ? "Transit camp: construction started. Scouts can now bring immigrants home." : "Southern settlement: construction started. Hold the supply route.");
            Feedback?.Invoke(building.Position, "Construction started"); return true;
        }
        public bool Recruit()
        {
            if (Outcome != CommandOutcome.Playing) return false;
            if (Money < 55 || Supplies < 25) { Announce("Brigade needs $55k and 25 supplies. Harvest citrus to replenish both."); return false; }
            int count = 0; foreach (var u in Units) if (u.Alive && u.Kind == CommandKind.Brigade) count++;
            if (count >= 5) { Announce("All five brigade slots are deployed."); return false; }
            Money -= 55; Supplies -= 25;
            Spawn(CommandKind.Brigade, new CommandPoint(Base.X + 6 + count * 2, Base.Y - 5));
            Announce("Reinforcements ready at the coastal depot."); Feedback?.Invoke(Base, "Brigade ready"); return true;
        }
        public void Advance(double seconds)
        {
            if (Paused || Outcome != CommandOutcome.Playing || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            accumulator += Math.Min(seconds, 600);
            while (accumulator + 1e-9 >= StepSeconds && Outcome == CommandOutcome.Playing) { accumulator -= StepSeconds; Tick((float)StepSeconds); }
        }
        void Tick(float dt)
        {
            Elapsed += StepSeconds;
            if (Elapsed >= 50 + Wave * 55 && Wave < 7) {
                Wave++; var enemy = Spawn(CommandKind.Threat, new CommandPoint(93, Wave % 2 == 0 ? 68 : 26));
                enemy.Moving = true; enemy.Destination = Wave % 2 == 0 ? Base : Outpost;
                Announce("Radio: patrol approaching from the east. Brigade counters patrols; scouts avoid direct combat.");
                Feedback?.Invoke(enemy.Position, "Incoming patrol");
            }
            int count = Units.Count;
            for (int i = 0; i < count; i++) {
                var u = Units[i]; if (!u.Alive) continue;
                if (u.BuildRemaining > 0) {
                    u.BuildRemaining = Math.Max(0, u.BuildRemaining - dt);
                    if (u.BuildRemaining == 0) { Announce(u.Name + " complete."); Feedback?.Invoke(u.Position, u.Name + " ready"); }
                    continue;
                }
                if (u.Kind == CommandKind.Immigrants) { Rescue(u); continue; }
                u.Cooldown = Math.Max(0, u.Cooldown - dt);
                var target = Find(u.TargetId);
                if (target != null && target.Alive && target.Kind == CommandKind.Threat) {
                    u.Destination = target.Position;
                    u.Moving = CommandPoint.Distance(u.Position, target.Position) > Range(u);
                }
                else if (u.TargetId >= 0) { u.TargetId = -1; }
                if (u.Moving) {
                    u.Position = CommandPoint.Towards(u.Position, u.Destination, Speed(u) * dt);
                    if (CommandPoint.Distance(u.Position, u.Destination) < .1f) {
                        u.Moving = false; if (u.Selectable) Feedback?.Invoke(u.Position, "Arrived");
                    }
                }
                if (u.Kind == CommandKind.Worker && u.Gathering && !u.Moving && CommandPoint.Distance(u.Position, Grove) < 8) {
                    u.HarvestClock += dt;
                    if (u.HarvestClock >= 2) { u.HarvestClock -= 2; Money += 5; Supplies += 3; Feedback?.Invoke(u.Position, "+$5k  +3 supply"); }
                }
                if (u.Kind == CommandKind.Convoy) {
                    if (u.Cargo > 0 && CommandPoint.Distance(u.Position, Outpost) < 5) {
                        u.Cargo = 0; Deliveries++; Money += 20;
                        Announce("Supplies delivered south. Return the convoy to the coastal depot to reload."); Feedback?.Invoke(u.Position, "+1 delivery  +$20k");
                    }
                    if (u.Cargo == 0 && CommandPoint.Distance(u.Position, Base) < 7) { u.Cargo = 30; u.Health = u.MaxHealth; Feedback?.Invoke(u.Position, "Reloaded + repaired"); }
                }
                if (u.Selectable && CommandPoint.Distance(u.Position, Base) < 9) u.Health = Math.Min(u.MaxHealth, u.Health + dt * 6);
                if (u.Kind == CommandKind.Brigade || u.Kind == CommandKind.Scout || u.Kind == CommandKind.Threat) Fight(u);
            }
            var depot = Units.Find(u => u.Kind == CommandKind.Depot);
            bool convoyAlive = Units.Exists(u => u.Alive && u.Kind == CommandKind.Convoy);
            bool workersAvailable = Units.Exists(u => u.Alive && (u.Kind == CommandKind.Worker || u.Kind == CommandKind.Immigrants));
            if (depot == null || !depot.Alive || (!convoyAlive && Deliveries < 2) || !workersAvailable || Elapsed >= Deadline) {
                Outcome = CommandOutcome.Lost;
                Announce(Elapsed >= Deadline ? "Time expired. The southern route could not be secured." : "Mission failed. Protect the depot, convoy and workers. Restart to try another plan.");
            }
            else if (Deliveries >= 2 && RescuedGroups >= 2 && SettlementReady) {
                Outcome = CommandOutcome.Won; Announce("Route secured. Two immigrant groups sheltered, two deliveries through, and a new settlement standing.");
            }
        }
        void Rescue(CommandUnit group)
        {
            if (!group.Rescued) {
                foreach (var u in Units) if (u.Alive && (u.Kind == CommandKind.Scout || u.Kind == CommandKind.Brigade) && CommandPoint.Distance(u.Position, group.Position) < 7) {
                    group.Rescued = true; Announce("Immigrant group located. Escort them to the coastal camp."); Feedback?.Invoke(group.Position, "Following to camp"); break;
                }
            }
            if (!group.Rescued) return;
            group.Position = CommandPoint.Towards(group.Position, Base, 2.7f * (float)StepSeconds);
            if (CommandPoint.Distance(group.Position, Base) < 8 && CampReady) {
                group.Kind = CommandKind.Worker; group.Health = group.MaxHealth; RescuedGroups++;
                Announce("Immigrants sheltered. A new worker crew is ready at the depot."); Feedback?.Invoke(group.Position, "+1 worker crew");
            }
        }
        void Fight(CommandUnit u)
        {
            CommandUnit target = null; float nearest = Range(u);
            foreach (var candidate in Units) {
                if (!candidate.Alive || candidate == u) continue;
                bool hostile = u.Kind == CommandKind.Threat ? candidate.Kind != CommandKind.Threat && candidate.Kind != CommandKind.Immigrants : candidate.Kind == CommandKind.Threat;
                if (!hostile) continue;
                float distance = CommandPoint.Distance(u.Position, candidate.Position);
                if (distance < nearest) { nearest = distance; target = candidate; }
            }
            if (target == null || u.Cooldown > 0) return;
            float damage = u.Kind == CommandKind.Brigade ? 22 : u.Kind == CommandKind.Scout ? 5 : target.Kind == CommandKind.Convoy || target.Kind == CommandKind.Worker ? 15 : 9;
            target.Health = Math.Max(0, target.Health - damage); u.Cooldown = 1.15f;
            Feedback?.Invoke(target.Position, target.Alive ? "−" + (int)damage : "Disabled");
            if (!target.Alive && target.Kind == CommandKind.Threat) { Money += 12; Announce("Roadblock cleared. +$12k recovered supplies."); }
        }
        public static float Speed(CommandUnit u) => u.Kind == CommandKind.Scout ? 5 : u.Kind == CommandKind.Convoy ? 3.2f : u.Kind == CommandKind.Threat ? 1.3f : u.Kind == CommandKind.Worker ? 2.5f : 2.8f;
        public static float Range(CommandUnit u) => u.Kind == CommandKind.Brigade ? 12 : u.Kind == CommandKind.Scout ? 8 : 7;
    }
}

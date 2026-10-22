namespace Statecraft.Sim
{
    /// <summary>One line of the "Why did this happen?" report.</summary>
    public sealed class Cause
    {
        public string System { get; }
        public string Target { get; }
        public double Delta { get; }
        public string Message { get; }
        /// <summary>The order the enemy AI gave unit Target, when this cause records one (R-ENEMY-001): Movement.Kind,
        /// Combat.Attack or Orders.Fortify; else empty. The turn report words it in the player's language (#1293), as Message
        /// is English only. An order the unit already has is kept with no cause (R-ENEMY-003), so it is named only on the turn
        /// it is given.</summary>
        public string EnemyOrder { get; }
        public Cause(string system, string target, double delta, string message, string enemyOrder = "")
        { System = system; Target = target; Delta = delta; Message = PlayerText.Capitalised(message); EnemyOrder = enemyOrder; }   // sentence case (playtest 2, P2-6)
        public override string ToString() => $"[{System}] {Message}";
    }
}

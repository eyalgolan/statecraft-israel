namespace Statecraft.Sim
{
    /// <summary>Presentation categories of the effects the existing option helper returns; no additional rules.</summary>
    public enum OptionEffectPreviewKind { Amount, Stock, UnitGrant, City, ProjectUnlock, SetPolicy, SetRung, Rejoin }

    /// <summary>Read-only presentation context for an actual OptionEffects entry. Amount is already formatted by the sim.</summary>
    public sealed class OptionEffectPreview
    {
        public Cause Cause { get; }
        public OptionEffectPreviewKind Kind { get; }
        public string ContextId { get; }
        public string ContextName { get; }
        public string Field { get; }
        public string Amount { get; }

        internal OptionEffectPreview(Cause cause, OptionEffectPreviewKind kind, string contextId, string contextName,
            string amount, string field = "")
        {
            Cause = cause; Kind = kind; ContextId = contextId; ContextName = contextName; Amount = amount; Field = field;
        }
    }
}

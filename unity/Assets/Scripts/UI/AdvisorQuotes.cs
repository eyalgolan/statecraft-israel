namespace Statecraft.Game.UI
{
    /// <summary>Spec 3.3's verified quotes (Codex cx285, checked 23 October 2026): only these may show, each styled as a
    /// quote with its source line, and only as that list allows (Q1 and Q2 attributed to the Declaration, never to a person).
    /// Its text and source are advisor.quote.&lt;id&gt;.text and .source.</summary>
    public static class AdvisorQuotes
    {
        public static readonly string[] Ids = { "q1", "q2", "q3", "q4" };
    }
}

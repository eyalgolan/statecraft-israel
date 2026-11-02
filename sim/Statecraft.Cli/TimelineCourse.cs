using Statecraft.Sim;

namespace Statecraft.Cli
{
    /// <summary>`--timeline-course SAVE` (#1911, R-ALT-011): the course a saved game took, one tab-separated row per answered
    /// event: the option chosen, the record's option, whether it left the record, and the consequence card that brought it
    /// back. The rows are Timeline.Entries of the replayed state, the list the Unity timeline screen (TimelinePresenter)
    /// draws, so the two agree for the same choices, after a reload too. A developer report: it adds no state, never
    /// writes the save, and is not player text.</summary>
    public static class TimelineCourse
    {
        public const string Header = "event\tchosen\thistorical\tdiverged\trejoined_by";

        /// <summary>Writes the report to <paramref name="output"/> and returns 0. A file that cannot be read, or a save that
        /// GameSession.Load refuses, writes one "Save error" line to <paramref name="errors"/>, no report, and returns 1. Load
        /// warnings (other content, a replay that differs) go to <paramref name="errors"/> and the report still prints. A
        /// failure while writing the report is not caught here: it is not a save error.</summary>
        public static int Run(Content content, string savePath, TextWriter output, TextWriter errors)
        {
            string text;
            try { text = File.ReadAllText(savePath); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                errors.WriteLine($"Save error: cannot read {savePath}: {e.Message}");
                return 1;
            }

            GameSession session;
            IReadOnlyList<string> warnings;
            try { session = GameSession.Load(content, text, out warnings); }
            catch (SaveException e)
            {
                errors.WriteLine($"Save error{(e.Code != null ? " (" + e.Code + ")" : "")}: {e.Message}");
                return 1;
            }

            foreach (var w in warnings) errors.WriteLine("Save warning: " + w);
            output.WriteLine(Header);
            foreach (var e in Timeline.Entries(content, session.State))
                output.WriteLine(string.Join("\t", e.Event, e.Chosen, Dash(e.Historical), e.Diverged ? "true" : "false", Dash(e.RejoinedBy)));
            return 0;
        }

        static string Dash(string id) => id.Length == 0 ? "-" : id;
    }
}

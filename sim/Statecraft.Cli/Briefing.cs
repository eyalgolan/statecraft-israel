using Statecraft.Sim;

namespace Statecraft.Cli
{
    /// <summary>The turn's briefing in the text front end: the date, government and security level, then each pending
    /// card with its options.</summary>
    public static class Briefing
    {
        public static void Write(Content c, GameState s, TextWriter output)
        {
            var gov = Offices.Government(c, s.Date);
            var attention = c.UsesAttention ? $" | attention {s.Attention:0.0}" : "";
            output.WriteLine($"== {s.Date:yyyy-MM-dd} | turn {s.Turn} | {gov?.Name} | security level {s.Rung}{attention}");
            foreach (var id in s.PendingEvents)
            {
                var e = c.Events.First(x => x.Id == id);
                var fillDate = EventSystem.FillDate(e, s.Date);   // after the window ends, name the last in-window holder
                output.WriteLine($"  EVENT {e.Id}: {Offices.Fill(c, e.Title, fillDate)} - {EventSystem.Render(c, e, s.Date)}");
                foreach (var o in e.Options) output.WriteLine($"    {o.Id}: {Offices.Fill(c, o.Text, fillDate)}");
            }
        }
    }
}

#nullable enable
using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Star, Report and Share on the map screen (#1236, v0.1.4): three round icons, one above the other, at the foot
    /// of the left icon lane (Game.uxml community-column). Each does exactly what its Settings button does: Star opens the
    /// repository in a new tab (ExternalLink, inside the press), Report opens the report panel (FeedbackPresenter) and Share the
    /// share panel (SharePresenter), and Close gives the focus back to the icon. Icon-only, as the side menu: each one's name is
    /// its text, drawn clear, and its tooltip, the Settings button's own string, which HoverTip shows beside the disc.</summary>
    public sealed class CommunityIconsPresenter : IDisposable
    {
        public const string ColumnName = "community-column";

        readonly Button star, report, share;
        readonly FeedbackPresenter feedback; readonly SharePresenter sharePanel;
        readonly Action unbindStar;
        readonly HoverTip? tips;

        public CommunityIconsPresenter(VisualElement root, UiText text, FeedbackPresenter feedback, SharePresenter share)
        {
            this.feedback = feedback; sharePanel = share;
            var column = root.Q(ColumnName);
            star = column.Q<Button>("hud-star"); report = column.Q<Button>("hud-report"); this.share = column.Q<Button>("hud-share");
            star.text = text["community.star"]; report.text = text["feedback.open"]; this.share.text = text["share.open"];
            foreach (var b in new[] { star, report, this.share }) b.tooltip = b.text;
            unbindStar = ExternalLink.Bind(star, ExternalLink.Repository);
            report.clicked += OpenReport;
            this.share.clicked += OpenShare;
            if (root.Q<Label>(HoverTip.TipName) != null) tips = new HoverTip(root, column.Query<Button>().ToList(), () => HoverTip.Side.Right);
        }

        void OpenReport() => feedback.Open(report);
        void OpenShare() => sharePanel.Open(share);

        public void Dispose()
        {
            unbindStar();
            report.clicked -= OpenReport;
            share.clicked -= OpenShare;
            tips?.Dispose();
        }
    }
}

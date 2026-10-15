"""Keep the human review guide honest about the current schema, tests and gate."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def read(path):
    return (ROOT / path).read_text(encoding='utf-8')


def test_guide_is_current_process_not_a_fabricated_signoff():
    guide = read('docs/HISTORIAN_REVIEW.md')
    for text in ('not yet a gate', 'leaving `status` unchanged', 'affirmative human review',
                 'exact commit', 'Historical accuracy', 'first genuine reviewed batch remains outstanding',
                 'not YAML `status` values', 'content hash', 'AI check'):
        assert text in guide
    assert 'EveryHistoricalRecordIsSourcedAndFactChecked' in read('sim/Statecraft.Sim.Tests/RealContentTests.cs')
    assert '? ReleaseGate.ReviewedStatus : "fact-checked"' in read('sim/Statecraft.Sim.Tests/RealContentTests.cs')
    assert 'public const string ReviewedStatus = "sensitivity-reviewed"' in read('sim/Statecraft.Sim/ReleaseGate.cs')
    # Owner decision, 15 October 2026 (#375, "Yes, humans only"): the reversal of "no reviewed_by keys", dated, with its
    # guard. A status row now carries a person's sign-off.
    event = re.search(r'class GameEvent\b.*?\n    }', read('sim/Statecraft.Sim/Content.cs'), re.S).group(0)
    assert 'ReviewedBy' in event and 'ReviewedOn' in event
    for text in ('Changed on 15 October 2026', '#375', 'Yes, humans only', 'reviewed_by_guard.py', 'fails closed',
                 'never an email'):
        assert text in guide


def test_review_example_adds_only_comments_and_links_resolve():
    guide = read('docs/HISTORIAN_REVIEW.md')
    sample = re.search(r'```yaml\n(.*?)```', guide, re.S).group(1)
    assert all(not line.strip() or line.lstrip().startswith('#') for line in sample.splitlines())   # no sign-off of its own
    for link in re.findall(r'\]\(([^)]+)\)', guide):
        if not link.startswith('https://'):
            assert (ROOT / 'docs' / link.split('#')[0]).resolve().is_file(), link
    for tag in ('holocaust', 'terror', 'massacre', 'war-casualties'):
        assert f'`{tag}`' in guide

# /// script
# requires-python = ">=3.11"
# dependencies = ["requests==2.32.5", "pyyaml==6.0.3"]
# ///
"""Write ASSETS.portraits.yaml: one provenance rule per portrait file, read from Wikimedia Commons (licence short name,
the licence templates on the file page, and the artist). A portrait is 'verified' only when it is public domain or
CC0 AND its page carries an explicit US-status template (PD-Israel alone does not settle US copyright). A file whose page
carries an explicit Cc-by-4.0 template and a named artist is recorded as CC-BY-4.0, 'verified' only when NOTICE also
has an attribution entry for its Commons page (owner ruling, #30). CC BY-SA 4.0 follows the same rule with an explicit
Cc-by-sa-4.0 template (owner ruling, 2026-10-14, D-P); its row also notes that the cropped portrait is an adaptation and
so is itself released under CC BY-SA 4.0 (share-alike). Anything else is 'owner-verify'; a licence that is none of these
is recorded as licence 'unknown', which the release gate refuses outright.

Usage: uv run scripts/oss/commons_licences.py   (network: commons.wikimedia.org, read-only)"""
from __future__ import annotations

import html
import re
import sys
from pathlib import Path
from urllib.parse import unquote, urlparse

ROOT = Path(__file__).resolve().parents[2]
UA = {"User-Agent": "statecraft-israel-licence-audit/1.0 (https://github.com/eyalgolan/statecraft-israel)"}
# Only the {{PD-1996}} licence tag itself. {{PD-1996-text}} is a text helper that other tags also transclude,
# including PD-India-URAA, PD-Poland and PD-HU-unknown, so on its own it is not an explicit US status.
US_1996_TEMPLATES = {"PD-1996"}
# The dated composite tags on the Weizmann source explicitly include US expiry.
US_EXPIRED_COMPOSITES = {"PD-old-70-expired", "PD-old-70-1923"}
# Commons artist values that name nobody: a CC BY or CC BY-SA attribution needs a named author.
UNNAMED_ARTISTS = {"unknown", "unknown author", "author unknown", "unknown photographer", "anonymous",
                   "anonymous photographer"}
# Portrait files with no people.yaml record (promo/CREDITS.md lists Dayan as CC0).
EXTRA = [("promo/ref/people/Moshe_Dayan_1962_CC0.jpg",
          "https://commons.wikimedia.org/wiki/File:Moshe_Dayan_met_mgr._A.C._Ramselaar_(cropped).jpg")]


def _has_us_status(name: str) -> bool:
    # Shared text/localisation helpers are not licence tags in their own right.
    if "/" in name or re.search(r"-text(?:$|-)", name):
        return False
    return name.startswith("PD-US") or name == "Cc-zero" or name in US_1996_TEMPLATES or name in US_EXPIRED_COMPOSITES


def rule_for(path: str, source: str, licence_short: str, templates: list[str], artist: str,
             attributed: bool = False) -> dict:
    names = [t.removeprefix("Template:") for t in templates]
    low = licence_short.lower()
    cc_by = low == "cc by 4.0" and "Cc-by-4.0" in names
    cc_by_sa = low == "cc by-sa 4.0" and "Cc-by-sa-4.0" in names
    licence = "CC0-1.0" if low.startswith("cc0") else "Public-Domain" if low.startswith(("public domain", "pd")) \
        else "CC-BY-4.0" if cc_by else "CC-BY-SA-4.0" if cc_by_sa else "unknown"
    rule = {"glob": path, "licence": licence, "source": source, "status": "verified",
            "commons": f"{licence_short}; {', '.join(names)}",
            "artist": re.sub(r"<[^>]+>", "", html.unescape(artist)).strip()}
    if licence == "unknown":
        rule.update(status="owner-verify", note=f"Commons licence '{licence_short}' is not public domain, CC0, CC BY 4.0 or "
                                                "CC BY-SA 4.0")
    elif cc_by or cc_by_sa:
        name = "CC BY 4.0" if cc_by else "CC BY-SA 4.0"
        if not rule["artist"] or rule["artist"].lower() in UNNAMED_ARTISTS:
            rule.update(status="owner-verify", note=f"{name} needs a named artist for the attribution")
        elif not attributed:
            rule.update(status="owner-verify", note=f"{name} needs an attribution entry for this file in NOTICE")
        elif cc_by_sa:
            rule["note"] = ("share-alike: this cropped portrait is an adaptation, released under CC BY-SA 4.0 like its "
                            "source (LICENSING.md, NOTICE)")
    elif any(n == "Not-PD-US-URAA" or n.startswith("Not-PD-US-URAA/") for n in names):
        rule.update(status="owner-verify", note="Commons explicitly warns Not-PD-US-URAA: US copyright restored")
    elif not any(_has_us_status(n) for n in names):
        rule.update(status="owner-verify", note="no explicit US status template on the Commons page")
    return rule


def _commons(title: str, session) -> tuple[str, list[str], str]:
    j = session.get("https://commons.wikimedia.org/w/api.php", timeout=30, params={
        "action": "query", "titles": title, "prop": "imageinfo|templates", "iiprop": "extmetadata",
        "tlnamespace": 10, "tllimit": "max", "format": "json", "redirects": 1}).json()
    page = next(iter(j["query"]["pages"].values()))
    meta = (page.get("imageinfo") or [{}])[0].get("extmetadata", {})
    templates = [t["title"] for t in page.get("templates", [])
                 if re.match(r"Template:(PD-|Not-PD-US-URAA(?:/|$)|Cc-zero|Cc-by)", t["title"], re.I)]
    return meta.get("LicenseShortName", {}).get("value", "UNKNOWN"), templates, meta.get("Artist", {}).get("value", "")


def main() -> int:
    import requests
    import yaml
    people = yaml.safe_load((ROOT / "content/people.yaml").read_text(encoding="utf-8"))["people"]
    pairs: list[tuple[str, str]] = []
    for p in people:
        portrait = p.get("portrait")
        if not portrait:
            continue
        pairs.append((portrait["file"], portrait["source"]))
        built = f"unity/Assets/UI/Portraits/{p['id']}.jpg"
        if (ROOT / built).exists():
            pairs.append((built, portrait["source"]))
    pairs += EXTRA
    notice = (ROOT / "NOTICE").read_text(encoding="utf-8")
    s = requests.Session()
    s.headers.update(UA)
    rules = []
    for path, source in pairs:
        title = "File:" + unquote(urlparse(source).path.rsplit("File:", 1)[1]).replace("_", " ")
        short, templates, artist = _commons(title, s)
        rules.append(rule_for(path, source, short, templates, artist, attributed=source in notice))
        print(f"{rules[-1]['status']:13} {path}  [{rules[-1]['commons']}]")
    header = ("# GENERATED by scripts/oss/commons_licences.py from content/people.yaml and Wikimedia Commons. Do not edit\n"
              "# by hand: re-run the script. A PD or CC0 row is 'verified' only with an explicit US-status template; a CC BY 4.0\n"
              "# or CC BY-SA 4.0 row only with its Cc-by-4.0 or Cc-by-sa-4.0 template, a named artist and a NOTICE entry.\n")
    (ROOT / "ASSETS.portraits.yaml").write_text(
        header + yaml.safe_dump({"rules": rules}, sort_keys=False, allow_unicode=True, width=120), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())

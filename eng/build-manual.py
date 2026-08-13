#!/usr/bin/env python3
"""Assemble the user manual site (D-038/D-050) from docs/manual/notes/.

The notes stay the single source of truth; this generator turns them into
a self-contained static site (no external assets) in the CUETools 2026
identity, ready for GitHub Pages. Evidence screenshots referenced by name
in the notes are copied beside the pages.

Usage: python3 eng/build-manual.py   (writes docs/site/)
"""

import html
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
NOTES = ROOT / "docs" / "manual" / "notes"
EVIDENCE = ROOT / "docs" / "evidence"
OUT = ROOT / "docs" / "site"

# Reading order and human titles; every note must be listed so a new note
# is a conscious addition, not an accident.
PAGES = [
    ("install", "Install & Run"),
    ("verify", "Verify"),
    ("repair", "Repair"),
    ("offline-and-backfill", "Offline & Backfill"),
    ("convert", "Convert"),
    ("queue", "Queue"),
    ("codecs", "Codecs"),
    ("enrich", "Enrich"),
    ("settings", "Settings"),
]

CSS = """
:root {
  --ground: #0c0f0d; --panel: #111613; --face: #151b17; --line: #28312a;
  --ink: #edf1e9; --ink-dim: #d4dcd2; --muted: #7d887c;
  --teal: #34cfc0; --amber: #e9a63f; --good: #5ccb8b;
  --glass: rgba(255,255,255,0.03); --glass-line: rgba(255,255,255,0.07);
}
* { margin: 0; padding: 0; box-sizing: border-box; }
body {
  background: var(--ground); color: var(--ink-dim);
  font: 15px/1.65 Georgia, 'Times New Roman', serif;
}
header {
  background: var(--panel); border-bottom: 1px solid var(--line);
  padding: 18px 24px; display: flex; align-items: baseline; gap: 10px;
  flex-wrap: wrap;
}
header .brand { font-size: 21px; font-weight: 600; color: var(--ink); }
header .brand em { color: var(--teal); font-style: normal; }
header .sub {
  color: var(--muted); font-family: ui-monospace, 'Cascadia Mono', monospace;
  font-size: 11px;
}
.layout { display: flex; max-width: 1060px; margin: 0 auto; }
nav {
  width: 210px; flex-shrink: 0; padding: 26px 14px;
  border-right: 1px solid var(--line); min-height: calc(100vh - 60px);
}
nav .group {
  color: var(--muted); font-family: ui-monospace, monospace;
  font-size: 10.5px; letter-spacing: 0.08em; margin: 0 0 8px 10px;
}
nav a {
  display: block; color: var(--ink-dim); text-decoration: none;
  padding: 7px 10px; border-radius: 6px; font-size: 14px; margin-bottom: 2px;
}
nav a:hover { background: var(--glass); }
nav a.current {
  background: var(--face); border: 1px solid var(--teal); color: var(--ink);
}
main { flex: 1; padding: 30px 34px 60px; min-width: 0; }
main h1 {
  color: var(--ink); font-size: 27px; font-weight: 600; margin-bottom: 18px;
}
main h2 {
  color: var(--teal); font-size: 13px; font-weight: 700;
  font-family: ui-monospace, monospace; letter-spacing: 0.06em;
  text-transform: uppercase; margin: 26px 0 10px;
}
main p { margin: 0 0 12px; }
main ul { margin: 0 0 12px 22px; }
main li { margin-bottom: 6px; }
main strong { color: var(--ink); }
main code {
  font-family: ui-monospace, 'Cascadia Mono', monospace; font-size: 13px;
  background: var(--glass); border: 1px solid var(--glass-line);
  border-radius: 4px; padding: 1px 5px; color: var(--ink);
}
main pre {
  background: var(--panel); border: 1px solid var(--line); border-radius: 8px;
  padding: 12px 14px; overflow-x: auto; margin: 0 0 12px;
}
main pre code { background: none; border: none; padding: 0; }
main img {
  max-width: 100%; border: 1px solid var(--line); border-radius: 10px;
  margin: 6px 0 14px; display: block;
}
main a { color: var(--teal); }
footer {
  color: var(--muted); border-top: 1px solid var(--line);
  margin-top: 40px; padding-top: 14px; font-size: 12.5px;
}
@media (max-width: 760px) {
  .layout { flex-direction: column; }
  nav { width: auto; min-height: 0; border-right: none;
        border-bottom: 1px solid var(--line); }
}
"""

SHELL = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{title} - CUETools Linux Manual</title>
<style>{css}</style>
</head>
<body>
<header>
  <span class="brand">CUETOOLS <em>LINUX</em></span>
  <span class="sub">user manual</span>
</header>
<div class="layout">
<nav>
  <p class="group">MANUAL</p>
{nav}
</nav>
<main>
{body}
<footer>CUETools Linux - GPL-2.0-or-later. Every number in this manual is
a measured value from the evidence runs recorded in the repository.</footer>
</main>
</div>
</body>
</html>
"""


def inline(text: str) -> str:
    text = html.escape(text, quote=False)
    text = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"`([^`]+)`", r"<code>\1</code>", text)
    # bare evidence screenshot names become images
    text = re.sub(
        r"(?<![\w/])((?:\d{4}-\d{2}-\d{2}-)[\w.-]+\.png)",
        r'<img src="img/\1" alt="\1">',
        text)
    return text


def convert(markdown: str) -> str:
    out, para, in_list, in_code = [], [], False, False
    code: list[str] = []

    def flush_para():
        nonlocal para
        if para:
            out.append("<p>" + inline(" ".join(para)) + "</p>")
            para = []

    def close_list():
        nonlocal in_list
        if in_list:
            out.append("</ul>")
            in_list = False

    for line in markdown.splitlines():
        if in_code:
            if line.startswith("    ") or line.strip() == "":
                code.append(line[4:])
                continue
            out.append("<pre><code>" + html.escape("\n".join(code).rstrip()) + "</code></pre>")
            code, in_code = [], False
        if line.startswith("# "):
            flush_para(); close_list()
            out.append("<h1>" + inline(line[2:]) + "</h1>")
        elif line.startswith("## "):
            flush_para(); close_list()
            out.append("<h2>" + inline(line[3:]) + "</h2>")
        elif line.startswith("- "):
            flush_para()
            if not in_list:
                out.append("<ul>")
                in_list = True
            out.append("<li>" + inline(line[2:]) + "</li>")
        elif line.startswith("  ") and in_list and line.strip():
            out[-1] = out[-1][:-5] + " " + inline(line.strip()) + "</li>"
        elif line.startswith("    ") and not para and not in_list:
            in_code = True
            code = [line[4:]]
        elif line.strip() == "":
            flush_para(); close_list()
        else:
            close_list()
            para.append(line.strip())
    if in_code:
        out.append("<pre><code>" + html.escape("\n".join(code).rstrip()) + "</code></pre>")
    flush_para(); close_list()
    return "\n".join(out)


def main() -> None:
    notes = {p.stem for p in NOTES.glob("*.md")}
    listed = {slug for slug, _ in PAGES}
    missing = notes - listed
    if missing:
        raise SystemExit(f"unlisted manual notes (add to PAGES): {sorted(missing)}")

    if OUT.exists():
        shutil.rmtree(OUT)
    (OUT / "img").mkdir(parents=True)

    used_images: set[str] = set()
    bodies: dict[str, str] = {}
    for slug, _title in PAGES:
        markdown = (NOTES / f"{slug}.md").read_text()
        used_images.update(re.findall(r"(?<![\w/])(\d{4}-\d{2}-\d{2}-[\w.-]+\.png)", markdown))
        bodies[slug] = convert(markdown)

    for image in sorted(used_images):
        source = EVIDENCE / image
        if source.exists():
            shutil.copy2(source, OUT / "img" / image)

    for slug, title in PAGES:
        nav = "\n".join(
            f'  <a href="{s}.html"{" class=\"current\"" if s == slug else ""}>{t}</a>'
            for s, t in PAGES)
        page = SHELL.format(title=title, css=CSS, nav=nav, body=bodies[slug])
        (OUT / f"{slug}.html").write_text(page)

    index = SHELL.format(
        title="Manual", css=CSS,
        nav="\n".join(f'  <a href="{s}.html">{t}</a>' for s, t in PAGES),
        body=(
            "<h1>The CUETools Linux Manual</h1>"
            "<p>CUETools Linux verifies, repairs, converts, and enriches CD rips "
            "against the AccurateRip and CUETools databases - the native Linux "
            "port of CUETools 2026. Start with <a href='install.html'>Install "
            "&amp; Run</a>, or jump to any page on the left.</p>"
            "<p>Everything this manual claims is backed by a recorded evidence "
            "run; where a number appears, it was measured.</p>"))
    (OUT / "index.html").write_text(index)
    print(f"manual: {len(PAGES) + 1} pages, {len(used_images)} images -> {OUT}")


if __name__ == "__main__":
    main()

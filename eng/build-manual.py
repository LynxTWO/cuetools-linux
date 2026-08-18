#!/usr/bin/env python3
"""Assemble the user manual site (D-038/D-050).

Two source layers feed the site:

- `docs/manual/pages/`: the human manual, written for someone doing a
  task. When a page exists here it is what the site publishes.
- `docs/manual/notes/`: the engineering sourcebook, kept as the record of
  what was verified. A topic with no rewritten page yet is published from
  its note and marked as such, so migration progress is visible.

Glossary terms are defined once in `docs/manual/pages/glossary.md`. Any
link to `glossary.md#term` gains a hover definition pulled from that
entry, so a reader gets the short answer without leaving the page.

Usage: python3 eng/build-manual.py   (writes docs/site/)
"""

import html
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MANUAL = ROOT / "docs" / "manual"
NOTES = MANUAL / "notes"
PAGES_DIR = MANUAL / "pages"
EVIDENCE = ROOT / "docs" / "evidence"
OUT = ROOT / "docs" / "site"

# Reading order and human titles; every source file must be listed so a
# new one is a conscious addition, not an accident.
PAGES = [
    ("install", "Install & Run"),
    ("rip", "Rip"),
    ("verify", "Verify"),
    ("repair", "Repair"),
    ("offline-and-backfill", "Offline & Backfill"),
    ("convert", "Convert"),
    ("queue", "Queue"),
    ("codecs", "Codecs"),
    ("enrich", "Enrich"),
    ("settings", "Settings"),
    ("glossary", "Glossary"),
]

# The landing page is organised by what the reader came to do, not by the
# reading order in the sidebar.
LANDING_CARDS = [
    ("verify", "Check a rip you already have",
     "Compare an album on disk against two community databases and read the verdict."),
    ("repair", "Fix a damaged album",
     "Rebuild damaged audio from CTDB recovery data, into a new copy."),
    ("rip", "Rip a CD",
     "Read a disc, check it as it goes, and publish the result."),
    ("convert", "Convert an album",
     "Re-encode a whole album into another format without touching the original."),
    ("install", "Install and run it",
     "What your system needs, how to install, and what leaves your machine."),
    ("codecs", "See what formats work",
     "Which audio formats this build reads and writes, and their limits."),
]

NOTE_BANNER = (
    "This page is still an engineering note. It is accurate, but it is "
    "written for the people building CUETools; a rewritten version is "
    "coming."
)

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
nav.side {
  width: 210px; flex-shrink: 0; padding: 26px 14px;
  border-right: 1px solid var(--line); min-height: calc(100vh - 60px);
}
.side .group {
  color: var(--muted); font-family: ui-monospace, monospace;
  font-size: 10.5px; letter-spacing: 0.08em; margin: 0 0 8px 10px;
}
.side a {
  display: block; color: var(--ink-dim); text-decoration: none;
  padding: 7px 10px; border-radius: 6px; font-size: 14px; margin-bottom: 2px;
}
.side a:hover { background: var(--glass); }
.side a.current {
  background: var(--face); border: 1px solid var(--teal); color: var(--ink);
}
main { flex: 1; padding: 30px 34px 60px; min-width: 0; }
main h1 {
  color: var(--ink); font-size: 27px; font-weight: 600; margin-bottom: 18px;
  text-wrap: balance;
}
main h2 {
  color: var(--teal); font-size: 13px; font-weight: 700;
  font-family: ui-monospace, monospace; letter-spacing: 0.06em;
  text-transform: uppercase; margin: 32px 0 10px;
}
main h3 {
  color: var(--ink); font-size: 16px; font-weight: 600; margin: 22px 0 8px;
}
main p { margin: 0 0 12px; max-width: 68ch; }
main ul, main ol { margin: 0 0 12px 22px; max-width: 68ch; }
main li { margin-bottom: 6px; }
main ol > li { margin-bottom: 10px; }
main ol ul { margin: 8px 0 0 18px; }
main li::marker { color: var(--muted); }
main strong { color: var(--ink); }
main em { color: var(--ink-dim); }
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
.tablewrap { overflow-x: auto; margin: 0 0 14px; }
main table {
  border-collapse: collapse; width: 100%; font-size: 13.5px;
  font-variant-numeric: tabular-nums; font-family: system-ui, sans-serif;
}
main th, main td {
  border: 1px solid var(--line); padding: 7px 11px; text-align: left;
  vertical-align: top;
}
main th {
  background: var(--face); color: var(--ink); font-weight: 600;
  font-family: ui-monospace, monospace; font-size: 11px;
  letter-spacing: 0.06em; text-transform: uppercase;
}
main td { background: var(--panel); }
main figure { margin: 0 0 16px; }
main img {
  max-width: 100%; border: 1px solid var(--line); border-radius: 10px;
  margin: 6px 0 0; display: block;
}
main figcaption {
  color: var(--muted); font-size: 13px; font-style: italic;
  margin-top: 7px; max-width: 68ch;
}
main a { color: var(--teal); }
.notebanner {
  background: var(--panel); border: 1px solid var(--line);
  border-left: 3px solid var(--amber); border-radius: 0 8px 8px 0;
  padding: 10px 14px; margin: 0 0 20px; color: var(--muted);
  font-size: 13.5px; max-width: 68ch;
}
.gl {
  border-bottom: 1px dotted var(--teal); text-decoration: none;
  position: relative;
}
.gl:hover::after, .gl:focus-visible::after {
  content: attr(data-def);
  position: absolute; left: 0; top: calc(100% + 7px); z-index: 5;
  width: max-content; max-width: 320px;
  background: var(--face); color: var(--ink-dim);
  border: 1px solid var(--teal); border-radius: 8px;
  padding: 9px 12px; font-size: 13px; line-height: 1.5;
  font-family: system-ui, sans-serif; font-style: normal;
  box-shadow: 0 8px 22px rgba(0,0,0,0.5);
}
main dl { margin: 0 0 12px; }
a:focus-visible, .side a:focus-visible {
  outline: 2px solid var(--teal); outline-offset: 2px;
}
footer {
  color: var(--muted); border-top: 1px solid var(--line);
  margin-top: 40px; padding-top: 14px; font-size: 12.5px;
}
.sr-only {
  position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px;
  overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; border: 0;
}
.side .search { margin: 0 0 14px; }
.side .search input {
  width: 100%; background: var(--ground); color: var(--ink);
  border: 1px solid var(--line); border-radius: 6px;
  padding: 7px 10px; font: inherit; font-size: 13px;
  font-family: ui-monospace, 'Cascadia Mono', monospace;
}
.side .search input::placeholder { color: var(--muted); }
.side .search input:focus {
  outline: none; border-color: var(--teal);
}
#results .noresults {
  color: var(--muted); font-size: 13px; padding: 4px 10px;
}
a.result {
  display: block; padding: 8px 10px; margin-bottom: 4px;
  border-radius: 6px; text-decoration: none; border: 1px solid transparent;
}
a.result:hover, a.result:focus-visible {
  background: var(--face); border-color: var(--line);
}
a.result .rpage {
  display: block; font-family: ui-monospace, monospace; font-size: 9.5px;
  letter-spacing: 0.1em; text-transform: uppercase; color: var(--teal);
}
a.result .rtitle {
  display: block; color: var(--ink); font-size: 13.5px; margin: 2px 0 1px;
}
a.result .rtext {
  display: block; color: var(--muted); font-size: 12px; line-height: 1.45;
}
.pager {
  display: flex; gap: 12px; justify-content: space-between;
  margin-top: 42px; padding-top: 18px; border-top: 1px solid var(--line);
}
.pager a {
  flex: 1 1 0; min-width: 0; text-decoration: none;
  border: 1px solid var(--line); border-radius: 8px;
  padding: 10px 14px; background: var(--panel);
}
.pager a:hover { border-color: var(--teal); }
.pager a.next { text-align: right; }
.pager .dir {
  display: block; font-family: ui-monospace, monospace; font-size: 9.5px;
  letter-spacing: 0.1em; text-transform: uppercase; color: var(--muted);
}
.pager .name { display: block; color: var(--ink); font-size: 14.5px; }
.cards {
  display: grid; grid-template-columns: repeat(auto-fit, minmax(232px, 1fr));
  gap: 14px; margin: 6px 0 26px;
}
.cards a {
  display: block; text-decoration: none; padding: 15px 17px;
  background: var(--panel); border: 1px solid var(--line); border-radius: 10px;
}
.cards a:hover { border-color: var(--teal); }
.cards .ctitle {
  display: block; color: var(--ink); font-size: 16px; margin-bottom: 5px;
}
.cards .cwhat {
  display: block; color: var(--ink-dim); font-size: 13.5px; line-height: 1.5;
}
@media (max-width: 760px) {
  .layout { flex-direction: column; }
  nav.side { width: auto; min-height: 0; border-right: none;
        border-bottom: 1px solid var(--line); }
  main { padding: 24px 18px 48px; }
  .pager { flex-direction: column; }
  .pager a.next { text-align: left; }
}
@media print {
  body { background: #fff; color: #111; font-size: 11pt; }
  header, .side, .pager, footer { display: none; }
  .layout { display: block; max-width: none; }
  main { padding: 0; }
  main h1, main h3, main strong, main .cards .ctitle { color: #000; }
  main h2 {
    color: #000; font-size: 12pt; border-bottom: 1px solid #999;
    padding-bottom: 3px;
  }
  main a { color: #000; text-decoration: underline; }
  main a[href^="http"]::after { content: " (" attr(href) ")"; font-size: 9pt; }
  main code, main pre, main th, main td, .cards a {
    background: #fff; border-color: #999; color: #000;
  }
  main figure, main table, main pre { break-inside: avoid; }
  main h2, main h3 { break-after: avoid; }
  main img { max-width: 60%; }
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
<nav class="side">
  <form class="search" role="search" onsubmit="return false">
    <label class="sr-only" for="q">Search the manual</label>
    <input id="q" type="search" placeholder="Search the manual"
           autocomplete="off" spellcheck="false">
  </form>
  <div id="results" hidden></div>
  <div id="chapters">
  <p class="group">MANUAL</p>
{nav}
  </div>
</nav>
<main>
{body}
{footer_nav}
<footer>CUETools Linux - GPL-2.0-or-later. Every number in this manual is
a measured value from the evidence runs recorded in the repository.</footer>
</main>
</div>
<script id="search-index" type="application/json">{index}</script>
<script>{script}</script>
</body>
</html>
"""

SCRIPT = """
(function () {
  var box = document.getElementById('q');
  var results = document.getElementById('results');
  var chapters = document.getElementById('chapters');
  var raw = document.getElementById('search-index');
  if (!box || !results || !chapters || !raw) return;
  var index;
  try { index = JSON.parse(raw.textContent); } catch (e) { return; }

  function render(matches, query) {
    if (!query) {
      results.hidden = true;
      chapters.hidden = false;
      return;
    }
    chapters.hidden = true;
    results.hidden = false;
    if (!matches.length) {
      results.innerHTML = '<p class="noresults">Nothing matches ' +
        '&ldquo;' + query.replace(/[<>&]/g, '') + '&rdquo;.</p>';
      return;
    }
    var html = '<p class="group">' + matches.length + ' RESULT' +
      (matches.length === 1 ? '' : 'S') + '</p>';
    matches.forEach(function (m) {
      html += '<a class="result" href="' + m.href + '">' +
        '<span class="rpage">' + m.page + '</span>' +
        '<span class="rtitle">' + m.title + '</span>' +
        '<span class="rtext">' + m.text + '</span></a>';
    });
    results.innerHTML = html;
  }

  function search(query) {
    var terms = query.toLowerCase().split(/\\s+/).filter(Boolean);
    if (!terms.length) return [];
    var scored = [];
    index.forEach(function (entry) {
      var hay = (entry.title + ' ' + entry.page + ' ' + entry.text).toLowerCase();
      var score = 0;
      for (var i = 0; i < terms.length; i++) {
        var at = hay.indexOf(terms[i]);
        if (at < 0) return;
        score += entry.title.toLowerCase().indexOf(terms[i]) >= 0 ? 12 : 1;
        score += at < 90 ? 3 : 0;
      }
      scored.push({ entry: entry, score: score });
    });
    scored.sort(function (a, b) { return b.score - a.score; });
    return scored.slice(0, 24).map(function (s) { return s.entry; });
  }

  var last = '';
  box.addEventListener('input', function () {
    var query = box.value.trim();
    if (query === last) return;
    last = query;
    render(query ? search(query) : [], query);
  });
  box.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
      box.value = '';
      last = '';
      render([], '');
      box.blur();
    }
    if (event.key === 'Enter') {
      var first = results.querySelector('a.result');
      if (first) window.location.href = first.getAttribute('href');
    }
  });
  document.addEventListener('keydown', function (event) {
    if (event.key === '/' && document.activeElement !== box) {
      event.preventDefault();
      box.focus();
    }
  });
})();
"""

# Filled by load_glossary(); anchor -> short definition.
GLOSSARY: dict[str, str] = {}


def slugify(text: str) -> str:
    """GitHub-style heading anchor.

    Each whitespace character becomes its own hyphen rather than collapsing a
    run into one, because that is what GitHub does and what anyone writing a
    link by hand will expect: "Test & Copy reads" loses the ampersand and keeps
    both surrounding spaces, giving "test--copy-reads".
    """
    text = re.sub(r"`([^`]*)`", r"\1", text)
    text = re.sub(r"\*\*(.+?)\*\*", r"\1", text)
    text = text.lower()
    text = re.sub(r"[^\w\s-]", "", text)
    return re.sub(r"[\s_]", "-", text).strip("-")


def link_target(target: str) -> str:
    """Rewrite in-manual markdown links to their generated pages."""
    if target.startswith(("http://", "https://", "#")):
        return target
    page, _, anchor = target.partition("#")
    if page.endswith(".md"):
        page = page[:-3] + ".html"
    return page + ("#" + anchor if anchor else "")


def render_link(text: str, target: str) -> str:
    href = link_target(target)
    page, _, anchor = href.partition("#")
    if page == "glossary.html" and anchor in GLOSSARY:
        return (f'<a class="gl" href="{href}" '
                f'data-def="{html.escape(GLOSSARY[anchor], quote=True)}">{text}</a>')
    return f'<a href="{href}">{text}</a>'


def inline(text: str, escape: bool = True) -> str:
    if escape:
        text = html.escape(text, quote=False)
    # markdown links first: their text may carry other inline markup
    def _link(match: re.Match) -> str:
        return render_link(inline(match.group(1), escape=False), match.group(2))
    text = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", _link, text)
    text = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"(?<![\w*])\*([^*\n]+)\*(?!\w)", r"<em>\1</em>", text)
    text = re.sub(r"`([^`]+)`", r"<code>\1</code>", text)
    text = text.replace(r"\|", "|")
    # bare evidence screenshot names (engineering notes) become images
    text = re.sub(
        r"(?<![\w/(])((?:\d{4}-\d{2}-\d{2}-)[\w.-]+\.png)(?![\w)])",
        r'<img src="img/\1" alt="\1">',
        text)
    return text


def split_row(line: str) -> list[str]:
    """Split a table row on unescaped pipes."""
    cells = re.split(r"(?<!\\)\|", line.strip())
    if cells and cells[0].strip() == "":
        cells = cells[1:]
    if cells and cells[-1].strip() == "":
        cells = cells[:-1]
    return [cell.strip() for cell in cells]


def is_divider(line: str) -> bool:
    cells = split_row(line)
    return bool(cells) and all(re.fullmatch(r":?-{2,}:?", c) for c in cells)


ITEM = re.compile(r"(?:[-*] |\d+\. )")


def convert_list(lines: list[str], start: int) -> tuple[str, int]:
    """Render one list block. Items own their wrapped lines and nested lists."""
    base = len(lines[start]) - len(lines[start].lstrip())
    ordered = bool(re.match(r"\d+\. ", lines[start].strip()))
    items: list[list[str]] = []
    index = start

    def same_level_item(line: str) -> bool:
        indent = len(line) - len(line.lstrip())
        if indent != base or not ITEM.match(line.strip()):
            return False
        return bool(re.match(r"\d+\. ", line.strip())) == ordered

    while index < len(lines):
        line = lines[index]
        if not line.strip():
            # a blank line ends the list unless indented content or another
            # item of this list follows it
            look = index + 1
            while look < len(lines) and not lines[look].strip():
                look += 1
            if look >= len(lines):
                break
            following = lines[look]
            indent = len(following) - len(following.lstrip())
            if same_level_item(following) or indent > base:
                if items:
                    items[-1].append("")
                index = look
                continue
            break
        if same_level_item(line):
            items.append([re.sub(r"^(?:[-*] |\d+\. )", "", line.strip())])
            index += 1
            continue
        indent = len(line) - len(line.lstrip())
        if items and indent > base:
            items[-1].append(line)
            index += 1
            continue
        break

    rendered = []
    for item in items:
        while item and not item[-1].strip():
            item.pop()
        head, tail = item[0], item[1:]
        if tail:
            pad = min((len(l) - len(l.lstrip()) for l in tail if l.strip()),
                      default=0)
            tail = [l[pad:] if l.strip() else "" for l in tail]
        body = convert("\n".join([head] + tail))
        # a single paragraph needs no block wrapper inside the item
        if body.startswith("<p>") and body.count("<p>") == 1 and \
                body.endswith("</p>") and "\n" not in body:
            body = body[3:-4]
        rendered.append("<li>" + body + "</li>")
    tag = "ol" if ordered else "ul"
    return f"<{tag}>" + "".join(rendered) + f"</{tag}>", index


def convert(markdown: str) -> str:
    lines = markdown.splitlines()
    out: list[str] = []
    para: list[str] = []
    index = 0

    def flush_para() -> None:
        nonlocal para
        if not para:
            return
        text = " ".join(para)
        para = []
        # an italic-only paragraph right after a figure is that figure's caption
        caption = re.fullmatch(r"\*(.+)\*", text.strip(), re.S)
        if caption and out and out[-1].endswith("</figure>"):
            out[-1] = out[-1][:-len("</figure>")] + \
                "<figcaption>" + inline(caption.group(1)) + "</figcaption></figure>"
            return
        out.append("<p>" + inline(text) + "</p>")

    while index < len(lines):
        line = lines[index]
        stripped = line.strip()

        if stripped.startswith("```"):
            flush_para()
            index += 1
            code: list[str] = []
            while index < len(lines) and not lines[index].strip().startswith("```"):
                code.append(lines[index])
                index += 1
            index += 1
            out.append("<pre><code>" +
                       html.escape("\n".join(code).rstrip()) + "</code></pre>")
            continue

        heading = re.match(r"(#{1,3}) +(.*)", stripped)
        if heading:
            flush_para()
            level = len(heading.group(1))
            title = heading.group(2)
            anchor = slugify(title)
            out.append(f'<h{level} id="{anchor}">{inline(title)}</h{level}>')
            index += 1
            continue

        # image on its own line becomes a figure
        image = re.fullmatch(r"!\[(.*)\]\(([^)]+)\)", stripped)
        if image:
            flush_para()
            alt = html.escape(image.group(1), quote=True)
            src = image.group(2)
            if not src.startswith(("http://", "https://", "img/")):
                src = "img/" + Path(src).name
            out.append(f'<figure><img src="{src}" alt="{alt}"></figure>')
            index += 1
            continue

        # table
        if stripped.startswith("|") and index + 1 < len(lines) \
                and is_divider(lines[index + 1]):
            flush_para()
            head = split_row(stripped)
            index += 2
            rows: list[list[str]] = []
            while index < len(lines) and lines[index].strip().startswith("|"):
                rows.append(split_row(lines[index].strip()))
                index += 1
            table = ['<div class="tablewrap"><table><thead><tr>']
            table += [f"<th>{inline(cell)}</th>" for cell in head]
            table.append("</tr></thead><tbody>")
            for row in rows:
                table.append("<tr>" +
                             "".join(f"<td>{inline(cell)}</td>" for cell in row) +
                             "</tr>")
            table.append("</tbody></table></div>")
            out.append("".join(table))
            continue

        # ordered or unordered list; items may carry wrapped text and nested lists
        if ITEM.match(stripped):
            flush_para()
            rendered, index = convert_list(lines, index)
            out.append(rendered)
            continue

        if not stripped:
            flush_para()
            index += 1
            continue

        para.append(stripped)
        index += 1

    flush_para()
    return "\n".join(out)


def load_glossary() -> None:
    """Map each glossary anchor to its first sentence, for hover definitions."""
    source = PAGES_DIR / "glossary.md"
    if not source.exists():
        return
    term = None
    body: list[str] = []

    def commit() -> None:
        if not term or not body:
            return
        text = " ".join(body).strip()
        text = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", text)
        text = re.sub(r"[`*]", "", text)
        sentence = re.split(r"(?<=\.)\s", text)[0]
        GLOSSARY[term] = sentence.strip()

    for line in source.read_text().splitlines():
        heading = re.match(r"## +(.*)", line.strip())
        if heading:
            commit()
            term, body = slugify(heading.group(1)), []
        elif term and term not in GLOSSARY and line.strip():
            body.append(line.strip())
        elif term and body:
            # only the entry's first paragraph becomes its hover definition
            commit()
            body = []
    commit()


TAG = re.compile(r"<[^>]+>")


def search_entries(slug: str, title: str, body: str) -> list[dict]:
    """One entry per section: its heading, and the prose that opens it.

    The index is inlined into every page, so it stays small on purpose. Section
    headings plus a snippet answer "which page and where" - the reader lands on the
    anchor and reads the rest there.
    """
    entries: list[dict] = []
    sections = re.split(r'<h([23]) id="([^"]+)">(.*?)</h[23]>', body)
    # sections[0] is whatever precedes the first heading
    lead = TAG.sub(" ", sections[0])
    lead = html.unescape(re.sub(r"\s+", " ", lead)).strip()
    if lead:
        entries.append({
            "page": title, "title": title, "href": f"{slug}.html",
            "text": lead[:180],
        })
    for level, anchor, heading, content in zip(
            sections[1::4], sections[2::4], sections[3::4], sections[4::4]):
        text = TAG.sub(" ", content)
        text = html.unescape(re.sub(r"\s+", " ", text)).strip()
        entries.append({
            "page": title,
            "title": html.unescape(TAG.sub("", heading)),
            "href": f"{slug}.html#{anchor}",
            "text": text[:180],
        })
    return entries


def pager(slug: str) -> str:
    order = [s for s, _ in PAGES]
    if slug not in order:
        return ""
    at = order.index(slug)
    titles = dict(PAGES)
    parts = ['<nav class="pager" aria-label="Manual pages">']
    if at > 0:
        previous = order[at - 1]
        parts.append(
            f'<a class="prev" href="{previous}.html">'
            f'<span class="dir">Previous</span>'
            f'<span class="name">{titles[previous]}</span></a>')
    if at < len(order) - 1:
        following = order[at + 1]
        parts.append(
            f'<a class="next" href="{following}.html">'
            f'<span class="dir">Next</span>'
            f'<span class="name">{titles[following]}</span></a>')
    parts.append("</nav>")
    return "\n".join(parts) if len(parts) > 2 else ""


def source_for(slug: str) -> tuple[Path, bool]:
    """The published source for a topic, and whether it is a raw note."""
    page = PAGES_DIR / f"{slug}.md"
    if page.exists():
        return page, False
    return NOTES / f"{slug}.md", True


def main() -> None:
    listed = {slug for slug, _ in PAGES}
    found = {p.stem for p in NOTES.glob("*.md")}
    found |= {p.stem for p in PAGES_DIR.glob("*.md")}
    missing = found - listed
    if missing:
        raise SystemExit(f"unlisted manual sources (add to PAGES): {sorted(missing)}")
    for slug, _title in PAGES:
        path, _ = source_for(slug)
        if not path.exists():
            raise SystemExit(f"listed page has no source file: {slug}")

    load_glossary()

    if OUT.exists():
        shutil.rmtree(OUT)
    (OUT / "img").mkdir(parents=True)

    used_images: set[str] = set()
    bodies: dict[str, str] = {}
    notes_published: list[str] = []
    for slug, _title in PAGES:
        path, is_note = source_for(slug)
        markdown = path.read_text()
        used_images.update(
            re.findall(r"(\d{4}-\d{2}-\d{2}-[\w.-]+\.png)", markdown))
        body = convert(markdown)
        if is_note:
            notes_published.append(slug)
            banner = f'<p class="notebanner">{NOTE_BANNER}</p>'
            head, split, rest = body.partition("</h1>")
            body = head + split + banner + rest if split else banner + body
        bodies[slug] = body

    for image in sorted(used_images):
        source = EVIDENCE / image
        if source.exists():
            shutil.copy2(source, OUT / "img" / image)

    entries: list[dict] = []
    for slug, title in PAGES:
        entries.extend(search_entries(slug, title, bodies[slug]))
    index_json = json.dumps(entries, separators=(",", ":"))

    for slug, title in PAGES:
        nav = "\n".join(
            f'  <a href="{s}.html"{" class=\"current\"" if s == slug else ""}>{t}</a>'
            for s, t in PAGES)
        page = SHELL.format(title=title, css=CSS, nav=nav, body=bodies[slug],
                            footer_nav=pager(slug), index=index_json, script=SCRIPT)
        (OUT / f"{slug}.html").write_text(page)

    cards = "\n".join(
        f'<a href="{slug}.html"><span class="ctitle">{title}</span>'
        f'<span class="cwhat">{what}</span></a>'
        for slug, title, what in LANDING_CARDS)
    index_page = SHELL.format(
        title="Manual", css=CSS,
        nav="\n".join(f'  <a href="{s}.html">{t}</a>' for s, t in PAGES),
        footer_nav="", index=index_json, script=SCRIPT,
        body=(
            "<h1>The CUETools Linux Manual</h1>"
            "<p>CUETools Linux checks CD rips against the AccurateRip and CUETools "
            "databases, repairs damaged ones, converts between lossless formats, and "
            "rips discs itself. It is the native Linux port of CUETools 2026.</p>"
            "<h2 id=\"start-with-what-you-want-to-do\">Start with what you want to do</h2>"
            f'<div class="cards">{cards}</div>'
            "<h2 id=\"about-this-manual\">About this manual</h2>"
            "<p>Every claim here was checked against the running program or its source "
            "code. Where a number appears, it was measured. Anything the project has not "
            "verified yet is recorded separately rather than guessed at, and terms you "
            "may not know link to the <a href=\"glossary.html\">glossary</a>.</p>"
            "<p>Press <code>/</code> to search from any page.</p>"))
    (OUT / "index.html").write_text(index_page)
    print(f"manual: {len(PAGES) + 1} pages "
          f"({len(PAGES) - len(notes_published)} rewritten, "
          f"{len(notes_published)} still notes: {', '.join(notes_published)}), "
          f"{len(GLOSSARY)} glossary terms, {len(entries)} search entries, "
          f"{len(used_images)} images -> {OUT}")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Build the Bublock docs webpage (site/index.html) from the repo's markdown and the knowledge graph.

One self-contained page: every doc in the repo, the glossary and indexes, and an
interactive graph from knowledge/generated/graph.json. Run knowledge-graph.py first
(scripts/knowledge.sh does both). See docs-site.py.md.
"""

import json
import os
import re
import sys
from datetime import datetime, timezone

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
OUT = os.path.join(ROOT, "site", "index.html")
GRAPH = os.path.join(ROOT, "knowledge", "generated", "graph.json")
SKIP_DIRS = {"bin", "obj", ".git", "lib", "logs", "site", "baseline", "maps", ".cursor"}
SKIP_FILES = {"cvarlist.md"}
EXTRA = ["RiftRoulette/reference/.rules"]
# The page can travel further than the repo: refuse to build if a doc holds a server detail.
IP = re.compile(r"\b(?:\d{1,3}\.){3}\d{1,3}\b")
ALLOWED_IPS = {"66.94.102.245"}  # Theo's public join address (README.md)
SERVER_ENV = os.path.join(ROOT, "scripts", "server.env")

NAV = [
    ("Start", [("Overview", "knowledge/README.md"), ("Glossary", "knowledge/glossary.md"),
               ("Changelog", "knowledge/changelog.md")]),
    ("Mental models", [("How Deadworks mods work", "knowledge/mental-models/how-deadworks-mods-work.md"),
                       ("Talking to the game", "knowledge/mental-models/talking-to-the-game.md"),
                       ("Rift Roulette architecture", "knowledge/mental-models/rift-roulette-architecture.md"),
                       ("Ship and operate", "knowledge/mental-models/ship-and-operate.md")]),
    ("Build a mode", [("Effects catalog", "knowledge/effects-catalog.md"),
                      ("Game-mode recipes", "knowledge/game-mode-recipes.md")]),
    ("Explore", [("Knowledge graph", "#graph"), ("Repo tree", "knowledge/generated/tree.md"),
                 ("Indexes", "knowledge/generated/indexes.md"), ("Deadworks API", "knowledge/generated/deadworks-api.md")]),
    ("Theo's reference", [("Development rules", "RiftRoulette/reference/.rules"),
                          ("Master plan", "RiftRoulette/reference/master-plan.md"),
                          ("Resources & discoveries", "RiftRoulette/reference/resources.md"),
                          ("Patch day", "RiftRoulette/reference/patch-day.md"),
                          ("Player commands", "RiftRoulette/reference/user-commands.md"),
                          ("Admin commands", "RiftRoulette/reference/admin-commands.md"),
                          ("Behavior inventory", "RiftRoulette/reference/behavior-inventory.md"),
                          ("Endless mode (shelved)", "RiftRoulette/reference/endless-mode.md"),
                          ("Chat handoff", "RiftRoulette/reference/chat-handoff.md"),
                          ("CI / CD", ".github/workflows/README.md"),
                          ("README", "README.md")]),
]


def rel(path):
    return os.path.relpath(path, ROOT).replace(os.sep, "/")


def title_of(path, text):
    for line in re.sub(r"^---\n.*?\n---\n", "", text, flags=re.S).splitlines():
        if line.startswith("# "):
            return line[2:].strip()
    return os.path.basename(path)


def collect_docs():
    docs = {}
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = sorted(d for d in dirnames if d not in SKIP_DIRS and (not d.startswith(".") or d in (".github",)))
        for name in sorted(filenames):
            if name.endswith(".md") and name not in SKIP_FILES:
                docs[rel(os.path.join(dirpath, name))] = None
    for extra in EXTRA:
        if os.path.exists(os.path.join(ROOT, extra)):
            docs[extra] = None
    secrets = server_secrets()
    out = {}
    for path in docs:
        with open(os.path.join(ROOT, path), encoding="utf-8") as f:
            text = f.read()
        leaks = [s for s in secrets if s in text] + [ip for ip in IP.findall(text) if ip not in ALLOWED_IPS]
        if leaks:
            sys.exit(f"refusing to build: {path} contains a server detail ({len(leaks)} hit(s)); keep them in scripts/server.env")
        out[path] = {"title": title_of(path, text), "text": text}
    return out


def server_secrets():
    """Non-empty DW_* values from the git-ignored server.env, which must never reach the page."""
    if not os.path.exists(SERVER_ENV):
        return []
    with open(SERVER_ENV, encoding="utf-8") as f:
        values = re.findall(r'^\s*DW_[A-Z_]+="?([^"#\n]+?)"?\s*(?:#.*)?$', f.read(), flags=re.M)
    return [v.strip() for v in values if len(v.strip()) >= 4]


def features(docs):
    items = [(p.rsplit("/", 2)[-2] if p.count("/") else "Repo", p) for p in docs if p.endswith("FEATURE.md")]
    return sorted(items, key=lambda i: (i[1].split("/")[0] != "RiftRoulette", i[1]))


def main():
    if not os.path.exists(GRAPH):
        sys.exit("error: run scripts/knowledge-graph.py first (knowledge/generated/graph.json missing).")
    with open(GRAPH, encoding="utf-8") as f:
        graph = json.load(f)
    docs = collect_docs()
    nav = [{"group": g, "items": [{"label": l, "path": p} for l, p in items if p.startswith("#") or p in docs]} for g, items in NAV]
    nav.append({"group": "Features", "items": [{"label": l, "path": p} for l, p in features(docs)]})
    bundle = {
        "meta": {"built": datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC"), "docs": len(docs),
                 "counts": graph["graph"].get("counts", {})},
        "nav": nav,
        "docs": docs,
        "graph": {"nodes": graph["nodes"], "links": graph["links"]},
    }
    payload = json.dumps(bundle, ensure_ascii=False, separators=(",", ":")).replace("</", "<\\/").replace("<!--", "<\\!--")
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        f.write(TEMPLATE.replace("/*BUNDLE*/", payload))
    print(f"Docs site: {len(docs)} docs, {len(graph['nodes'])} graph nodes -> {rel(OUT)} ({os.path.getsize(OUT) // 1024} KB)")


TEMPLATE = r"""<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>Bublock Field Manual</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Federo&family=Atkinson+Hyperlegible:ital,wght@0,400;0,700;1,400&family=JetBrains+Mono:wght@400;600&display=swap">
<style>
:root {
  --ground: #eceef2; --surface: #ffffff; --sunk: #e3e6ec; --ink: #151922; --muted: #596172;
  --rule: #d3d8e0; --amber: #a35e0c; --sapphire: #2957a4; --ok: #2c7a4b; --bad: #b3261e;
  --code-bg: #e7eaf0; --focus: #2957a4; --shadow: 0 1px 2px rgba(21,25,34,.06), 0 4px 16px rgba(21,25,34,.06);
  --n-structure: #a35e0c; --n-code: #2957a4; --n-command: #7a4fb5; --n-game: #2c7a4b; --n-docs: #6b7384; --n-ops: #b0457a; --n-plan: #0f7c86;
  --display: "Federo", "Josefin Sans", "Futura", "Century Gothic", sans-serif;
  --body: "Atkinson Hyperlegible", "Segoe UI", "Helvetica Neue", Arial, sans-serif;
  --mono: "JetBrains Mono", ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace;
  --side: 272px;
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    color-scheme: dark;
    --ground: #0e1014; --surface: #161920; --sunk: #1d2129; --ink: #e3e6ec; --muted: #98a0af;
    --rule: #282d37; --amber: #e2a24e; --sapphire: #7fa9ee; --ok: #6cc592; --bad: #f2837b;
    --code-bg: #1d2129; --focus: #7fa9ee; --shadow: 0 1px 2px rgba(0,0,0,.4), 0 4px 16px rgba(0,0,0,.3);
    --n-structure: #e2a24e; --n-code: #7fa9ee; --n-command: #b894ee; --n-game: #6cc592; --n-docs: #98a0af; --n-ops: #ea8cbb; --n-plan: #4fc3cf;
  }
}
:root[data-theme="dark"] {
  color-scheme: dark;
  --ground: #0e1014; --surface: #161920; --sunk: #1d2129; --ink: #e3e6ec; --muted: #98a0af;
  --rule: #282d37; --amber: #e2a24e; --sapphire: #7fa9ee; --ok: #6cc592; --bad: #f2837b;
  --code-bg: #1d2129; --focus: #7fa9ee; --shadow: 0 1px 2px rgba(0,0,0,.4), 0 4px 16px rgba(0,0,0,.3);
  --n-structure: #e2a24e; --n-code: #7fa9ee; --n-command: #b894ee; --n-game: #6cc592; --n-docs: #98a0af; --n-ops: #ea8cbb; --n-plan: #4fc3cf;
}
* { box-sizing: border-box; }
body { margin: 0; background: var(--ground); color: var(--ink); font: 16px/1.6 var(--body); }
a { color: var(--sapphire); text-underline-offset: 2px; }
:focus-visible { outline: 2px solid var(--focus); outline-offset: 2px; }
.shell { display: grid; grid-template-columns: var(--side) minmax(0, 1fr); min-height: 100vh; }
.side { position: sticky; top: env(safe-area-inset-top, 0px); height: 100vh; overflow-y: auto; background: var(--surface);
  border-right: 1px solid var(--rule); padding: 20px 16px 32px; display: flex; flex-direction: column; gap: 18px; }
.brand { display: flex; flex-direction: column; gap: 2px; text-decoration: none; color: var(--ink); }
.brand b { font: 400 25px/1.05 var(--display); letter-spacing: .06em; text-transform: uppercase; }
.brand span { font-size: 12px; color: var(--muted); letter-spacing: .08em; text-transform: uppercase; }
.brand .teams { display: flex; height: 3px; margin-top: 8px; width: 72px; }
.brand .teams i { flex: 1; } .brand .teams i:first-child { background: var(--amber); } .brand .teams i:last-child { background: var(--sapphire); }
.search { width: 100%; font: inherit; font-size: 14px; padding: 8px 10px; border: 1px solid var(--rule); border-radius: 6px;
  background: var(--ground); color: var(--ink); }
.nav { display: flex; flex-direction: column; gap: 16px; }
.nav h2 { margin: 0 0 4px; font: 700 11px/1.4 var(--body); letter-spacing: .12em; text-transform: uppercase; color: var(--muted); }
.nav ul { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; }
.nav a { display: block; padding: 4px 8px; margin: 0 -8px; border-radius: 5px; color: var(--ink); text-decoration: none; font-size: 14.5px; }
.nav a:hover { background: var(--sunk); }
.nav a[aria-current="page"] { background: var(--sunk); color: var(--amber); font-weight: 700; }
.side .foot { margin-top: auto; font-size: 12px; color: var(--muted); }
.topbar { display: none; }
main { min-width: 0; padding-inline: 40px; padding-block: 32px 64px; }
.doc { max-width: 76ch; }
.crumb { font: 12px/1.4 var(--mono); color: var(--muted); margin-bottom: 14px; display: flex; flex-wrap: wrap; gap: 8px 14px; align-items: center; }
.crumb button, .btn { font: 600 12.5px/1 var(--body); padding: 7px 10px; border-radius: 5px; border: 1px solid var(--rule);
  background: var(--surface); color: var(--ink); cursor: pointer; }
.crumb button:hover, .btn:hover { border-color: var(--sapphire); color: var(--sapphire); }
.md h1 { font: 700 34px/1.15 var(--body); letter-spacing: -.01em; margin: 0 0 18px; text-wrap: balance; }
.md h2 { font: 700 23px/1.25 var(--body); margin: 40px 0 12px; padding-top: 14px; border-top: 1px solid var(--rule); text-wrap: balance; }
.md h3 { font: 700 18px/1.3 var(--body); margin: 28px 0 8px; }
.md p, .md ul, .md ol { margin: 0 0 14px; }
.md li { margin: 3px 0; }
.md code { font: 0.86em/1.4 var(--mono); background: var(--code-bg); padding: 1px 5px; border-radius: 4px; overflow-wrap: anywhere; }
.md a code { color: var(--sapphire); }
.md pre { font: 13px/1.5 var(--mono); background: var(--code-bg); padding: 14px 16px; border-radius: 8px; overflow-x: auto; margin: 0 0 18px; }
.md pre code { background: none; padding: 0; font-size: inherit; overflow-wrap: normal; }
.md pre b { color: var(--amber); font-weight: 600; } .md pre i { color: var(--muted); font-style: normal; }
.table-wrap { overflow-x: auto; margin: 0 0 18px; border: 1px solid var(--rule); border-radius: 8px; background: var(--surface); }
.md table { border-collapse: collapse; width: 100%; font-size: 14px; font-variant-numeric: tabular-nums; }
.md th { text-align: left; font: 700 11.5px/1.4 var(--body); letter-spacing: .08em; text-transform: uppercase; color: var(--muted);
  background: var(--sunk); padding: 8px 10px; border-bottom: 1px solid var(--rule); }
.md td { padding: 7px 10px; border-bottom: 1px solid var(--rule); vertical-align: top; }
.md tr:last-child td { border-bottom: 0; }
.md blockquote { margin: 0 0 14px; padding: 2px 14px; border-left: 3px solid var(--amber); color: var(--muted); }
.chip { display: inline-block; font: 700 11px/1 var(--body); letter-spacing: .06em; text-transform: uppercase; padding: 4px 7px;
  border-radius: 999px; white-space: nowrap; }
.chip.ok { background: var(--ok); color: var(--surface); }
.chip.untested { border: 1px dashed var(--muted); color: var(--muted); }
.chip.bad { background: var(--bad); color: var(--surface); }
.glance { display: flex; flex-wrap: wrap; gap: 8px 22px; margin: 0 0 26px; padding: 14px 16px; background: var(--surface);
  border: 1px solid var(--rule); border-radius: 8px; }
.glance div { display: flex; flex-direction: column; }
.glance b { font: 700 22px/1.1 var(--body); font-variant-numeric: tabular-nums; }
.glance span { font-size: 12px; color: var(--muted); letter-spacing: .04em; }
.results { display: flex; flex-direction: column; gap: 4px; max-width: 76ch; }
.results a { display: block; padding: 10px 12px; border-radius: 6px; text-decoration: none; color: var(--ink); background: var(--surface); border: 1px solid var(--rule); }
.results a:hover { border-color: var(--sapphire); }
.results small { display: block; font: 12px/1.4 var(--mono); color: var(--muted); }
.results p { margin: 4px 0 0; font-size: 14px; color: var(--muted); }
.results mark { background: none; color: var(--amber); font-weight: 700; }
/* graph */
.graph-head { display: flex; flex-wrap: wrap; gap: 10px 18px; align-items: flex-end; justify-content: space-between; margin-bottom: 14px; }
.graph-head h1 { font: 700 30px/1.15 var(--body); margin: 0; }
.graph-head p { margin: 4px 0 0; color: var(--muted); max-width: 70ch; }
.filters { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; margin-bottom: 12px; }
.filters label { display: inline-flex; align-items: center; gap: 6px; font-size: 13px; padding: 5px 9px; border: 1px solid var(--rule);
  border-radius: 999px; background: var(--surface); cursor: pointer; user-select: none; }
.filters input { accent-color: var(--sapphire); margin: 0; }
.filters .dot { width: 9px; height: 9px; border-radius: 50%; }
.graph-grid { display: grid; grid-template-columns: minmax(0, 1fr) 320px; gap: 14px; }
.stage { position: relative; height: min(76vh, 860px); min-height: 420px; background: var(--surface); border: 1px solid var(--rule); border-radius: 10px; overflow: hidden; }
.stage svg { width: 100%; height: 100%; display: block; cursor: grab; }
.stage .hint { position: absolute; left: 12px; bottom: 10px; font-size: 12px; color: var(--muted); pointer-events: none; }
.stage .gsearch { position: absolute; top: 10px; left: 10px; width: min(280px, calc(100% - 20px)); font: inherit; font-size: 13.5px;
  padding: 7px 10px; border: 1px solid var(--rule); border-radius: 6px; background: var(--surface); color: var(--ink); box-shadow: var(--shadow); }
.panel { background: var(--surface); border: 1px solid var(--rule); border-radius: 10px; padding: 16px; height: min(76vh, 860px); min-height: 420px; overflow-y: auto; }
.panel h2 { font: 700 19px/1.3 var(--body); margin: 6px 0 4px; overflow-wrap: anywhere; }
.panel .kind { font: 700 11px/1 var(--body); letter-spacing: .1em; text-transform: uppercase; }
.panel .path { font: 12px/1.4 var(--mono); color: var(--muted); overflow-wrap: anywhere; }
.panel p { font-size: 14px; margin: 10px 0; }
.panel h3 { font: 700 11px/1.4 var(--body); letter-spacing: .1em; text-transform: uppercase; color: var(--muted); margin: 16px 0 6px; }
.panel ul { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 2px; }
.panel li button { all: unset; cursor: pointer; font-size: 13.5px; display: flex; gap: 7px; align-items: center; padding: 2px 4px; border-radius: 4px; overflow-wrap: anywhere; }
.panel li button:hover, .panel li button:focus-visible { background: var(--sunk); }
.panel .empty { color: var(--muted); font-size: 14px; }
.legend-dot { width: 8px; height: 8px; border-radius: 50%; flex: none; }
.node-label { font: 11px var(--body); fill: var(--ink); paint-order: stroke; stroke: var(--surface); stroke-width: 3px; pointer-events: none; }
@media (max-width: 1100px) { .graph-grid { grid-template-columns: 1fr; } .panel { height: auto; min-height: 0; } }
@media (max-width: 860px) {
  .shell { grid-template-columns: 1fr; }
  .topbar { display: flex; position: sticky; top: env(safe-area-inset-top, 0px); z-index: 5; align-items: center; justify-content: space-between;
    gap: 12px; padding: 10px 16px; background: var(--surface); border-bottom: 1px solid var(--rule); }
  .topbar b { font: 400 20px/1 var(--display); letter-spacing: .06em; text-transform: uppercase; }
  .side { position: fixed; inset: 0 25% 0 0; z-index: 10; height: 100%; box-shadow: var(--shadow); padding-top: calc(20px + env(safe-area-inset-top, 0px)); }
  .side:not(.open) { display: none; }
  main { padding-inline: 16px; padding-block: 20px 48px; }
  .md h1 { font-size: 28px; }
  .stage { height: 62vh; }
}
@media (prefers-reduced-motion: reduce) { * { scroll-behavior: auto !important; } }
</style>

<div class="topbar"><b>Bublock</b><button class="btn" id="menu" aria-expanded="false" aria-controls="side">Menu</button></div>
<div class="shell">
  <aside class="side" id="side">
    <a class="brand" href="#start"><b>Bublock</b><span>Field manual · Deadlock modding</span><span class="teams"><i></i><i></i></span></a>
    <input class="search" id="search" type="search" placeholder="Search every doc" aria-label="Search every doc">
    <nav class="nav" id="nav" aria-label="Docs"></nav>
    <div class="foot" id="foot"></div>
  </aside>
  <main id="main" tabindex="-1"></main>
</div>

<script type="application/json" id="bundle">/*BUNDLE*/</script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/marked/12.0.2/marked.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/d3/7.9.0/d3.min.js"></script>
<script>
(() => {
  const B = JSON.parse(document.getElementById('bundle').textContent);
  const main = document.getElementById('main');
  const nodesById = new Map(B.graph.nodes.map(n => [n.id, n]));
  const CATEGORY = {
    plugin: 'structure', module: 'structure', feature: 'structure', 'test-project': 'structure',
    source: 'code', test: 'code', command: 'command', script: 'ops', workflow: 'ops', doc: 'docs', knowledge: 'docs',
    stage: 'plan', 'game-mode': 'plan',
  };
  const catOf = k => CATEGORY[k] || 'game';
  const CATS = [
    ['structure', 'Plugins, modules, features'], ['code', 'Source files'], ['command', 'Commands'],
    ['game', 'Game dependencies'], ['plan', 'Stages & game modes'], ['docs', 'Docs'], ['ops', 'Scripts & CI'],
  ];
  const cssVar = name => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  const color = cat => cssVar('--n-' + cat);
  const slug = p => 'doc-' + p.replace(/\//g, '~');
  const unslug = s => s.slice(4).replace(/~/g, '/');
  const docForNode = n => n && (n.doc && B.docs[n.doc] ? n.doc : (n.path && B.docs[n.path] ? n.path : null));
  const nodeForDoc = new Map();
  B.graph.nodes.forEach(n => { if (n.doc && !nodeForDoc.has(n.doc)) nodeForDoc.set(n.doc, n.id); if (n.kind === 'doc' || n.kind === 'knowledge') nodeForDoc.set(n.path, n.id); });
  const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
  const tryStore = (k, v) => { try { localStorage.setItem(k, v); } catch (e) {} };
  const readStore = k => { try { return localStorage.getItem(k); } catch (e) { return null; } };

  // ------------------------------------------------------------------ nav
  const nav = document.getElementById('nav');
  nav.innerHTML = B.nav.map(g => `<section><h2>${esc(g.group)}</h2><ul>${g.items.map(i =>
    `<li><a href="#${i.path.startsWith('#') ? i.path.slice(1) : slug(i.path)}">${esc(i.label)}</a></li>`).join('')}</ul></section>`).join('');
  const c = B.meta.counts;
  document.getElementById('foot').textContent = `${B.meta.docs} docs · ${B.graph.nodes.length} graph nodes · built ${B.meta.built}`;
  const side = document.getElementById('side'), menu = document.getElementById('menu');
  menu.addEventListener('click', () => { const open = side.classList.toggle('open'); menu.setAttribute('aria-expanded', open); });
  const markNav = () => {
    nav.querySelectorAll('a').forEach(a => a.getAttribute('href') === location.hash ? a.setAttribute('aria-current', 'page') : a.removeAttribute('aria-current'));
  };

  // ------------------------------------------------------------- markdown
  marked.setOptions({ gfm: true, breaks: false });
  const dirOf = p => p.includes('/') ? p.slice(0, p.lastIndexOf('/')) : '';
  const normalize = p => { const out = []; p.split('/').forEach(s => { if (s === '..') out.pop(); else if (s && s !== '.') out.push(s); }); return out.join('/'); };
  const resolveDoc = (from, target) => {
    const bases = [dirOf(from), '', 'RiftRoulette'];
    for (const base of bases) {
      const p = normalize((base ? base + '/' : '') + target.replace(/\\/g, '/'));
      for (const cand of [p, p.replace(/\.cs$/, '.md'), p.replace(/\/$/, '') + '/FEATURE.md', p + '.md']) if (B.docs[cand]) return cand;
    }
    return null;
  };
  function renderDoc(path) {
    const doc = B.docs[path];
    const body = doc.text.replace(/^---\n[\s\S]*?\n---\n/, '');
    const html = marked.parse(body);
    const nodeId = nodeForDoc.get(path);
    main.innerHTML = `<article class="doc">
      <div class="crumb"><span>${esc(path)}</span>${nodeId ? '<button type="button" id="show-in-graph">Show in graph</button>' : ''}</div>
      ${path === 'knowledge/README.md' ? glance() : ''}
      <div class="md">${html}</div></article>`;
    const md = main.querySelector('.md');
    md.querySelectorAll('table').forEach(t => { const w = document.createElement('div'); w.className = 'table-wrap'; t.replaceWith(w); w.appendChild(t); });
    md.querySelectorAll('a[href]').forEach(a => {
      const href = a.getAttribute('href');
      if (/^(https?:|mailto:)/.test(href)) { a.target = '_blank'; a.rel = 'noopener'; return; }
      if (href.startsWith('#')) { a.removeAttribute('href'); return; }
      const hit = resolveDoc(path, href.split('#')[0]);
      if (hit) a.setAttribute('href', '#' + slug(hit)); else a.removeAttribute('href');
    });
    md.querySelectorAll('code').forEach(code => {
      if (code.closest('a, pre')) return;
      const t = code.textContent.trim();
      if (!/^[\w.-]+(\/[\w.-]+)+\/?$|^[\w-]+\.(md|cs)$/.test(t)) return;
      const hit = resolveDoc(path, t);
      if (hit && hit !== path) { const a = document.createElement('a'); a.href = '#' + slug(hit); code.replaceWith(a); a.appendChild(code); }
    });
    md.querySelectorAll('td').forEach(td => {
      const t = td.textContent.trim().toLowerCase();
      if (t === 'avoid' && td.querySelector('strong')) td.innerHTML = '<span class="chip bad">avoid</span>';
      else if (/^verified\b/.test(t)) td.innerHTML = td.innerHTML.replace(/^verified/, '<span class="chip ok">verified</span>');
      else if (/^untested\b/.test(t)) td.innerHTML = td.innerHTML.replace(/^untested/, '<span class="chip untested">untested</span>');
    });
    const btn = document.getElementById('show-in-graph');
    if (btn) btn.addEventListener('click', () => { pendingSelect = nodeId; location.hash = 'graph'; });
  }
  function glance() {
    const g = k => c[k] || 0;
    const deps = ['convar', 'entity', 'event', 'hook', 'modifier', 'modifier-state', 'schema-field', 'ability', 'net-message'].reduce((s, k) => s + g(k), 0);
    const items = [[g('plugin'), 'plugin DLLs'], [g('module'), 'modules'], [g('feature'), 'features'], [g('source'), 'source files'],
      [g('command'), 'commands'], [deps, 'game dependencies'], [g('test'), 'test files']];
    return `<div class="glance" aria-label="Repo at a glance">${items.map(([n, l]) => `<div><b>${n}</b><span>${l}</span></div>`).join('')}</div>`;
  }

  // --------------------------------------------------------------- search
  const search = document.getElementById('search');
  search.addEventListener('input', () => {
    const q = search.value.trim().toLowerCase();
    if (q.length < 2) { route(); return; }
    const hits = [];
    for (const [path, d] of Object.entries(B.docs)) {
      const text = d.text.toLowerCase(), title = d.title.toLowerCase();
      const i = text.indexOf(q);
      if (i < 0 && !title.includes(q) && !path.toLowerCase().includes(q)) continue;
      const score = (title.includes(q) ? 0 : 1) + (path.startsWith('knowledge/') ? 0 : 0.5) + (path.endsWith('FEATURE.md') ? 0 : 0.2);
      const start = Math.max(0, i - 60), snip = i >= 0 ? d.text.slice(start, i + q.length + 80).replace(/\s+/g, ' ') : '';
      hits.push({ path, d, score, snip });
    }
    hits.sort((a, b) => a.score - b.score || a.path.localeCompare(b.path));
    const mark = s => esc(s).replace(new RegExp(q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'ig'), m => `<mark>${m}</mark>`);
    main.innerHTML = `<div class="doc"><div class="crumb"><span>${hits.length} docs match “${esc(search.value.trim())}”</span></div>
      <div class="results">${hits.slice(0, 60).map(h => `<a href="#${slug(h.path)}"><b>${esc(h.d.title)}</b><small>${esc(h.path)}</small>${h.snip ? `<p>…${mark(h.snip)}…</p>` : ''}</a>`).join('')}</div></div>`;
  });

  // ---------------------------------------------------------------- graph
  let pendingSelect = null;
  const hidden = new Set((readStore('bublock.hiddenCats') || 'docs').split(',').filter(Boolean));
  let showRefs = readStore('bublock.showRefs') === '1';
  function renderGraph() {
    main.innerHTML = `<section>
      <div class="graph-head"><div><h1>Knowledge graph</h1>
        <p>Plugins, modules and features contain files; files register commands and touch game dependencies. Click a node to see its links, drag to pan, scroll to zoom.</p></div></div>
      <div class="filters" id="filters">${CATS.map(([k, l]) => `<label><input type="checkbox" data-cat="${k}" ${hidden.has(k) ? '' : 'checked'}><span class="dot" style="background:${color(k)}"></span>${l}</label>`).join('')}
        <label><input type="checkbox" id="refs" ${showRefs ? 'checked' : ''}>Code references between files</label></div>
      <div class="graph-grid"><div class="stage" id="stage"><input class="gsearch" id="gsearch" type="search" placeholder="Find a node (Enter to select)" aria-label="Find a node"><div class="hint">Showing <span id="count"></span></div></div>
      <aside class="panel" id="panel" aria-live="polite"></aside></div></section>`;
    document.querySelectorAll('#filters input[data-cat]').forEach(i => i.addEventListener('change', () => {
      i.checked ? hidden.delete(i.dataset.cat) : hidden.add(i.dataset.cat); tryStore('bublock.hiddenCats', [...hidden].join(',')); draw();
    }));
    document.getElementById('refs').addEventListener('change', e => { showRefs = e.target.checked; tryStore('bublock.showRefs', showRefs ? '1' : '0'); draw(); });
    const stage = document.getElementById('stage');
    const svg = d3.select(stage).insert('svg', ':first-child').attr('role', 'img').attr('aria-label', 'Knowledge graph');
    const root = svg.append('g');
    const zoom = d3.zoom().scaleExtent([0.15, 5]).on('zoom', e => { root.attr('transform', e.transform); labelsFor(e.transform.k); });
    svg.call(zoom);
    let sim, nodeSel, linkSel, labelSel, selected = pendingSelect, visible = [];
    pendingSelect = null;
    const radius = n => ({ plugin: 12, module: 10, feature: 8, 'test-project': 8, command: 4.5, source: 5, test: 3.5, script: 6, workflow: 7, knowledge: 6, doc: 5, stage: 6, 'game-mode': 8 }[n.kind] || 5.5);
    function labelsFor(k) {
      if (!labelSel) return;
      labelSel.attr('display', d => (d.id === selected || ['plugin', 'module', 'feature', 'workflow', 'game-mode'].includes(d.kind) || k > 1.7 || (catOf(d.kind) === 'game' && k > 1.1)) ? null : 'none');
    }
    function draw() {
      if (sim) sim.stop();
      const vis = new Set(B.graph.nodes.filter(n => !hidden.has(catOf(n.kind))).map(n => n.id));
      if (selected && nodesById.has(selected)) vis.add(selected);
      const links = B.graph.links.filter(l => vis.has(l.source.id || l.source) && vis.has(l.target.id || l.target) && (showRefs || !['uses', 'tests'].includes(l.kind)))
        .map(l => ({ source: l.source.id || l.source, target: l.target.id || l.target, kind: l.kind }));
      const prev = new Map(visible.map(n => [n.id, n]));
      visible = B.graph.nodes.filter(n => vis.has(n.id)).map(n => Object.assign(prev.get(n.id) || {}, n));
      document.getElementById('count').textContent = `${visible.length} nodes, ${links.length} links`;
      root.selectAll('*').remove();
      linkSel = root.append('g').attr('stroke-opacity', .5).selectAll('line').data(links).join('line')
        .attr('stroke', d => d.kind === 'contains' || d.kind === 'imports' || d.kind === 'feature-uses' ? cssVar('--n-structure') : cssVar('--rule'))
        .attr('stroke-width', d => d.kind === 'imports' ? 1.6 : 1).attr('stroke-dasharray', d => d.kind === 'imports' ? '4 3' : null);
      nodeSel = root.append('g').selectAll('circle').data(visible, d => d.id).join('circle')
        .attr('r', radius).attr('fill', d => color(catOf(d.kind))).attr('stroke', cssVar('--surface')).attr('stroke-width', 1.5)
        .style('cursor', 'pointer').on('click', (e, d) => { e.stopPropagation(); select(d.id); })
        .call(d3.drag().on('start', (e, d) => { if (!e.active) sim.alphaTarget(.2).restart(); d.fx = d.x; d.fy = d.y; })
          .on('drag', (e, d) => { d.fx = e.x; d.fy = e.y; }).on('end', (e, d) => { if (!e.active) sim.alphaTarget(0); d.fx = null; d.fy = null; }));
      nodeSel.append('title').text(d => `${d.label} (${d.kind})`);
      labelSel = root.append('g').selectAll('text').data(visible, d => d.id).join('text').attr('class', 'node-label')
        .attr('dx', d => radius(d) + 3).attr('dy', 4).text(d => d.label);
      const box = stage.getBoundingClientRect();
      sim = d3.forceSimulation(visible)
        .force('link', d3.forceLink(links).id(d => d.id).distance(l => l.kind === 'contains' ? 34 : l.kind === 'imports' ? 90 : 70).strength(l => l.kind === 'contains' ? .9 : .25))
        .force('charge', d3.forceManyBody().strength(d => -40 - radius(d) * 8))
        .force('collide', d3.forceCollide(d => radius(d) + 2))
        .force('x', d3.forceX(box.width / 2).strength(.03)).force('y', d3.forceY(box.height / 2).strength(.05))
        .on('tick', () => {
          linkSel.attr('x1', d => d.source.x).attr('y1', d => d.source.y).attr('x2', d => d.target.x).attr('y2', d => d.target.y);
          nodeSel.attr('cx', d => d.x).attr('cy', d => d.y); labelSel.attr('x', d => d.x).attr('y', d => d.y);
        });
      if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) { sim.stop(); for (let i = 0; i < 300; i++) sim.tick(); sim.on('tick')(); }
      labelsFor(d3.zoomTransform(svg.node()).k);
      highlight();
      panel();
    }
    function neighbors(id) {
      const out = [];
      B.graph.links.forEach(l => { if (l.source === id) out.push([l.kind, l.target, 'out']); else if (l.target === id) out.push([l.kind, l.source, 'in']); });
      return out;
    }
    function highlight() {
      if (!selected) { nodeSel.attr('opacity', 1); linkSel.attr('stroke-opacity', .5); labelsFor(d3.zoomTransform(svg.node()).k); return; }
      const near = new Set([selected, ...neighbors(selected).map(x => x[1])]);
      nodeSel.attr('opacity', d => near.has(d.id) ? 1 : .15).attr('stroke', d => d.id === selected ? cssVar('--ink') : cssVar('--surface'));
      linkSel.attr('stroke-opacity', d => d.source.id === selected || d.target.id === selected ? .9 : .06);
      labelSel.attr('display', d => near.has(d.id) ? null : 'none');
    }
    const VERBS = { contains: ['Contains', 'Inside'], imports: ['Compiles in', 'Compiled into'], uses: ['Uses', 'Used by'], tests: ['Tests', 'Tested by'],
      registers: ['Registers', 'Registered by'], 'depends-on': ['Depends on', 'Used by'], hooks: ['Hooks', 'Hooked by'], listens: ['Listens to', 'Heard by'],
      sends: ['Sends', 'Sent by'], links: ['Links to', 'Linked from'], documents: ['Documented by', 'Documents'], 'refers-to': ['Refers to', 'Referred to by'],
      'feature-uses': ['Depends on', 'Needed by'], touches: ['Touches', 'Changed in stage'], 'mode-uses': ['Uses', 'Used by mode'], 'planned-in': ['Planned in', 'Plans'] };
    function panel() {
      const el = document.getElementById('panel');
      const n = nodesById.get(selected);
      if (!n) {
        el.innerHTML = `<p class="kind" style="color:var(--muted)">Legend</p><ul>${CATS.map(([k, l]) => `<li><button type="button" disabled><span class="legend-dot" style="background:${color(k)}"></span>${l}</button></li>`).join('')}</ul>
          <h3>Start here</h3><ul>${['mode:gun-game', 'stage:13k', 'plugin:RiftRoulette', 'feature:RiftRoulette/GunGame', 'module:Modules/Restraint', 'event:player_death', 'workflow:.github/workflows/deploy.yml']
            .filter(id => nodesById.has(id)).map(id => `<li><button type="button" data-id="${esc(id)}"><span class="legend-dot" style="background:${color(catOf(nodesById.get(id).kind))}"></span>${esc(nodesById.get(id).label)}</button></li>`).join('')}</ul>`;
      } else {
        const groups = new Map();
        neighbors(n.id).forEach(([kind, other, dir]) => {
          const label = (VERBS[kind] || [kind, kind])[dir === 'out' ? 0 : 1];
          if (!groups.has(label)) groups.set(label, []);
          groups.get(label).push(other);
        });
        const doc = docForNode(n);
        el.innerHTML = `<span class="kind" style="color:${color(catOf(n.kind))}">${esc(n.kind)}${n.audience ? ' · ' + esc(n.audience) : ''}${n.status ? ' · ' + esc(n.status) : ''}</span>
          <h2>${esc(n.label)}</h2>${n.path ? `<div class="path">${esc(n.path)}</div>` : ''}
          ${n.description ? `<p>${esc(n.description)}</p>` : ''}${n.summary ? `<p>${esc(n.summary)}</p>` : ''}
          ${doc ? `<button class="btn" type="button" id="open-doc">Open doc</button>` : ''}
          ${[...groups].map(([label, ids]) => `<h3>${esc(label)} (${ids.length})</h3><ul>${ids.sort((a, b) => nodesById.get(a).label.localeCompare(nodesById.get(b).label)).slice(0, 80).map(id => {
            const o = nodesById.get(id); return `<li><button type="button" data-id="${esc(id)}"><span class="legend-dot" style="background:${color(catOf(o.kind))}"></span>${esc(o.label)}</button></li>`; }).join('')}</ul>`).join('')}`;
        const od = document.getElementById('open-doc');
        if (od) od.addEventListener('click', () => { location.hash = slug(doc); });
      }
      el.querySelectorAll('button[data-id]').forEach(b => b.addEventListener('click', () => select(b.dataset.id)));
    }
    function select(id) {
      const n = nodesById.get(id);
      if (n && hidden.has(catOf(n.kind))) { selected = id; draw(); } else { selected = id; highlight(); panel(); }
      const d = visible.find(v => v.id === id);
      if (d && d.x != null) {
        const box = stage.getBoundingClientRect(), k = Math.max(1.2, d3.zoomTransform(svg.node()).k);
        svg.transition().duration(400).call(zoom.transform, d3.zoomIdentity.translate(box.width / 2 - d.x * k, box.height / 2 - d.y * k).scale(k));
      }
    }
    svg.on('click', () => { selected = null; highlight(); panel(); });
    const gs = document.getElementById('gsearch');
    gs.addEventListener('keydown', e => {
      if (e.key !== 'Enter') return;
      const q = gs.value.trim().toLowerCase(); if (!q) return;
      const hit = B.graph.nodes.find(n => n.label.toLowerCase() === q || n.label.toLowerCase() === '/' + q) || B.graph.nodes.find(n => n.label.toLowerCase().includes(q)) || B.graph.nodes.find(n => (n.path || '').toLowerCase().includes(q));
      if (hit) { hidden.delete(catOf(hit.kind)); document.querySelector(`#filters input[data-cat="${catOf(hit.kind)}"]`).checked = true; selected = hit.id; draw(); setTimeout(() => select(hit.id), 700); }
    });
    draw();
    if (selected) setTimeout(() => select(selected), 900);
  }

  // ---------------------------------------------------------------- route
  function route() {
    const h = decodeURIComponent(location.hash.slice(1)) || 'start';
    side.classList.remove('open'); menu.setAttribute('aria-expanded', 'false');
    if (h === 'graph') renderGraph();
    else if (h.startsWith('doc-') && B.docs[unslug(h)]) renderDoc(unslug(h));
    else renderDoc('knowledge/README.md');
    markNav();
    if (h !== 'graph') window.scrollTo(0, 0);
  }
  window.addEventListener('hashchange', () => { search.value = ''; route(); });
  route();
})();
</script>
"""

if __name__ == "__main__":
    sys.exit(main())

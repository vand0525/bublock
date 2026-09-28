#!/usr/bin/env python3
"""Build the Bublock knowledge graph from the repo: graph.json, tree.md, indexes.md.

Offline and read-only on the source; writes only knowledge/generated/. The graph
links plugins, modules, features, source files, commands, game dependencies and
docs. See knowledge-graph.py.md.
"""

import json
import os
import re
import sys
from collections import defaultdict
from datetime import datetime, timezone

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
OUT = os.path.join(ROOT, "knowledge", "generated")
SKIP_DIRS = {"bin", "obj", ".git", "lib", "logs", "site", ".github", "baseline", "maps", ".cursor", "templates"}
CVARLIST = os.path.join(ROOT, "RiftRoulette", "reference", "cvarlist.md")
ENTITY_DUMP = os.path.join(ROOT, "RiftRoulette", "reference", "maps", "dl_midtown", "entities.json")
ENTITY_PREFIXES = ("npc_", "citadel_item_", "citadel_koth_", "citadel_gamerules", "citadel_shop_",
                   "citadel_herotest_", "info_", "point_", "trigger_", "item_crate")
RUNTIME_ENTITIES = {"observer"}  # designer names that never appear in the map dump
BARE_CONVARS = {"maxplayers"}  # convars without an underscore (most bare words are commands)

nodes = {}
links = []
DECLARED = {}  # C# type name -> file node that declares it
MASTER_PLAN = os.path.join(ROOT, "RiftRoulette", "reference", "master-plan.md")
RECIPES = os.path.join(ROOT, "knowledge", "game-mode-recipes.md")


def rel(path):
    return os.path.relpath(path, ROOT).replace(os.sep, "/")


def add_node(node_id, kind, label, **attrs):
    node = nodes.setdefault(node_id, {"id": node_id, "kind": kind, "label": label})
    node.update({k: v for k, v in attrs.items() if v not in (None, "", [])})
    return node


def add_link(source, target, kind):
    if source != target and source in nodes and target in nodes:
        links.append({"source": source, "target": target, "kind": kind})


def read(path):
    with open(path, encoding="utf-8", errors="replace") as f:
        return f.read()


def walk(exts):
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = sorted(d for d in dirnames if d not in SKIP_DIRS and not d.startswith("."))
        for name in sorted(filenames):
            if name.endswith(exts):
                yield os.path.join(dirpath, name)


def doc_summary(md_path):
    """First sentence of the first prose paragraph of a markdown doc."""
    if not os.path.exists(md_path):
        return ""
    text = re.sub(r"^---\n.*?\n---\n", "", read(md_path), flags=re.S)
    in_code = False
    para = []
    for line in text.splitlines():
        if line.startswith("```"):
            in_code = not in_code
            continue
        if in_code or line.startswith(("#", "|", "- ", "* ", ">")) or re.match(r"^\d+\. ", line):
            if para:
                break
            continue
        if not line.strip():
            if para:
                break
            continue
        para.append(line.strip())
    sentence = re.split(r"(?<!e\.g\.)(?<!i\.e\.)(?<!etc\.)(?<=[.!?])\s(?=[A-Z`(])", " ".join(para), maxsplit=1)[0]
    return sentence[:220]


def doc_title(md_path):
    for line in read(md_path).splitlines():
        if line.startswith("# "):
            return line[2:].strip()
    return os.path.basename(md_path)


def strip_code(text):
    """Remove comments and string literals so identifier scans see only code."""
    text = re.sub(r"//[^\n]*", "", text)
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    return re.sub(r'@?\$?"(?:[^"\\\n]|\\.)*"', '""', text)


def load_convars():
    names = set()
    if os.path.exists(CVARLIST):
        for line in read(CVARLIST).splitlines():
            m = re.match(r"^([A-Za-z0-9_+.-]+) \| ", line)
            if m:
                names.add(m.group(1))
    return names


def load_map_entities():
    if not os.path.exists(ENTITY_DUMP):
        return set()
    with open(ENTITY_DUMP, encoding="utf-8") as f:
        return {e.get("classname") for e in json.load(f)["entities"]}


# ---------------------------------------------------------------- containers

def owner_of(path):
    """Nearest folder node (feature, module, plugin, test project) that contains path."""
    d = os.path.dirname(path)
    while d.startswith(ROOT) and d != ROOT:
        for prefix in ("feature:", "module:", "plugin:", "tests:"):
            if prefix + rel(d) in nodes:
                return prefix + rel(d)
        d = os.path.dirname(d)
    return None


def build_containers():
    for proj in walk((".csproj",)):
        folder = os.path.dirname(proj)
        name = os.path.splitext(os.path.basename(proj))[0]
        is_test = rel(proj).startswith("Tests/")
        feature_md = os.path.join(folder, "FEATURE.md")
        add_node(("tests:" if is_test else "plugin:") + rel(folder), "test-project" if is_test else "plugin",
                 name + ("" if is_test else ".dll"), path=rel(folder),
                 doc=rel(feature_md) if os.path.exists(feature_md) else None,
                 summary=doc_summary(feature_md))
    for items in walk((".projitems",)):
        folder = os.path.dirname(items)
        node_id = "module:" + rel(folder)
        feature_md = os.path.join(folder, "FEATURE.md")
        node = add_node(node_id, "module", os.path.basename(folder), path=rel(folder),
                        doc=rel(feature_md) if os.path.exists(feature_md) else None,
                        summary=doc_summary(feature_md))
        node.setdefault("projitems", []).append(os.path.basename(items))
    for feature_md in walk(("FEATURE.md",)):
        folder = os.path.dirname(feature_md)
        if any(p + rel(folder) in nodes for p in ("plugin:", "module:", "tests:")) or folder == ROOT:
            continue
        add_node("feature:" + rel(folder), "feature", os.path.basename(folder), path=rel(folder),
                 doc=rel(feature_md), summary=doc_summary(feature_md))
    for node_id in list(nodes):
        # Search from the folder's parent, so a feature finds its plugin rather than itself.
        parent = owner_of(os.path.join(ROOT, nodes[node_id]["path"]))
        if parent and parent != node_id:
            add_link(parent, node_id, "contains")
    for proj in walk((".csproj",)):
        folder = os.path.dirname(proj)
        src = ("tests:" if rel(proj).startswith("Tests/") else "plugin:") + rel(folder)
        for imp in re.findall(r'<Import Project="([^"]+\.projitems)"', read(proj)):
            target = os.path.normpath(os.path.join(folder, imp.replace("\\", "/")))
            add_link(src, "module:" + rel(os.path.dirname(target)), "imports")


# ------------------------------------------------------------------ sources

def build_sources(convars, map_entities):
    declared = DECLARED
    sources = {}
    for cs in walk((".cs",)):
        if rel(cs).startswith("scripts/"):
            continue
        text = read(cs)
        node_id = "file:" + rel(cs)
        is_test = rel(cs).startswith("Tests/")
        md = cs[:-3] + ".md"
        types = re.findall(r"\b(?:class|record|struct|enum|interface)\s+([A-Z]\w*)", strip_code(text))
        add_node(node_id, "test" if is_test else "source", os.path.basename(cs)[:-3], path=rel(cs),
                 doc=rel(md) if os.path.exists(md) else None, summary=doc_summary(md), types=sorted(set(types)))
        owner = owner_of(cs)
        if owner:
            add_link(owner, node_id, "contains")
        for t in types:
            declared.setdefault(t, node_id)
        sources[node_id] = text

    for node_id, text in sources.items():
        code = strip_code(text)
        idents = set(re.findall(r"\b[A-Z]\w+\b", code))
        for ident in idents:
            target = declared.get(ident)
            if target and target != node_id:
                add_link(node_id, target, "tests" if nodes[node_id]["kind"] == "test" and nodes[target]["label"] + "Tests" == nodes[node_id]["label"] else "uses")
        if nodes[node_id]["kind"] == "test":
            continue

        # commands
        for m in re.finditer(r'^\s*\[Command\("([a-z0-9_]+)"(.*)\)\]\s*$', text, flags=re.M):
            name, rest = m.group(1), m.group(2)
            desc = re.search(r'Description\s*=\s*"((?:[^"\\]|\\.)*)"', rest)
            body = text[m.end():]
            nxt = body.find("[Command(")
            body = body[: nxt if nxt >= 0 else 3000][:3000]
            admin = "AdminCommand.Authorize" in body or "Authorize(" in body
            flags = [f for f in ("ConsoleOnly", "ChatOnly", "ServerOnly", "Hidden", "SuppressChat") if f in rest]
            add_node("command:" + name, "command", "/" + name, audience="admin" if admin else "player",
                     description=desc.group(1) if desc else "", flags=flags, file=rel_path(node_id))
            add_link(node_id, "command:" + name, "registers")

        # hooks and events
        for hook in set(re.findall(r"override\s+[\w<>?]+\s+(On[A-Z]\w+)\(", text)):
            add_node("hook:" + hook, "hook", hook)
            add_link(node_id, "hook:" + hook, "hooks")
        for event in set(re.findall(r'\[GameEventHandler\("([a-z_]+)"\)\]', text)):
            add_node("event:" + event, "event", event)
            add_link(node_id, "event:" + event, "listens")

        # string-literal game dependencies (our own command names share words with console commands)
        own_commands = set(re.findall(r'\[Command\("([a-z0-9_]+)"', text))
        for lit in set(re.findall(r'"([A-Za-z_][A-Za-z0-9_]{2,})"', text)) - own_commands:
            if lit in convars and ("_" in lit or lit in BARE_CONVARS):
                kind = "convar"
            elif lit.startswith("modifier_"):
                kind = "modifier"
            elif lit.startswith("citadel_ability_"):
                kind = "ability"
            elif re.match(r"^m_[a-zA-Z]", lit):
                kind = "schema-field"
            elif lit in map_entities or lit in RUNTIME_ENTITIES or lit.startswith(ENTITY_PREFIXES):
                kind = "entity"
            else:
                continue
            add_node(f"{kind}:{lit}", kind, lit)
            add_link(node_id, f"{kind}:{lit}", "depends-on")

        # enum and message dependencies
        for state in set(re.findall(r"EModifierState\.([A-Za-z]+)", text)):
            add_node("modifier-state:" + state, "modifier-state", "EModifierState." + state)
            add_link(node_id, "modifier-state:" + state, "depends-on")
        for msg in set(re.findall(r"\b(CCitadelUserMsg_\w+|CUserMessage\w+)", text)):
            add_node("net-message:" + msg, "net-message", msg)
            add_link(node_id, "net-message:" + msg, "sends")


def rel_path(node_id):
    return node_id.split(":", 1)[1]


# ------------------------------------------- feature deps, stages, game modes

def slug(text):
    return re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")


def build_feature_deps():
    """Roll file-to-file type references up to the folders that hold them (feature, module)."""
    owner = {l["target"]: l["source"] for l in links
             if l["kind"] == "contains" and nodes[l["target"]]["kind"] == "source"}
    weights = defaultdict(int)
    for l in links:
        if l["kind"] == "uses" and nodes[l["source"]]["kind"] == "source" and nodes[l["target"]]["kind"] == "source":
            a, b = owner.get(l["source"]), owner.get(l["target"])
            if a and b and a != b:
                weights[(a, b)] += 1
    for (a, b), weight in sorted(weights.items()):
        before = len(links)
        add_link(a, b, "feature-uses")
        if len(links) > before:
            links[-1]["weight"] = weight


def resolve_ref(token):
    """Map a backticked name from a doc to a graph node: path, folder, type, command, hook or game dependency."""
    t = token.strip().strip("`").split("(")[0].strip().rstrip(".,;:")
    if not t or " " in t.strip():
        t = t.split(" ")[0] if t.startswith("/") else t
    if not t:
        return None
    if t.startswith("/") and "command:" + t[1:].split(" ")[0] in nodes:
        return "command:" + t[1:].split(" ")[0]
    bare = t.rstrip("/")
    for cand in (bare, "RiftRoulette/" + bare, bare + ".cs", "RiftRoulette/" + bare + ".cs"):
        for prefix in ("file:", "script:", "workflow:", "feature:", "module:", "plugin:", "doc:"):
            if prefix + cand in nodes:
                return prefix + cand
    if t.startswith("EModifierState.") and "modifier-state:" + t.split(".")[1] in nodes:
        return "modifier-state:" + t.split(".")[1]
    for kind in ("event", "hook", "convar", "entity", "modifier", "schema-field", "ability", "net-message"):
        if f"{kind}:{t}" in nodes:
            return f"{kind}:{t}"
    head = bare.split("/")[-1].split(".")[0]
    if head in DECLARED:
        return DECLARED[head]
    if "hook:" + head in nodes:
        return "hook:" + head
    return None


def refs_in(text):
    found = []
    for token in re.findall(r"`([^`\n]+)`", text):
        target = resolve_ref(token)
        if target and target not in found:
            found.append(target)
    return found


def mark_roles():
    """A plugin that admits joining players (OnClientFullConnect) is a game type; the rest are tools."""
    kids = defaultdict(list)
    for l in links:
        if l["kind"] == "contains":
            kids[l["source"]].append(l["target"])
    for node in nodes.values():
        if node["kind"] != "plugin":
            continue
        stack, files = [node["id"]], []
        while stack:
            for kid in kids[stack.pop()]:
                if nodes[kid]["kind"] == "source":
                    files.append(kid)
                else:
                    stack.append(kid)
        admits = any(l["source"] in files and l["target"] == "hook:OnClientFullConnect" for l in links)
        node["role"] = "game type" if admits else "tool"


def build_stages():
    """Master-plan stages (Theo's roadmap) with status, linked to the code each one names."""
    if not os.path.exists(MASTER_PLAN):
        return
    text = read(MASTER_PLAN)
    # Theo's roadmap, then the fork's game-type stages (Stage G1, ...).
    parts = [text.split(marker, 1)[-1].split("\n## ", 1)[0] for marker in ("## Staged roadmap", "## Game types") if marker in text]
    sections = []
    for part in parts:
        split = re.split(r"^(#{3,4} .+)$", part, flags=re.M)
        sections += list(zip(split[1::2], split[2::2]))
    order = 0
    for heading, body in sections:
        title = heading.lstrip("#").strip()
        match = re.match(r"Stage ([A-Z]?\d+[a-z]?)\b", title)
        node_id = "stage:" + (match.group(1).lower() if match else slug(title))
        box = re.search(r"^- \[(x| )\] (.+)$", body, flags=re.M)
        note = box.group(2) if box else ""
        status = ("done" if box and box.group(1) == "x"
                  else "open" if not box or "not uploaded" in note or "uploaded" not in note
                  else "awaiting playtest")
        goal = re.search(r"^- \*\*Goal:\*\* (.+)$", body, flags=re.M)
        order += 1
        add_node(node_id, "stage", title.replace(" — ", ": "), path="RiftRoulette/reference/master-plan.md",
                 doc="RiftRoulette/reference/master-plan.md", status=status, order=order,
                 summary=(goal.group(1) if goal else (box.group(2) if box else ""))[:220])
        for target in refs_in(body):
            add_link(node_id, target, "touches")


def build_game_modes():
    """Game-mode recipes (knowledge/game-mode-recipes.md) linked to the levers and code they use."""
    if not os.path.exists(RECIPES):
        return
    status = "designed"
    text = read(RECIPES)
    for block in re.split(r"^(?=## |### )", text, flags=re.M):
        heading = block.splitlines()[0] if block.strip() else ""
        if heading.startswith("## "):
            name = heading[3:].lower()
            status = "shipped" if "shipped" in name else "in progress" if "progress" in name else "designed" if "design" in name else None
            continue
        if not heading.startswith("### ") or status is None:
            continue
        title = heading[4:].strip()
        node_id = "mode:" + slug(title)
        body = block[len(heading):]
        para = next((p.strip().replace("\n", " ") for p in body.split("\n\n") if p.strip() and not p.strip().startswith(("-", "|", "```"))), "")
        add_node(node_id, "game-mode", title, path="knowledge/game-mode-recipes.md",
                 doc="knowledge/game-mode-recipes.md", status=status, summary=para[:220])
        for target in refs_in(body):
            add_link(node_id, target, "mode-uses")
        for stage in set(re.findall(r"Stage ([A-Z]?\d+[a-z]?)\b", body)):
            add_link(node_id, "stage:" + stage.lower(), "planned-in")


# ---------------------------------------------------------- scripts and CI

def build_scripts():
    scripts_dir = os.path.join(ROOT, "scripts")
    workflows_dir = os.path.join(ROOT, ".github", "workflows")
    found = []
    for name in sorted(os.listdir(scripts_dir)):
        if name.endswith((".sh", ".py", ".cs")) and not name.endswith(".md"):
            path = os.path.join(scripts_dir, name)
            md = path + ".md"
            add_node("script:" + rel(path), "script", name, path=rel(path),
                     doc=rel(md) if os.path.exists(md) else None, summary=doc_summary(md))
            found.append(path)
    if os.path.isdir(workflows_dir):
        readme = os.path.join(workflows_dir, "README.md")
        for name in sorted(os.listdir(workflows_dir)):
            if name.endswith((".yml", ".yaml")):
                path = os.path.join(workflows_dir, name)
                first = next((l[2:].strip() for l in read(path).splitlines() if l.startswith("# ")), "")
                add_node("workflow:" + rel(path), "workflow", name, path=rel(path),
                         doc=rel(readme) if os.path.exists(readme) else None, summary=first)
                found.append(path)
    for path in found:
        text = "\n".join(l for l in read(path).splitlines() if not l.lstrip().startswith(("#", "//")))
        src = ("workflow:" if path.endswith((".yml", ".yaml")) else "script:") + rel(path)
        for other in found:
            name = os.path.basename(other)
            if other != path and re.search(r"(?<![\w-])" + re.escape(name) + r"(?![\w.-])", text):
                add_link(src, "script:" + rel(other), "refers-to")


# --------------------------------------------------------------------- docs

def build_docs():
    colocated = {n.get("doc") for n in nodes.values() if n.get("doc")}
    path_to_node = {n["path"]: n["id"] for n in nodes.values() if n.get("path")}
    path_to_node.update({n["doc"]: n["id"] for n in nodes.values() if n.get("doc")})
    docs = []
    for md in walk((".md",)):
        r = rel(md)
        if r in colocated or r.endswith("cvarlist.md"):
            continue
        kind = "knowledge" if r.startswith("knowledge/") else "doc"
        text = read(md)
        fm = re.match(r"^---\n(.*?)\n---\n", text, flags=re.S)
        add_node("doc:" + r, kind, doc_title(md), path=r, doc=r, summary=doc_summary(md),
                 doctype=(re.search(r"^type:\s*(\S+)", fm.group(1), flags=re.M).group(1) if fm and re.search(r"^type:", fm.group(1), flags=re.M) else None))
        path_to_node[r] = "doc:" + r
        docs.append((md, text, fm))
    for md, text, fm in docs:
        src = "doc:" + rel(md)
        projitems = md[:-3] + ".projitems"
        if os.path.exists(projitems):
            add_link("module:" + rel(os.path.dirname(md)), src, "documents")
        targets = re.findall(r"\]\(([^)#\s]+)(?:#[^)]*)?\)", text)
        if fm:
            targets += re.findall(r"^\s+-\s+(\S+\.md)\s*$", fm.group(1), flags=re.M)
        targets += re.findall(r"`((?:[A-Za-z]+/)+[A-Za-z0-9_.-]+\.(?:cs|md|sh|py|json))`", text)
        for t in targets:
            if t.startswith(("http", "mailto")):
                continue
            for base in (os.path.dirname(md), ROOT, os.path.join(ROOT, "RiftRoulette")):
                p = rel(os.path.normpath(os.path.join(base, t)))
                target = path_to_node.get(p) or path_to_node.get(p[:-3] + ".cs" if p.endswith(".md") else p)
                if not target and p.endswith(".cs"):
                    target = "file:" + p if "file:" + p in nodes else None
                if target:
                    add_link(src, target, "links")
                    break


# ------------------------------------------------------------------ outputs

def children(node_id, kind="contains"):
    return sorted((l["target"] for l in links if l["source"] == node_id and l["kind"] == kind),
                  key=lambda n: (nodes[n]["kind"] not in ("feature", "module"), nodes[n]["label"].lower()))


def outgoing(node_id, kinds):
    return sorted({l["target"] for l in links if l["source"] == node_id and l["kind"] in kinds})


def write_tree(path):
    lines = ["---", "type: generated-reference", "generator: scripts/knowledge-graph.py", "---", "",
             "# Repo tree", "",
             "Plugins and test projects, the modules they compile in, their feature folders and",
             "files. Each file shows its doc's first sentence, the commands it registers and",
             "the game dependencies it touches. Generated; do not edit.", ""]

    def render(node_id, prefix, last, out):
        n = nodes[node_id]
        bits = []
        if n["kind"] in ("source", "test"):
            cmds = [nodes[c]["label"] for c in outgoing(node_id, {"registers"})]
            deps = [nodes[d]["label"] for d in outgoing(node_id, {"depends-on", "listens", "hooks", "sends"})]
            if cmds:
                bits.append("cmds: " + " ".join(cmds))
            if deps:
                bits.append("game: " + ", ".join(deps))
        label = n["label"] + ("/" if n["kind"] in ("feature", "module") else (".cs" if n["kind"] in ("source", "test") else ""))
        summary = (" — " + n["summary"]) if n.get("summary") else ""
        extra = ("  `[" + "; ".join(bits) + "]`") if bits else ""
        out.append(f"{prefix}{'└── ' if last else '├── '}**{label}**{summary}{extra}" if prefix is not None else "")
        kids = children(node_id)
        for i, kid in enumerate(kids):
            render(kid, (prefix or "") + ("    " if last else "│   "), i == len(kids) - 1, out)

    roots = [n for n in nodes.values() if n["kind"] in ("plugin", "test-project")]
    roots += [n for n in nodes.values() if n["kind"] == "module" and not any(l["target"] == n["id"] and l["kind"] == "contains" for l in links)]
    for root in sorted(roots, key=lambda n: (n["kind"] != "plugin", n["kind"] == "test-project", n["label"])):
        imports = [nodes[m]["label"] for m in outgoing(root["id"], {"imports"})]
        lines.append(f"## {root['label']} ({root['kind']})")
        lines.append("")
        if root.get("summary"):
            lines.append(root["summary"])
            lines.append("")
        if imports:
            lines.append("Compiles in: " + ", ".join(imports))
            lines.append("")
        out = []
        kids = children(root["id"])
        for i, kid in enumerate(kids):
            render(kid, "", i == len(kids) - 1, out)
        lines.append("<pre>")
        lines.extend(re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", re.sub(r"`\[(.+?)\]`", r"<i>[\1]</i>", l.replace("<", "&lt;"))) for l in out)
        lines.append("</pre>")
        lines.append("")
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


def write_indexes(path):
    users = defaultdict(set)
    for l in links:
        if l["kind"] in ("registers", "depends-on", "listens", "hooks", "sends"):
            users[l["target"]].add(nodes[l["source"]]["path"])

    def cell(s):
        return (s or "").replace("|", "\\|")

    lines = ["---", "type: generated-reference", "generator: scripts/knowledge-graph.py", "---", "",
             "# Indexes", "",
             "Every command, game dependency, hook and type in the code, with where it lives.",
             "Generated from source; the command catalogs (`reference/user-commands.md`,",
             "`admin-commands.md`) stay the authority for behavior.", ""]

    cmds = sorted((n for n in nodes.values() if n["kind"] == "command"), key=lambda n: (n["audience"], n["label"]))
    lines += [f"## Commands ({len(cmds)})", "", "| Command | Who | Description | File |", "|---|---|---|---|"]
    for n in cmds:
        who = n["audience"] + ("".join(", " + f for f in n.get("flags", [])))
        lines.append(f"| `{n['label']}` | {who} | {cell(n.get('description'))} | `{n['file']}` |")
    lines.append("")

    groups = [("Game events", "event"), ("Hooks", "hook"), ("Convars", "convar"), ("Entities (designer names)", "entity"),
              ("Modifiers", "modifier"), ("Modifier states", "modifier-state"), ("Schema fields", "schema-field"),
              ("Abilities", "ability"), ("Net messages", "net-message")]
    lines += ["## Game dependencies", "",
              "What the code touches in the game. A new entry here needs a self-test and a row in",
              "`patch-day.md` §5.", ""]
    for title, kind in groups:
        items = sorted((n for n in nodes.values() if n["kind"] == kind), key=lambda n: n["label"])
        if not items:
            continue
        lines += [f"### {title} ({len(items)})", "", "| Name | Used in |", "|---|---|"]
        for n in items:
            lines.append(f"| `{n['label']}` | " + ", ".join(f"`{p}`" for p in sorted(users[n["id"]])) + " |")
        lines.append("")

    stages = sorted((n for n in nodes.values() if n["kind"] == "stage"), key=lambda n: n["order"])
    lines += [f"## Stages ({len(stages)})", "", "From `RiftRoulette/reference/master-plan.md`; each links to the code its entry names.", "",
              "| Stage | Status | Touches |", "|---|---|---|"]
    for n in stages:
        touched = [nodes[t]["label"] for t in outgoing(n["id"], {"touches"})]
        lines.append(f"| {cell(n['label'])} | {n['status']} | {cell(', '.join(touched[:12]) + (' ...' if len(touched) > 12 else ''))} |")
    lines.append("")

    modes = sorted((n for n in nodes.values() if n["kind"] == "game-mode"), key=lambda n: (n["status"] != "shipped", n["status"] != "in progress", n["label"]))
    lines += [f"## Game modes ({len(modes)})", "", "From `knowledge/game-mode-recipes.md`; each links to the levers and code it uses.", "",
              "| Mode | Status | Uses |", "|---|---|---|"]
    for n in modes:
        used = [nodes[t]["label"] for t in outgoing(n["id"], {"mode-uses", "planned-in"})]
        lines.append(f"| {cell(n['label'])} | {n['status']} | {cell(', '.join(used))} |")
    lines.append("")

    plugins = sorted((n for n in nodes.values() if n["kind"] == "plugin"), key=lambda n: (n.get("role") != "game type", n["label"]))
    lines += [f"## Plugins ({len(plugins)})", "", "Game types run one per server; tools run beside any of them.", "",
              "| Plugin | Role | Engine modules |", "|---|---|---|"]
    for n in plugins:
        modules = [nodes[m]["label"] for m in outgoing(n["id"], {"imports"})]
        lines.append(f"| {n['label']} | {n.get('role', '-')} | {', '.join(modules)} |")
    lines.append("")

    files = sorted((n for n in nodes.values() if n["kind"] == "source"), key=lambda n: n["path"])
    lines += [f"## Types ({sum(len(n.get('types', [])) for n in files)})", "", "| Type | File | Summary |", "|---|---|---|"]
    for n in files:
        for t in n.get("types", []):
            lines.append(f"| `{t}` | `{n['path']}` | {cell(n.get('summary'))} |")
    lines.append("")

    docs = sorted((n for n in nodes.values() if n["kind"] in ("doc", "knowledge")), key=lambda n: n["path"])
    lines += [f"## Docs ({len(docs)})", "", "| Doc | Title | Summary |", "|---|---|---|"]
    for n in docs:
        lines.append(f"| `{n['path']}` | {cell(n['label'])} | {cell(n.get('summary'))} |")
    lines.append("")
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


def main():
    build_containers()
    build_sources(load_convars(), load_map_entities())
    build_scripts()
    build_docs()
    build_feature_deps()
    build_stages()
    build_game_modes()
    mark_roles()
    seen = set()
    unique = []
    for l in links:
        key = (l["source"], l["target"], l["kind"])
        if key not in seen:
            seen.add(key)
            unique.append(l)
    links[:] = unique

    os.makedirs(OUT, exist_ok=True)
    counts = defaultdict(int)
    for n in nodes.values():
        counts[n["kind"]] += 1
    graph = {
        "directed": True,
        "multigraph": False,
        "graph": {"name": "bublock", "generated": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
                  "generator": "scripts/knowledge-graph.py", "counts": dict(sorted(counts.items()))},
        "nodes": sorted(nodes.values(), key=lambda n: n["id"]),
        "links": links,
    }
    with open(os.path.join(OUT, "graph.json"), "w", encoding="utf-8") as f:
        json.dump(graph, f, indent=1, ensure_ascii=False)
    write_tree(os.path.join(OUT, "tree.md"))
    write_indexes(os.path.join(OUT, "indexes.md"))
    print(f"Graph: {len(nodes)} nodes, {len(links)} links "
          f"({', '.join(f'{k} {v}' for k, v in sorted(counts.items()))}) -> {rel(OUT)}/")


if __name__ == "__main__":
    sys.exit(main())

"""Apply a ben-manes gradle-versions-plugin JSON report to inline versions in *.gradle.kts files.

Usage: python gradle_updates.py <report.json> <file.gradle.kts> [<file.gradle.kts> ...]
Rewrites the files in place and prints one line per applied update.
"""
import json
import re
import sys

_UNSTABLE = re.compile(r"(alpha|beta|rc|cr|m\d|milestone|preview|snapshot|dev|eap)", re.IGNORECASE)


def is_stable(version):
    # Qualifiers like -jre / -android are stable; alpha/beta/rc/M1/SNAPSHOT/dev/eap are not.
    qualifier = version.split("-", 1)[1] if "-" in version else ""
    return not (qualifier and _UNSTABLE.search(qualifier))


def _candidate(entry):
    available = entry.get("available") or {}
    return available.get("release") or available.get("milestone") or available.get("integration")


def apply_updates(report, files):
    """Return (updated_files, applied_descriptions). `files` maps path -> text; inputs are not mutated."""
    out = dict(files)
    applied = []
    for dep in (report.get("outdated") or {}).get("dependencies", []):
        group, name, current, candidate = dep["group"], dep["name"], dep["version"], _candidate(dep)
        if not candidate or candidate == current:
            continue
        if is_stable(current) and not is_stable(candidate):
            continue

        if name == f"{group}.gradle.plugin":
            pattern = re.compile(r'(id\("' + re.escape(group) + r'"\)\s+version\s+")' + re.escape(current) + '"')
            replacement = r"\g<1>" + candidate + '"'
            label = group
        else:
            pattern = re.compile('"' + re.escape(f"{group}:{name}:{current}") + '"')
            replacement = f'"{group}:{name}:{candidate}"'
            label = f"{group}:{name}"

        changed = False
        for path, text in out.items():
            new_text, count = pattern.subn(replacement, text)
            if count:
                out[path] = new_text
                changed = True
        if changed:
            applied.append(f"{label}: {current} -> {candidate}")
    return out, applied


def main(argv):
    if len(argv) < 3:
        print(__doc__, file=sys.stderr)
        return 2
    with open(argv[1], encoding="utf-8") as f:
        report = json.load(f)
    files = {}
    for path in argv[2:]:
        # newline="" keeps CRLF line endings intact so only the version strings change.
        with open(path, encoding="utf-8", newline="") as f:
            files[path] = f.read()
    updated, applied = apply_updates(report, files)
    for path, text in updated.items():
        if text != files[path]:
            with open(path, "w", encoding="utf-8", newline="") as f:
                f.write(text)
    for line in applied:
        print(line)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

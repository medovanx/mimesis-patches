"""Releases one or more patches everywhere players get them, in one go.

    python tools/release.py Fov HostOptions --note "Fixed the slider" [--minor] [--no-thunderstore] [--dry-run]

For each patch: bump the version (patch number, or minor with --minor), add the note to its CHANGELOG, update the
version in its README and the root table, build it and package it for Thunderstore. Then commit and push, create the
GitHub release (installer users and the in-game Update button get it from there) and upload the package to
Thunderstore (Gale / r2modman users get it from there).

Thunderstore uploads need a service account token (thunderstore.io > Teams > medovanx > Service accounts) in the
THUNDERSTORE_TOKEN environment variable. Only patches listed in THUNDERSTORE are uploaded there.
"""
import argparse, json, os, re, subprocess, sys, tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(Path(__file__).resolve().parent))
from package_thunderstore import package, DESCRIPTIONS   # noqa: E402

REPO = "medovanx/mimesis-patches"
TEAM = "medovanx"
COMMUNITY = "mimesis"
CATEGORIES = ["mods"]
# Patches published on Thunderstore (tested in multiplayer). Others are GitHub only for now.
THUNDERSTORE = {"BiggerLobby", "Fov", "FpsCounter", "HostOptions", "HudPercent", "Inventory", "Minimap", "PSController"}


def run(*cmd, cwd=ROOT, dry=False):
    print("  $", " ".join(str(c) for c in cmd))
    if not dry:
        subprocess.run(cmd, cwd=cwd, check=True)


def bump(patch, minor):
    csproj = next((ROOT / patch / "Runtime").glob("*.csproj"))
    text = csproj.read_text(encoding="utf-8")
    old = re.search(r"<Version>([^<]+)</Version>", text).group(1)
    major, mid, low = (int(x) for x in old.split("."))
    new = f"{major}.{mid + 1}.0" if minor else f"{major}.{mid}.{low + 1}"
    csproj.write_text(text.replace(f"<Version>{old}</Version>", f"<Version>{new}</Version>"), encoding="utf-8")
    readme = ROOT / patch / "README.md"
    readme.write_text(re.sub(r"\*\*Version [0-9.]+\*\*", f"**Version {new}**", readme.read_text(encoding="utf-8")), encoding="utf-8")
    root = ROOT / "README.md"
    root.write_text(re.sub(rf"(\| \[{patch}\]\({patch}/\) \| )[0-9.]+", rf"\g<1>{new}", root.read_text(encoding="utf-8")), encoding="utf-8")
    return old, new


def changelog(patch, version, note):
    path = ROOT / patch / "CHANGELOG.md"
    text = path.read_text(encoding="utf-8")
    entry = f"## {version}\n" + "".join(f"- {line.strip()}\n" for line in note.split("\n") if line.strip()) + "\n"
    head = f"# {patch} changelog\n\n"
    path.write_text(text.replace(head, head + entry, 1) if text.startswith(head) else head + entry + text, encoding="utf-8")
    return entry


def build(patch, dry):
    run("dotnet", "build", "-c", "Release", cwd=ROOT / patch / "Runtime", dry=dry)
    if dry:
        return None
    return next(p for p in (ROOT / patch / "Runtime" / "bin" / "Release").rglob(f"{patch}Runtime.dll"))


def upload_thunderstore(zip_path, token):
    """Thunderstore's upload API (the same one their CLI uses)."""
    import urllib.request, uuid
    boundary = uuid.uuid4().hex
    metadata = {"author_name": TEAM, "communities": [COMMUNITY], "categories": [],
                "community_categories": {COMMUNITY: CATEGORIES}, "has_nsfw_content": False}
    body = b"".join([
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"metadata\"\r\n\r\n{json.dumps(metadata)}\r\n".encode(),
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{zip_path.name}\"\r\n"
        f"Content-Type: application/zip\r\n\r\n".encode(), zip_path.read_bytes(), f"\r\n--{boundary}--\r\n".encode()])
    req = urllib.request.Request("https://thunderstore.io/api/experimental/package/upload/", data=body, method="POST",
                                 headers={"Authorization": f"Bearer {token}", "Content-Type": f"multipart/form-data; boundary={boundary}"})
    try:
        with urllib.request.urlopen(req, timeout=600) as r:
            return r.status, r.read().decode()[:300]
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()[:500]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("patches", nargs="+", help="patch folder names, e.g. Fov HostOptions")
    ap.add_argument("--note", required=True, help="what changed (one CHANGELOG bullet per line)")
    ap.add_argument("--minor", action="store_true", help="bump the minor version instead of the patch number")
    ap.add_argument("--no-thunderstore", action="store_true", help="GitHub release only")
    ap.add_argument("--dry-run", action="store_true", help="show what would happen, change nothing")
    args = ap.parse_args()

    for p in args.patches:
        if p not in DESCRIPTIONS:
            sys.exit(f"Unknown patch: {p}")
    token = os.environ.get("THUNDERSTORE_TOKEN")
    ts = not args.no_thunderstore
    if ts and not token and any(p in THUNDERSTORE for p in args.patches):
        sys.exit("Set THUNDERSTORE_TOKEN (Thunderstore service account token), or pass --no-thunderstore.")

    out = Path(tempfile.mkdtemp(prefix="mimesis-release-"))
    done = []
    for p in args.patches:
        print(f"== {p}")
        if args.dry_run:
            print(f"  would bump, add to CHANGELOG, build and package {p}")
            continue
        old, new = bump(p, args.minor)
        entry = changelog(p, new, args.note)
        dll = build(p, False)
        done.append((p, new, entry, dll, package(p, out)))
        print(f"  {old} -> {new}")

    if args.dry_run:
        return
    names = ", ".join(f"{p} {v}" for p, v, *_ in done)
    run("git", "add", "-A")
    run("git", "commit", "-q", "-m", f"Release {names}")
    run("git", "push", "-q")

    for p, v, entry, dll, zip_path in done:
        notes = entry.split("\n", 1)[1] + f"\nInstall: **MimesisPatchesInstaller.exe** or the in-game Patches window, or from Thunderstore with Gale / r2modman. Full history: [CHANGELOG](https://github.com/{REPO}/blob/main/{p}/CHANGELOG.md)"
        run("gh", "release", "create", f"{p}-v{v}", str(dll), str(zip_path), "-t", f"{p} {v}", "--latest=false", "-n", notes)
        if ts and p in THUNDERSTORE:
            status, reply = upload_thunderstore(zip_path, token)
            print(f"  Thunderstore: {'uploaded' if status < 300 else f'FAILED ({status}) {reply}'}")
    print(f"\nReleased {names}. Packages are in {out}")


if __name__ == "__main__":
    main()

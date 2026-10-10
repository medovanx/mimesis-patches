"""Builds a Thunderstore package (zip) for each patch from its built plugin DLL.

Usage: python tools/package_thunderstore.py <output folder>
Run `dotnet build -c Release` in each <Patch>/Runtime first. Each zip holds manifest.json, icon.png (256x256),
README.md and the mod DLL (at the root, like other MIMESIS MelonLoader packages).
"""
import json, re, sys, zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
REPO = "https://github.com/medovanx/mimesis-patches"
MELONLOADER = "LavaGang-MelonLoader-0.7.3"

DESCRIPTIONS = {
    "BiggerLobby": "10-player lobbies with UI for 10 and difficulty that scales with the group size. Host only.",
    "PSController": "PlayStation button icons instead of Xbox ones, and prefers the DS4Windows pad when both are connected.",
    "SpectatorCam": "Free spectator camera while dead, limited to the area around the player you're watching.",
    "Minimap": "Minimap of the level: explored or full map, plain or graphic style, optional player, monster and item dots.",
    "HostOptions": "Host options: infinite stamina, starting money, and Easy/Normal/Hard presets or sliders for quota, shop prices, sell value and monsters.",
    "HudPercent": "Health and radiation percentages next to the HUD bars.",
    "Inventory": "The host sets 1-8 inventory slots, shown as a 2x4 grid. Every player needs it.",
    "LateJoin": "Join a game in progress: you enter at the tram when the team returns. Host and joining players need it.",
    "Fov": "Field of view slider (50-120). Tap or hold [ and ] in game to change it on the fly.",
    "Revive": "Stand next to a dead teammate for a few seconds to revive them. Host only.",
    "FpsCounter": "Shows your frame rate in the bottom-right corner (FPS: 144).",
}


def version(patch):
    csproj = next((ROOT / patch / "Runtime").glob("*.csproj")).read_text(encoding="utf-8")
    return re.search(r"<Version>([^<]+)</Version>", csproj).group(1)


def readme(patch):
    """The patch README with relative links made absolute: Thunderstore shows it outside the repo, so
    "icon.png" or "../Installer/" would point nowhere. Images load from raw.githubusercontent.com."""
    text = (ROOT / patch / "README.md").read_text(encoding="utf-8")
    raw = f"https://raw.githubusercontent.com/medovanx/mimesis-patches/main/{patch}/"
    page = f"{REPO}/tree/main/{patch}/"

    def fix(path, base):
        if re.match(r"[a-z]+://|#|mailto:", path):
            return path
        return requests_join(base, path)

    text = re.sub(r'src="([^"]+)"', lambda m: f'src="{fix(m.group(1), raw)}"', text)
    text = re.sub(r"!\[([^\]]*)\]\(([^)]+)\)", lambda m: f"![{m.group(1)}]({fix(m.group(2), raw)})", text)
    text = re.sub(r"(?<!!)\[([^\]]*)\]\(([^)]+)\)", lambda m: f"[{m.group(1)}]({fix(m.group(2), page)})", text)
    return text


def requests_join(base, path):
    from urllib.parse import urljoin
    return urljoin(base, path)


def package(patch, out):
    v = version(patch)
    dll = next((ROOT / patch / "Runtime" / "bin" / "Release").rglob(f"{patch}Runtime.dll"))
    manifest = {
        "name": patch,
        "version_number": v,
        "website_url": f"{REPO}/tree/main/{patch}",
        "description": DESCRIPTIONS[patch][:250],
        "dependencies": [MELONLOADER],
    }
    target = out / f"{patch}-{v}.zip"
    with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as z:
        z.writestr("manifest.json", json.dumps(manifest, indent=2))
        z.writestr("README.md", readme(patch))
        z.write(ROOT / patch / "icon.png", "icon.png")   # <Patch>/icon.png, drawn by tools/icons.py
        z.write(dll, dll.name)   # at the root, like other MIMESIS packages; mod managers put it in Mods
    return target


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "thunderstore")
    out.mkdir(parents=True, exist_ok=True)
    for p in DESCRIPTIONS:
        print(package(p, out))

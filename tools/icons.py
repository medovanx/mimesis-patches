"""Draws the 256x256 patch icons (Thunderstore): foggy dark background, a symbol per patch, gold name band.

Drawn at 4x and scaled down for smooth edges. Saved as <Patch>/icon.png (shown in the README, used by the
Thunderstore package): python tools/icons.py --save. Or preview into a folder: python tools/icons.py <output folder>
"""
import math, random, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

S = 1024                      # drawing size (scaled to 256)
GOLD = (240, 196, 92)
WHITE = (236, 238, 232)
DIM = (150, 160, 165)


def font(name, size):
    try:
        return ImageFont.truetype(name, size)
    except OSError:
        return ImageFont.load_default()


def background(seed):
    """Dark blue-grey fog with a soft light behind the symbol and a vignette, like the game's menu art."""
    rnd = random.Random(seed)
    img = Image.new("RGB", (S, S), (14, 18, 22))
    fog = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(fog)
    for _ in range(26):
        x, y, r = rnd.randint(0, S), rnd.randint(0, int(S * 0.8)), rnd.randint(120, 320)
        d.ellipse([x - r, y - r, x + r, y + r], fill=rnd.randint(20, 55))
    d.ellipse([S * 0.18, S * 0.08, S * 0.82, S * 0.72], fill=90)   # glow behind the symbol
    fog = fog.filter(ImageFilter.GaussianBlur(90))
    img = Image.composite(Image.new("RGB", (S, S), (70, 88, 98)), img, fog)
    vignette = Image.new("L", (S, S), 0)
    ImageDraw.Draw(vignette).ellipse([-S * 0.25, -S * 0.25, S * 1.25, S * 1.25], fill=255)
    vignette = vignette.filter(ImageFilter.GaussianBlur(120))
    return Image.composite(img, Image.new("RGB", (S, S), (6, 8, 10)), vignette)


# ---- symbols: each draws in the box (cx, cy) +- 250 ----
def person(d, x, y, s, fill):
    d.ellipse([x - 0.22 * s, y - 0.62 * s, x + 0.22 * s, y - 0.18 * s], fill=fill)
    d.pieslice([x - 0.42 * s, y - 0.12 * s, x + 0.42 * s, y + 0.72 * s], 180, 360, fill=fill)


def biggerlobby(d, cx, cy):
    for i, (dx, s) in enumerate([(-170, 150), (170, 150), (-85, 175), (85, 175), (0, 210)]):
        person(d, cx + dx, cy + (30 if s < 160 else 10 if s < 200 else 0), s, WHITE if s == 210 else (190, 196, 196))
    d.text((cx + 175, cy - 175), "10", font=font("seguibl.ttf", 150), fill=GOLD, anchor="mm")


def pscontroller(d, cx, cy):
    w = 34
    d.polygon([(cx, cy - 268), (cx - 106, cy - 76), (cx + 106, cy - 76)], outline=WHITE, width=w - 4)            # triangle
    d.ellipse([cx + 95, cy - 70, cx + 235, cy + 70], outline=WHITE, width=w)                               # circle
    d.line([(cx - 65, cy + 95), (cx + 65, cy + 225)], fill=WHITE, width=w)                                 # cross
    d.line([(cx + 65, cy + 95), (cx - 65, cy + 225)], fill=WHITE, width=w)
    d.rectangle([cx - 235, cy - 68, cx - 99, cy + 68], outline=WHITE, width=w)                             # square


def spectatorcam(d, cx, cy):
    d.rounded_rectangle([cx - 230, cy - 120, cx + 110, cy + 140], 40, fill=WHITE)
    d.polygon([(cx + 110, cy - 20), (cx + 240, cy - 110), (cx + 240, cy + 130), (cx + 110, cy + 40)], fill=WHITE)
    d.ellipse([cx - 160, cy - 60, cx + 40, cy + 140], fill=(30, 38, 44))
    d.ellipse([cx - 110, cy - 10, cx - 10, cy + 90], fill=GOLD)
    d.ellipse([cx - 200, cy - 230, cx - 80, cy - 110], outline=WHITE, width=26)
    d.ellipse([cx - 40, cy - 230, cx + 80, cy - 110], outline=WHITE, width=26)


def minimap(d, cx, cy):
    d.rounded_rectangle([cx - 230, cy - 230, cx + 230, cy + 230], 50, fill=(28, 36, 42), outline=WHITE, width=24)
    for x0, y0, x1, y1 in [(-150, -150, -40, 40), (-40, -20, 150, 40), (60, -150, 150, -20), (-150, 40, -90, 160), (40, 40, 150, 160)]:
        d.rectangle([cx + x0, cy + y0, cx + x1, cy + y1], fill=(120, 132, 138))
    d.polygon([(cx, cy - 70), (cx - 45, cy + 40), (cx, cy + 15), (cx + 45, cy + 40)], fill=GOLD)
    for x, y, c in [(-110, -100, (220, 80, 70)), (110, 100, (90, 200, 120)), (100, -90, (90, 160, 230))]:
        d.ellipse([cx + x - 22, cy + y - 22, cx + x + 22, cy + y + 22], fill=c)


def hostoptions(d, cx, cy):
    for i, (y, k) in enumerate([(-150, 0.3), (0, 0.75), (150, 0.5)]):
        d.line([(cx - 220, cy + y), (cx + 220, cy + y)], fill=(120, 132, 138), width=26)
        d.line([(cx - 220, cy + y), (cx - 220 + 440 * k, cy + y)], fill=GOLD, width=26)
        x = cx - 220 + 440 * k
        d.ellipse([x - 48, cy + y - 48, x + 48, cy + y + 48], fill=WHITE)


def hudpercent(d, cx, cy):
    d.line([(cx - 240, cy - 60), (cx - 120, cy - 60), (cx - 80, cy - 170), (cx - 20, cy + 60), (cx + 30, cy - 60), (cx + 240, cy - 60)],
           fill=(110, 220, 120), width=30, joint="curve")
    d.text((cx, cy + 130), "100%", font=font("seguibl.ttf", 190), fill=WHITE, anchor="mm")


def inventory(d, cx, cy):
    for r in range(2):
        for c in range(4):
            x, y = cx - 230 + c * 120, cy - 120 + r * 130
            d.rounded_rectangle([x, y, x + 100, y + 110], 18, fill=(40, 34, 20), outline=GOLD if (r, c) != (1, 0) else WHITE, width=12)
    d.ellipse([cx - 195, cy + 35, cx - 145, cy + 85], fill=WHITE)


def latejoin(d, cx, cy):
    d.rounded_rectangle([cx - 20, cy - 230, cx + 220, cy + 230], 30, outline=WHITE, width=30)
    d.line([(cx - 240, cy), (cx + 80, cy)], fill=GOLD, width=44)
    d.polygon([(cx + 140, cy), (cx + 40, cy - 100), (cx + 40, cy + 100)], fill=GOLD)


def fov(d, cx, cy):
    d.pieslice([cx - 330, cy - 330, cx + 330, cy + 330], 215, 325, fill=(70, 84, 92))
    d.arc([cx - 330, cy - 330, cx + 330, cy + 330], 215, 325, fill=GOLD, width=26)
    d.line([(cx, cy + 40), (cx - 270, cy - 190)], fill=WHITE, width=22)
    d.line([(cx, cy + 40), (cx + 270, cy - 190)], fill=WHITE, width=22)
    d.ellipse([cx - 120, cy + 10, cx + 120, cy + 190], fill=WHITE)
    d.ellipse([cx - 55, cy + 45, cx + 55, cy + 155], fill=(30, 38, 44))


def revive(d, cx, cy):
    d.ellipse([cx - 230, cy - 200, cx + 10, cy + 40], fill=(220, 80, 70))
    d.ellipse([cx - 10, cy - 200, cx + 230, cy + 40], fill=(220, 80, 70))
    d.polygon([(cx - 222, cy - 50), (cx + 222, cy - 50), (cx, cy + 230)], fill=(220, 80, 70))
    d.rectangle([cx - 30, cy - 130, cx + 30, cy + 70], fill=WHITE)
    d.rectangle([cx - 100, cy - 60, cx + 100, cy], fill=WHITE)


def fpscounter(d, cx, cy):
    d.arc([cx - 240, cy - 200, cx + 240, cy + 280], 180, 360, fill=(120, 132, 138), width=40)
    d.arc([cx - 240, cy - 200, cx + 240, cy + 280], 180, 300, fill=GOLD, width=40)
    a = math.radians(300)
    d.line([(cx, cy + 40), (cx + 190 * math.cos(a), cy + 40 + 190 * math.sin(a))], fill=WHITE, width=28)
    d.ellipse([cx - 40, cy, cx + 40, cy + 80], fill=WHITE)
    d.text((cx, cy + 180), "FPS", font=font("seguibl.ttf", 150), fill=WHITE, anchor="mm")


SYMBOLS = {
    "BiggerLobby": biggerlobby, "PSController": pscontroller, "SpectatorCam": spectatorcam, "Minimap": minimap,
    "HostOptions": hostoptions, "HudPercent": hudpercent, "Inventory": inventory, "LateJoin": latejoin,
    "Fov": fov, "Revive": revive, "FpsCounter": fpscounter,
}


def icon(patch):
    img = background(patch)
    d = ImageDraw.Draw(img)
    # Soft shadow under the symbol, then the symbol itself.
    shadow = Image.new("L", (S, S), 0)
    SYMBOLS[patch](_Mask(shadow), S // 2, int(S * 0.40))
    img.paste((0, 0, 0), (0, 18), shadow.filter(ImageFilter.GaussianBlur(18)).point(lambda v: v * 0.55))
    SYMBOLS[patch](d, S // 2, int(S * 0.40))
    # Name band along the bottom.
    d.rectangle([0, int(S * 0.78), S, S], fill=(10, 12, 15))
    d.line([(0, int(S * 0.78)), (S, int(S * 0.78))], fill=GOLD, width=10)
    size = 150
    while size > 70 and d.textlength(patch, font=font("seguibl.ttf", size)) > S * 0.88:
        size -= 6
    d.text((S // 2, int(S * 0.89)), patch, font=font("seguibl.ttf", size), fill=GOLD, anchor="mm")
    d.rectangle([0, 0, S - 1, S - 1], outline=(60, 64, 60), width=8)
    return img.resize((256, 256), Image.LANCZOS)


class _Mask:
    """Draws any shape in white on a mask (for the shadow), ignoring the requested colours."""
    def __init__(self, img): self.d = ImageDraw.Draw(img)
    def __getattr__(self, name):
        f = getattr(self.d, name)
        def call(*a, **k):
            for key in ("fill", "outline"):
                if k.get(key) is not None: k[key] = 255
            if name == "text": k["fill"] = 255
            return f(*a, **k)
        return call


if __name__ == "__main__":
    if sys.argv[1:] == ["--save"]:
        root = Path(__file__).resolve().parent.parent
        for p in SYMBOLS:
            icon(p).save(root / p / "icon.png")
        sys.exit()
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "icons")
    out.mkdir(parents=True, exist_ok=True)
    for p in SYMBOLS:
        icon(p).save(out / f"{p}.png")
    # contact sheet
    sheet = Image.new("RGB", (256 * 6, 256 * 2), (0, 0, 0))
    for i, p in enumerate(SYMBOLS):
        sheet.paste(Image.open(out / f"{p}.png"), ((i % 6) * 256, (i // 6) * 256))
    sheet.save(out / "_sheet.png")
    print(out / "_sheet.png")

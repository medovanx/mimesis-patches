import sys, os, math
from PIL import Image, ImageDraw
src, out = sys.argv[1], sys.argv[2]
INK = (100, 70, 10, 255)
CX, CY, R = 37.5, 31.5, 19.5   # inner area of the game's 74x74 button frame

def blank(base):
    im = Image.open(os.path.join(src, base + ".png")).convert("RGBA")
    grad = Image.open(os.path.join(src, "p_lb.png")).convert("RGBA")  # column x=37 is empty background
    px, g = im.load(), grad.load()
    for y in range(74):
        for x in range(74):
            dx, dy = x - CX, y - CY
            inside = (dx*dx + dy*dy <= R*R) if base in ("p_a",) else (abs(dx) <= R - 1 and abs(dy) <= R - 1)
            if inside: px[x, y] = g[37, y]
    return im

FONT = {  # 5x7 pixel glyphs, to match the game's pixel lettering
    "L": ["10000","10000","10000","10000","10000","10000","11111"],
    "R": ["11110","10001","10001","11110","10100","10010","10001"],
    "1": ["00100","01100","00100","00100","00100","00100","01110"],
    "2": ["01110","10001","00001","00010","00100","01000","11111"],
    "3": ["11110","00001","00001","01110","00001","00001","11110"],
}
def text(im, s, scale=3):
    d = ImageDraw.Draw(im)
    w = len(s) * 5 * scale + (len(s) - 1) * scale
    x0, y0 = round(CX - w / 2), round(CY - 7 * scale / 2)
    for i, ch in enumerate(s):
        for r, row in enumerate(FONT[ch]):
            for c, bit in enumerate(row):
                if bit == "1":
                    x, y = x0 + i * 6 * scale + c * scale, y0 + r * scale
                    d.rectangle([x, y, x + scale - 1, y + scale - 1], fill=INK)

def ss_draw(fn):
    # draw supersampled then downscale for clean shapes
    big = Image.new("RGBA", (74*4, 74*4), (0,0,0,0)); fn(ImageDraw.Draw(big), 4)
    return big.resize((74, 74), Image.LANCZOS)

def shape(name, fn):
    im = blank("p_a"); im.alpha_composite(ss_draw(fn)); im.save(os.path.join(out, name + ".png"))

w = 4.5
shape("cross", lambda d, k: [d.line([( (CX-12)*k, (CY-12)*k), ((CX+12)*k, (CY+12)*k)], fill=INK, width=int(w*k)),
                             d.line([( (CX+12)*k, (CY-12)*k), ((CX-12)*k, (CY+12)*k)], fill=INK, width=int(w*k))])
shape("circle", lambda d, k: d.ellipse([(CX-13)*k, (CY-13)*k, (CX+13)*k, (CY+13)*k], outline=INK, width=int(w*k)))
shape("square", lambda d, k: d.rectangle([(CX-12)*k, (CY-12)*k, (CX+10)*k, (CY+10)*k], outline=INK, width=int(w*k)))
def tri(d, k):
    p = [(CX*k, (CY-14)*k), ((CX+14)*k, (CY+10)*k), ((CX-14)*k, (CY+10)*k)]
    d.polygon(p, outline=INK, width=int(w*k))
shape("triangle", tri)

for name, base in [("l1","p_lb"),("r1","p_rb"),("l2","p_lt"),("r2","p_rt")]:
    im = blank(base); text(im, name.upper()); im.save(os.path.join(out, name + ".png"))
for name in ("l3", "r3"):
    im = blank("p_a"); text(im, name.upper()); im.save(os.path.join(out, name + ".png"))
for name, base in [("create","p_select"),("options","p_start"),("dpad_up","p_dpad_up"),("dpad_down","p_dpad_down"),("dpad_left","p_dpad_left"),("dpad_right","p_dpad_right")]:
    Image.open(os.path.join(src, base + ".png")).save(os.path.join(out, name + ".png"))

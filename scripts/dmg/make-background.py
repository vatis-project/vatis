#!/usr/bin/env python3
"""Draws the DMG window background (../dmg-background.tiff): 660x400 plus a 1320x800 @2x representation.

The icon positions (app at 195,170, Applications at 475,170) come from scripts/dmg/build-dmg.py. Run this
only when changing the design; the TIFF is committed. Needs Pillow and ImageMagick (to combine the two sizes);
the logo and fonts come from the repo.

    python3 scripts/dmg/make-background.py
"""
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

W, H = 660, 400
SCALE = 4  # draw large, then downsample for smooth edges
BG_TOP, BG_BOTTOM = (252, 253, 253), (238, 244, 244)
TEAL, AMBER, MUTED = (19, 148, 138), (255, 170, 30), (96, 120, 128)  # the logo's teal and amber
APP = (195, 170)  # icon centre, see build-dmg.py
ROOT = Path(__file__).resolve().parents[2]
LOGO = ROOT / "docs/images/logo-light.png"
FONT = ROOT / "vATIS.Desktop/Assets/Fonts/IBMPlexSans-Regular.ttf"


def chevrons(d, y, s):
    """Three chevrons that grow more solid towards the Applications icon."""
    for i, x in enumerate((300, 326, 352)):
        pts = [(x * s, y - 16 * s), ((x + 14) * s, y), (x * s, y + 16 * s)]
        d.line(pts, fill=TEAL + (70 + 90 * i,), width=6 * s, joint="curve")
        for px, py in (pts[0], pts[2]):
            d.ellipse([px - 3 * s, py - 3 * s, px + 3 * s, py + 3 * s], fill=TEAL + (70 + 90 * i,))


def dots(d, y, s):
    """A row of dots that grow towards a filled head."""
    for i, x in enumerate(range(290, 354, 14)):
        r = (1.8 + i * 0.5) * s
        d.ellipse([x * s - r, y - r, x * s + r, y + r], fill=TEAL)
    d.polygon([(358 * s, y - 14 * s), (382 * s, y), (358 * s, y + 14 * s)], fill=TEAL)


def pill(d, y, s):
    """A filled rounded pill with a chevron inside."""
    d.rounded_rectangle([300 * s, y - 20 * s, 370 * s, y + 20 * s], radius=20 * s, fill=TEAL)
    pts = [(329 * s, y - 9 * s), (342 * s, y), (329 * s, y + 9 * s)]
    d.line(pts, fill=(255, 255, 255), width=4 * s, joint="curve")
    for px, py in (pts[0], pts[2]):
        d.ellipse([px - 2 * s, py - 2 * s, px + 2 * s, py + 2 * s], fill=(255, 255, 255))


def line(d, y, s):
    """A thin shaft with an open head."""
    d.rounded_rectangle([290 * s, y - 2 * s, 366 * s, y + 2 * s], radius=2 * s, fill=TEAL)
    head = [(358 * s, y - 14 * s), (380 * s, y), (358 * s, y + 14 * s)]
    d.line(head, fill=TEAL, width=5 * s, joint="curve")
    for px, py in (head[0], head[2]):
        d.ellipse([px - 2.5 * s, py - 2.5 * s, px + 2.5 * s, py + 2.5 * s], fill=TEAL)


def waves(d, y, s):
    """Broadcast arcs, as in the logo, getting larger towards the Applications icon."""
    for i, x in enumerate((296, 318, 340)):
        r = (12 + i * 7) * s
        d.arc([x * s - r, y - r, x * s + r, y + r], -45, 45, fill=AMBER + (110 + 70 * i,), width=int(4.5 * s))
    d.ellipse([284 * s - 4 * s, y - 4 * s, 284 * s + 4 * s, y + 4 * s], fill=AMBER)


def circle(d, y, s):
    """A teal disc with a white arrow."""
    cx = 335 * s
    d.ellipse([cx - 24 * s, y - 24 * s, cx + 24 * s, y + 24 * s], fill=TEAL)
    d.line([(cx - 11 * s, y), (cx + 10 * s, y)], fill=(255, 255, 255), width=4 * s)
    pts = [(cx + 2 * s, y - 9 * s), (cx + 11 * s, y), (cx + 2 * s, y + 9 * s)]
    d.line(pts, fill=(255, 255, 255), width=4 * s, joint="curve")


def gradient(d, y, s):
    """A thick shaft fading from teal to amber, with a solid head."""
    for x in range(286 * s, 352 * s):
        k = (x - 286 * s) / (66 * s)
        d.line([(x, y - 4 * s), (x, y + 4 * s)], fill=tuple(round(a + (b - a) * k) for a, b in zip(TEAL, AMBER)))
    d.polygon([(348 * s, y - 17 * s), (380 * s, y), (348 * s, y + 17 * s)], fill=AMBER)


def flight(d, y, s):
    """An aircraft climbing out of the app icon and descending towards Applications, with a trail that
    thickens and brightens behind it and an amber marker at the destination."""
    import math

    p0, p1, p2, p3 = (268, y / s + 6), (298, y / s - 46), (372, y / s - 54), (402, y / s - 2)

    def point(t):
        u = 1 - t
        return tuple((u**3 * a + 3 * u * u * t * b + 3 * u * t * t * c + t**3 * e) * s
                     for a, b, c, e in zip(p0, p1, p2, p3))

    # Trail: dots that grow and brighten towards the aircraft.
    n, plane_t = 32, 0.8
    for i in range(n):
        t = plane_t * i / n
        k = i / n
        px, py = point(t)
        r = (1.0 + 2.0 * k) * s
        d.ellipse([px - r, py - r, px + r, py + r], fill=TEAL + (round(35 + 220 * k**1.3),))

    # Destination marker: amber dot and ring.
    ex, ey = point(1.0)
    d.ellipse([ex - 9 * s, ey - 9 * s, ex + 9 * s, ey + 9 * s], outline=AMBER + (200,), width=round(2 * s))
    d.ellipse([ex - 3.5 * s, ey - 3.5 * s, ex + 3.5 * s, ey + 3.5 * s], fill=AMBER)

    # Aircraft, nose along the path.
    (x1, y1), (x2, y2) = point(plane_t - 0.02), point(plane_t + 0.02)
    ang = math.atan2(y2 - y1, x2 - x1)
    shape = [(14, 0), (5, -2), (-1, -13), (-5, -13), (-3, -2), (-9, -1.5), (-11, -5), (-13, -5), (-11, 0),
             (-13, 5), (-11, 5), (-9, 1.5), (-3, 2), (-5, 13), (-1, 13), (5, 2)]
    cx, cy = point(plane_t)
    poly = [(cx + (px * math.cos(ang) - py * math.sin(ang)) * 1.15 * s,
             cy + (px * math.sin(ang) + py * math.cos(ang)) * 1.15 * s) for px, py in shape]
    d.polygon(poly, fill=TEAL)


def double(d, y, s):
    """Two parallel strokes, one solid and one lighter, with an open head."""
    d.rounded_rectangle([290 * s, y - 6 * s, 362 * s, y - 2 * s], radius=2 * s, fill=TEAL)
    d.rounded_rectangle([290 * s, y + 3 * s, 340 * s, y + 7 * s], radius=2 * s, fill=TEAL + (110,))
    head = [(354 * s, y - 17 * s), (378 * s, y - 4 * s), (354 * s, y + 9 * s)]
    d.line(head, fill=TEAL, width=5 * s, joint="curve")


ARROWS = {"chevrons": chevrons, "dots": dots, "pill": pill, "line": line,
          "waves": waves, "circle": circle, "gradient": gradient, "flight": flight, "double": double}
STYLE = "chevrons"


def render(style=STYLE):
    s = SCALE
    img = Image.new("RGB", (1, 2))
    img.putdata([BG_TOP, BG_BOTTOM])
    img = img.resize((W * s, H * s), Image.BICUBIC).convert("RGBA")

    logo = Image.open(LOGO).convert("RGBA")
    lh = 56 * s
    logo = logo.resize((round(logo.width * lh / logo.height), lh), Image.LANCZOS)
    img.alpha_composite(logo, ((W * s - logo.width) // 2, 16 * s))

    d = ImageDraw.Draw(img)
    f = ImageFont.truetype(str(FONT), 14 * s)
    t = "Drag vATIS into the Applications folder"
    d.text(((W * s - d.textlength(t, font=f)) / 2, 76 * s), t, font=f, fill=MUTED)

    # Own layer, so the arrows' translucent strokes blend instead of overwriting the background.
    arrow = Image.new("RGBA", img.size, (255, 255, 255, 0))
    ARROWS[style](ImageDraw.Draw(arrow), y=APP[1] * s, s=s)
    img = Image.alpha_composite(img, arrow)
    return img.convert("RGB")


def main():
    out = Path(__file__).resolve().parent.parent / "dmg-background.tiff"
    big = render()
    with tempfile.TemporaryDirectory() as d:
        one, two = Path(d, "bg.png"), Path(d, "bg@2x.png")
        big.resize((W, H), Image.LANCZOS).save(one, dpi=(72, 72))
        big.resize((W * 2, H * 2), Image.LANCZOS).save(two, dpi=(144, 144))
        # Two representations in one TIFF: Finder picks the @2x one on Retina displays.
        subprocess.run(["magick", str(one), str(two), "-compress", "LZW", str(out)], check=True)
    print("wrote", out)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Generate the vATIS icon assets from the macOS Icon Composer source (scripts/macos/AppIcon.icon).

The gradient comes from icon.json and the layers (and their stacking order) from icon.json plus Assets/*.svg, so
changing the icon's color is a matter of editing those two files and re-running this script.

    python3 scripts/icons/generate.py app      # Windows/Linux icon + a fallback .icns, written in place
    python3 scripts/icons/generate.py web      # website favicon and BETA logo (512 and 1024 px)
    python3 scripts/icons/generate.py social   # Discord server icon and GitHub avatar
    python3 scripts/icons/generate.py all --out some/dir

Requires: pip install pillow numpy scipy cairosvg  (the BETA ribbon also needs a bold sans font, see FONTS).

Not covered: the README logos (docs/images/logo-*.png) and Assets.car. The logos are the icon plus the "vATIS"
wordmark and are recolored by hand; Assets.car and the real AppIcon.icns come from `actool` on a Mac, see
scripts/macos/README.md. The .icns written by the `app` command is only a fallback for machines without a Mac.
"""
import argparse
import io
import json
import os
from pathlib import Path

import cairosvg
import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ICON_DIR = ROOT / 'scripts/macos/AppIcon.icon'
SHADOW_RGB = (0, 25, 35)           # tint of the soft shadow under the artwork (dark teal)
FONTS = [                          # first one that exists is used for the BETA ribbon text
    '/usr/share/fonts/google-noto/NotoSans-ExtraBold.ttf',
    '/usr/share/fonts/truetype/noto/NotoSans-ExtraBold.ttf',
    '/System/Library/Fonts/Supplemental/Arial Bold.ttf',
    'C:/Windows/Fonts/arialbd.ttf',
]


def read_source():
    """Return (gradient top color, gradient bottom color, layer names from bottom to top)."""
    data = json.loads((ICON_DIR / 'icon.json').read_text())
    top, bottom = [tuple(round(float(v) * 255) for v in c.split(':')[1].split(',')[:3])
                   for c in data['fill']['linear-gradient']]
    names = [Path(layer['image-name']).stem for layer in data['groups'][0]['layers']]
    return top, bottom, names[::-1]        # Icon Composer lists the topmost layer first


def gradient(size, top, bottom):
    t = np.linspace(0, 1, size)[:, None, None]
    col = np.array(top)[None, None, :] * (1 - t) + np.array(bottom)[None, None, :] * t
    return Image.fromarray(np.repeat(col, size, axis=1).astype('uint8'), 'RGB').convert('RGBA')


def foreground(body, names):
    fg = Image.new('RGBA', (body, body), (0, 0, 0, 0))
    for name in names:
        png = cairosvg.svg2png(url=str(ICON_DIR / 'Assets' / (name + '.svg')), output_width=body, output_height=body)
        fg.alpha_composite(Image.open(io.BytesIO(png)).convert('RGBA'))
    return fg


def with_shadow(fg):
    body = fg.width
    sh = fg.getchannel('A').filter(ImageFilter.GaussianBlur(body * 0.012))
    sh = ImageChops.offset(sh, 0, int(body * 0.010)).point(lambda a: int(a * 0.40))
    shadow = Image.new('RGBA', fg.size, SHADOW_RGB + (0,))
    shadow.putalpha(sh)
    return Image.alpha_composite(shadow, fg)


def render_icon(canvas, fill=0.97, radius=0.224, art=1.0):
    """The rounded-square app icon, `fill` of the canvas wide, centered on a transparent canvas.
    `art` is the size of the artwork relative to the tile; above 1 the layers overflow the tile and are cropped,
    which is how Icon Composer lays the layers out on the full canvas."""
    top, bottom, names = read_source()
    body = int(round(canvas * fill))
    img = gradient(body, top, bottom)
    abody = int(round(body * art))
    fg = with_shadow(foreground(abody, names))
    off = (abody - body) // 2
    img.alpha_composite(fg.crop((off, off, off + body, off + body)))
    big = Image.new('L', (body * 2, body * 2), 0)
    ImageDraw.Draw(big).rounded_rectangle((0, 0, body * 2 - 1, body * 2 - 1), radius=int(body * 2 * radius), fill=255)
    img.putalpha(big.resize((body, body), Image.LANCZOS))
    out = Image.new('RGBA', (canvas, canvas), (0, 0, 0, 0))
    out.alpha_composite(img, ((canvas - body) // 2, (canvas - body) // 2))
    return out


def render_bleed(canvas, layer_scale):
    """Full-bleed square (no rounded corners) with the artwork scaled to stay inside a circular crop."""
    top, bottom, names = read_source()
    img = gradient(canvas, top, bottom)
    body = int(round(canvas * layer_scale))
    img.alpha_composite(with_shadow(foreground(body, names)), ((canvas - body) // 2, (canvas - body) // 2))
    return img


def ribbon(canvas, base):
    """BETA ribbon that wraps around the icon: it runs over the icon's right and bottom edges and turns round them,
    so just past the edge you see the ribbon's darker underside. Soft shadow on the icon, darker toward the edges."""
    from scipy import ndimage as ndi
    font_path = next((f for f in FONTS if os.path.exists(f)), None)
    if font_path is None:
        raise SystemExit('No bold font found for the BETA ribbon; add one to FONTS in this script.')
    k = canvas / 512.0
    s2 = 2 ** 0.5
    SL, U0, WIDTH = 0.9756, 668.3, 163.0          # slanted edges of the band
    WRAP = 11.0 * k                                # how far the ribbon turns round the edge
    M = np.array(base.getchannel('A')) > 127       # the icon itself
    dist_out = ndi.distance_transform_edt(~M)      # distance outside the icon
    dist_in = ndi.distance_transform_edt(M)        # distance inside the icon

    ys, xs = np.mgrid[0:canvas, 0:canvas]
    xf, yf = xs / k, ys / k
    upper = U0 - SL * xf
    in_band = (yf >= upper) & (yf <= upper + WIDTH)
    region = M | (dist_out <= WRAP)
    mask = in_band & region
    # round the sharp tips where the slanted edges meet the icon outline
    r = 7.0 * k
    eroded = ndi.distance_transform_edt(mask) > r
    mask = ndi.distance_transform_edt(~eroded) <= r

    front = mask & M
    wrap = mask & ~M

    # front face: red gradient across the band, darker toward the icon's outline
    u = (xs + ys) / s2 / k
    t = np.clip((u - 477.0) / (592.6 - 477.0), 0, 1)
    c0, c1 = np.array((254, 24, 40)), np.array((228, 0, 18))
    rgb = c0[None, None, :] * (1 - t[..., None]) + c1[None, None, :] * t[..., None]
    w = np.clip(1 - (dist_in / k) / 30.0, 0, 1)
    rgb[..., 0] *= 1 - 0.30 * w ** 2
    g = np.clip(((dist_in / k) - 3.0) / 30.0, 0, 1) ** 2
    rgb[..., 1] *= g
    rgb[..., 2] *= g
    # wrapped part: the underside of the ribbon, darkest at the outer rim
    q = np.clip(dist_out / WRAP, 0, 1)
    under = np.stack([170 - 55 * q, 0 * q, 12 - 6 * q], axis=-1)
    rgb = np.where(wrap[..., None], under, rgb)
    # lighter rim where the ribbon turns away (a thin specular line on the outer edge)
    rim = wrap & (dist_out >= WRAP - 1.6 * k)
    rgb = np.where(rim[..., None], np.array((205, 38, 52)), rgb)
    layer = Image.fromarray(np.clip(rgb, 0, 255).astype('uint8'), 'RGB').convert('RGBA')

    # pink highlight along the slanted upper edge, front face only
    d_edge = (yf - upper) / (1 + SL * SL) ** 0.5      # perpendicular distance from the upper slanted edge
    hl = (d_edge >= 1.9) & (d_edge <= 3.7) & front
    hl_img = Image.new('RGBA', (canvas, canvas), (255, 135, 145, 0))
    hl_img.putalpha(Image.fromarray(hl.astype('uint8') * 255).filter(ImageFilter.GaussianBlur(0.35 * k)).point(lambda a: int(a * 0.75)))
    layer = Image.alpha_composite(layer, hl_img)

    # text
    cap = 63 * k
    font = ImageFont.truetype(font_path, int(cap / 0.714))
    tw = Image.new('RGBA', (int(400 * k), int(160 * k)), (0, 0, 0, 0))
    dr = ImageDraw.Draw(tw)
    bb = dr.textbbox((0, 0), 'BETA', font=font)
    w_, h_ = bb[2] - bb[0], bb[3] - bb[1]
    tx, ty = (tw.width - w_) // 2 - bb[0], (tw.height - h_) // 2 - bb[1]
    sh = Image.new('RGBA', tw.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).text((tx + 2.5 * k, ty + 3.0 * k), 'BETA', font=font, fill=(110, 0, 10, 150))
    sh = sh.filter(ImageFilter.GaussianBlur(1.6 * k))
    dr.text((tx, ty), 'BETA', font=font, fill=(255, 255, 255, 255))
    tw = Image.alpha_composite(sh, tw)
    scale = 198 * k / w_
    if abs(scale - 1) > 0.02:
        tw = tw.resize((int(tw.width * scale), int(tw.height * scale)), Image.LANCZOS)
    tw = tw.rotate(45, expand=True, resample=Image.BICUBIC)
    cxt, cyt = (533.5 + 3.5) / s2 * k, (533.5 - 3.5) / s2 * k
    layer.alpha_composite(tw, (int(cxt - tw.width / 2), int(cyt - tw.height / 2)))

    alpha = Image.fromarray(mask.astype('uint8') * 255).filter(ImageFilter.GaussianBlur(0.4 * k))
    layer.putalpha(alpha)

    # soft shadow the ribbon casts on the icon (clipped to the icon)
    sh_a = Image.fromarray(mask.astype('uint8') * 255).filter(ImageFilter.GaussianBlur(4.0 * k))
    sh_a = ImageChops.offset(sh_a, int(2 * k), int(5 * k)).point(lambda a: int(a * 0.42))
    sh_a = ImageChops.multiply(sh_a, base.getchannel('A'))
    shadow = Image.new('RGBA', (canvas, canvas), (0, 20, 70, 0))
    shadow.putalpha(sh_a)
    return shadow, layer


def save_sizes(img, directory, name, sizes):
    for s in sizes:
        img.resize((s, s), Image.LANCZOS).save(directory / f'{name}-{s}.png', optimize=True)


def cmd_app(out):
    """Windows/Linux icons, and a fallback AppIcon.icns (macOS uses Apple's actool output, see scripts/macos)."""
    N = 4096
    icon = render_icon(N, fill=0.953, radius=0.2285, art=1 / 0.953)
    desktop = (out / 'vATIS.Desktop/Assets') if out else ROOT / 'vATIS.Desktop/Assets'
    macos = (out / 'scripts/macos') if out else ROOT / 'scripts/macos'
    desktop.mkdir(parents=True, exist_ok=True)
    macos.mkdir(parents=True, exist_ok=True)
    icon.resize((512, 512), Image.LANCZOS).save(desktop / 'MainIcon.png')
    sizes = [16, 24, 32, 48, 64, 128, 256]
    imgs = [icon.resize((s, s), Image.LANCZOS) for s in sizes]
    imgs[-1].save(desktop / 'MainIcon.ico', format='ICO', sizes=[(s, s) for s in sizes], append_images=imgs[:-1])
    # macOS template: the tile is 824 of 1024 px, with a soft shadow
    mac_icon = render_icon(N, fill=824 / 1024, radius=0.2285, art=1024 / 824)
    full = mac_icon.resize((1024, 1024), Image.LANCZOS)
    sh = Image.new('RGBA', full.size, (0, 0, 0, 0))
    sh.putalpha(full.getchannel('A').point(lambda v: int(v * 0.35)))
    sh = ImageChops.offset(sh.filter(ImageFilter.GaussianBlur(12)), 0, 10)
    Image.alpha_composite(sh, full).save(macos / 'AppIcon.icns', format='ICNS',
                                         sizes=[(s, s) for s in (16, 32, 64, 128, 256, 512, 1024)])
    print('wrote', desktop, macos)


def cmd_web(out):
    """Website favicon and the landing page logo with the BETA ribbon."""
    out.mkdir(parents=True, exist_ok=True)
    N = 2048
    base = render_icon(N, fill=0.97)
    beta = render_icon(N, fill=0.935)          # a little smaller so the ribbon has room to turn round the edge
    shadow, rib = ribbon(N, beta)
    beta.alpha_composite(shadow)
    beta.alpha_composite(rib)
    save_sizes(base, out, 'vATIS-icon', (512, 1024))
    save_sizes(beta, out, 'vATIS-icon-beta', (512, 1024))
    print('wrote', out, '(website uses vATIS-icon-512.png as assets/icon.png and vATIS-icon-beta-512.png as assets/icon_beta.png)')


def cmd_social(out):
    """Discord server icon (cropped to a circle by Discord) and GitHub avatar (square)."""
    out.mkdir(parents=True, exist_ok=True)
    N = 2048
    discord = render_bleed(N, 0.80)
    discord.resize((512, 512), Image.LANCZOS).save(out / 'vatis-discord-icon-512.png', optimize=True)
    discord.resize((1024, 1024), Image.LANCZOS).save(out / 'vatis-discord-icon-1024.png', optimize=True)
    render_bleed(N, 0.80).resize((1024, 1024), Image.LANCZOS).save(out / 'vatis-github-avatar-1024.png', optimize=True)
    print('wrote', out)


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('what', choices=['app', 'web', 'social', 'all'])
    p.add_argument('--out', type=Path, help='output directory (default: icon-out; the app command writes in place)')
    a = p.parse_args()
    out = a.out
    if a.what in ('app', 'all'):
        cmd_app(out if a.what == 'all' or a.out else None)
    if a.what in ('web', 'all'):
        cmd_web((out or Path('icon-out')) / 'web' if a.what == 'all' else (out or Path('icon-out')))
    if a.what in ('social', 'all'):
        cmd_social((out or Path('icon-out')) / 'social' if a.what == 'all' else (out or Path('icon-out')))


if __name__ == '__main__':
    main()

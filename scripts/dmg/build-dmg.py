#!/usr/bin/env python3
"""Builds a compressed DMG from an .app bundle without macOS. Runs inside the scripts/dmg Docker image.

    build-dmg.py <app> <background.tiff> <volume icon.icns> <volume name> <output.dmg>

The volume is an HFS+ image created with mkfs.hfsplus and filled with libdmg-hfsplus's hfsplus tool, then
converted to a zlib-compressed UDIF image with its dmg tool. Finder reads the window size, icon positions and
background from the .DS_Store written here, which is how dmgbuild and create-dmg lay out a DMG on macOS.
"""
import os
import subprocess
import sys
import tempfile
import datetime
from pathlib import Path

from ds_store import DSStore
from mac_alias import Alias
from mac_alias.alias import ALIAS_FIXED_DISK, ALIAS_KIND_FILE, TargetInfo, VolumeInfo

# Window content size and icon centres; the arrow in dmg-background.tiff points from one to the other.
WINDOW_POS = (200, 120)
WINDOW_SIZE = (660, 400)
ICON_SIZE = 128
APP_POS = (195, 170)
APPLICATIONS_POS = (475, 170)


def run(*args):
    subprocess.run([str(a) for a in args], check=True, stdout=subprocess.DEVNULL)


def background_alias(volume, filename):
    """An alias to <volume>:.background:<filename>. Finder resolves it by volume name and path, so the
    catalog ids (not knowable without mounting the image) are placeholders."""
    a = Alias()
    now = datetime.datetime.now(datetime.timezone.utc)
    a.volume = VolumeInfo(volume.encode(), now, b"H+", ALIAS_FIXED_DISK, 0, b"\0\0")
    a.target = TargetInfo(ALIAS_KIND_FILE, filename.encode(), 16, 17, now, b"\0\0\0\0", b"\0\0\0\0")
    a.target.folder_name = b".background"
    a.target.carbon_path = volume.encode() + b":.background:\0" + filename.encode()
    a.target.cnid_path = [16]
    a.volume.posix_path = b"/Volumes/" + volume.encode()
    a.target.posix_path = b"/.background/" + filename.encode()
    return a.to_bytes()


def write_ds_store(path, app_name, volume, background_name):
    x, y = WINDOW_POS
    w, h = WINDOW_SIZE
    # The same keys and value types dmgbuild writes (bwsp = window settings, icvp = icon view, icvl = view type).
    bwsp = {
        "ShowStatusBar": False, "WindowBounds": f"{{{{{x}, {y}}}, {{{w}, {h}}}}}", "ContainerShowSidebar": False,
        "PreviewPaneVisibility": False, "SidebarWidth": 180, "ShowTabView": False, "ShowToolbar": False,
        "ShowPathbar": False, "ShowSidebar": False,
    }
    icvp = {
        "viewOptionsVersion": 1, "backgroundType": 2, "backgroundImageAlias": background_alias(volume, background_name),
        "backgroundColorRed": 1.0, "backgroundColorGreen": 1.0, "backgroundColorBlue": 1.0,
        "gridOffsetX": 0.0, "gridOffsetY": 0.0, "gridSpacing": 100.0, "arrangeBy": "none",
        "showIconPreview": False, "showItemInfo": False, "labelOnBottom": True, "textSize": 12.0,
        "iconSize": float(ICON_SIZE), "scrollPositionX": 0.0, "scrollPositionY": 0.0,
    }
    with DSStore.open(str(path), "w+") as d:
        d["."]["vSrn"] = ("long", 1)
        d["."]["bwsp"] = bwsp
        d["."]["icvp"] = icvp
        d["."]["icvl"] = (b"type", b"icnv")
        d[app_name]["Iloc"] = APP_POS
        d["Applications"]["Iloc"] = APPLICATIONS_POS


def open_on_mount(image):
    """Makes Finder open the volume's window when the DMG mounts: what `bless --openfolder` does on macOS.
    It sets Finder info word 2 in the HFS+ volume header (and its backup copy in the last 1024 bytes but one
    sector) to the root folder's id, 2."""
    size = os.path.getsize(image)
    with open(image, "r+b") as f:
        for header in (1024, size - 1024):
            f.seek(header)
            assert f.read(2) in (b"H+", b"HX"), "not an HFS+ volume header"
            f.seek(header + 80 + 2 * 4)  # finderInfo starts at byte 80 of the header; each word is 4 bytes
            f.write((2).to_bytes(4, "big"))


def set_custom_icon_flag(image):
    """Sets kHasCustomIcon (0x0400) on the root folder's Finder flags, so Finder uses /.VolumeIcon.icns: what
    `SetFile -a C` does on macOS. Finds the root folder record (folder id 2) in the catalog B-tree."""
    be = lambda b: int.from_bytes(b, "big")
    with open(image, "r+b") as f:
        f.seek(1024)
        header = f.read(512)
        assert header[:2] in (b"H+", b"HX"), "not an HFS+ volume header"
        block_size = be(header[40:44])
        extents = [(be(header[288 + i * 8:292 + i * 8]), be(header[292 + i * 8:296 + i * 8])) for i in range(8)]
        extents = [(start * block_size, count * block_size) for start, count in extents if count]
        catalog = bytearray()
        for offset, length in extents:
            f.seek(offset)
            catalog += f.read(length)
        node_size = be(catalog[32:34])
        node = be(catalog[24:28])  # first leaf node
        while node:
            base = node * node_size
            next_node, kind, count = be(catalog[base:base + 4]), catalog[base + 8], be(catalog[base + 10:base + 12])
            assert kind == 0xFF, "expected a leaf node"
            for i in range(count):
                rec = base + be(catalog[base + node_size - 2 * (i + 1):base + node_size - 2 * i])
                data = rec + 2 + be(catalog[rec:rec + 2])
                data += (data - rec) % 2  # the key is padded so the record data is 2-byte aligned
                if be(catalog[data:data + 2]) == 1 and be(catalog[data + 8:data + 12]) == 2:  # folder record, id 2
                    flags = be(catalog[data + 56:data + 58]) | 0x0400
                    catalog[data + 56:data + 58] = flags.to_bytes(2, "big")
                    pos = 0
                    for offset, length in extents:
                        f.seek(offset)
                        f.write(catalog[pos:pos + length])
                        pos += length
                    return
            node = next_node
    sys.exit("root folder record not found in the catalog")


def main():
    app, background, volume_icon, volume, output = (Path(a) for a in sys.argv[1:6])
    volume = str(volume)
    work = Path(tempfile.mkdtemp())
    image = work / "volume.img"

    size_mb = int(subprocess.check_output(["du", "-sm", str(app)]).split()[0])
    with open(image, "wb") as f:
        f.truncate((size_mb * 12 // 10 + 16) * 1024 * 1024)
    run("mkfs.hfsplus", "-v", volume, image)

    background_name = "background.tiff"
    ds_store = work / ".DS_Store"
    write_ds_store(ds_store, app.name, volume, background_name)

    run("hfsplus", image, "mkdir", ".background")
    run("hfsplus", image, "add", background, f".background/{background_name}")
    run("hfsplus", image, "mkdir", f"/{app.name}")
    run("hfsplus", image, "addall", app, f"/{app.name}")
    # addall drops the executable bit.
    for f in app.rglob("*"):
        if f.is_file() and not f.is_symlink() and os.access(f, os.X_OK):
            run("hfsplus", image, "chmod", "755", f"/{app.name}/{f.relative_to(app).as_posix()}")
    # addall follows symlinks and copies their targets, which would change the bundle and invalidate its code
    # signature (the seal records which entries are symlinks). Put each symlink back.
    for f in sorted(app.rglob("*")):
        if f.is_symlink():
            path = f"/{app.name}/{f.relative_to(app).as_posix()}"
            run("hfsplus", image, "rm", path)
            run("hfsplus", image, "symlink", path, os.readlink(f))
    run("hfsplus", image, "symlink", "Applications", "/Applications")
    run("hfsplus", image, "add", ds_store, ".DS_Store")
    run("hfsplus", image, "add", volume_icon, ".VolumeIcon.icns")
    set_custom_icon_flag(image)

    open_on_mount(image)
    run("dmg", "build", image, output)
    print(f"wrote {output}")


if __name__ == "__main__":
    main()

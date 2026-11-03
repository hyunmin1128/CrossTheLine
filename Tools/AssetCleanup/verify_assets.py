"""Read-only checks against the pre-cleanup ZIP and the GUID-preserving move manifest."""
from pathlib import Path
import csv
import hashlib
import json
import re
import zipfile

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "Docs/QA/AssetCleanup"
GUID = re.compile(rb"guid:\s*([0-9a-fA-F]{32})")


def main():
    errors, checks = [], []
    with zipfile.ZipFile(ROOT / "Docs/Backups/AssetCleanup-20261028/Assets-before.zip") as backup:
        names = {name.replace("\\", "/"): name for name in backup.namelist()}
        moves = list(csv.DictReader((QA / "moves.tsv").open(encoding="utf-8-sig"), delimiter="\t"))
        for move in moves:
            target = ROOT / move["target"]
            meta = Path(str(target) + ".meta")
            if not target.is_file() or not meta.is_file():
                errors.append(f"Missing destination or meta: {target}")
                continue
            match = GUID.search(meta.read_bytes())
            if not match or match[1].decode() != move["guid"]:
                errors.append(f"Changed GUID: {target}")
            original = move["source"].removeprefix("Assets/")
            # Code changes and scene reference changes are intentional; image/audio bytes are not.
            if target.suffix.lower() in {".png", ".wav", ".aseprite", ".anim", ".mat", ".shader"}:
                if original in names and backup.read(names[original]) != target.read_bytes():
                    errors.append(f"Changed asset bytes: {target}")
        checks.append(f"Validated {len(moves)} moves and stable GUIDs")
        aseprite = [name for name in names if name.lower().endswith(".aseprite")]
        for name in aseprite:
            current = ROOT / "Assets" / name
            if not current.exists() or current.read_bytes() != backup.read(names[name]):
                errors.append(f"User Aseprite source changed: {name}")
        checks.append(f"Preserved all {len(aseprite)} original Aseprite files byte-for-byte")
        # Every original user sprite outside generated Art/Resources stays unchanged.
        sprites = [name for name in names if name.startswith("Sprite/") and name.lower().endswith((".png", ".aseprite"))]
        for name in sprites:
            current = ROOT / "Assets" / name
            if not current.exists() or current.read_bytes() != backup.read(names[name]):
                errors.append(f"Original user sprite changed: {name}")
        checks.append(f"Preserved {len(sprites)} user sprite/source files byte-for-byte")
    for folder in ("Art", "Resources", "Editor"):
        if (ROOT / "Assets" / folder).exists():
            errors.append(f"Old folder still exists: Assets/{folder}")
    duplicates = {}
    for meta in (ROOT / "Assets").rglob("*.meta"):
        match = GUID.search(meta.read_bytes())
        if match:
            duplicates.setdefault(match[1].decode(), []).append(str(meta.relative_to(ROOT)))
    for guid, paths in duplicates.items():
        if len(paths) > 1:
            errors.append(f"Duplicate GUID {guid}: {paths}")
    checks.append("Checked duplicate GUIDs and removed legacy folder names")
    result = {"success": not errors, "checks": checks, "errors": errors}
    (QA / "filesystem-validation.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))
    raise SystemExit(0 if not errors else 1)


if __name__ == "__main__":
    main()

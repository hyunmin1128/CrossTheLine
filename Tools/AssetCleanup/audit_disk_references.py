from pathlib import Path
import re, json, zipfile

root = Path(__file__).resolve().parents[2]
qa = root / 'Docs/QA/AssetCleanup'
pattern = re.compile(rb'guid:\s*([0-9a-fA-F]{32})')
known = set()
for folder in [root/'Assets', root/'Library/PackageCache']:
    for p in folder.rglob('*.meta'):
        m = pattern.search(p.read_bytes())
        if m:
            known.add(m[1].decode().lower())
missing = {}
for p in (root/'Assets').rglob('*'):
    if p.suffix not in ['.unity','.prefab','.asset','.mat','.anim','.controller']:
        continue
    for match in pattern.finditer(p.read_bytes()):
        guid = match[1].decode().lower()
        if guid not in known and not guid.startswith('0000000000000000'):
            missing.setdefault(guid,set()).add(str(p.relative_to(root)))
with zipfile.ZipFile(root/'Docs/Backups/AssetCleanup-20261028/Assets-before.zip') as z:
    old_known = {m[1].decode().lower() for n in z.namelist() if n.endswith('.meta') for m in [pattern.search(z.read(n))] if m}
    status = {g:{'present_in_backup':g in old_known,'references':sorted(paths)} for g,paths in missing.items()}
(qa/'disk-reference-audit.json').write_text(json.dumps(status,indent=2),encoding='utf-8')
print(json.dumps({'missing_disk_guids':len(missing),'present_in_backup':sum(g in old_known for g in missing),'examples':list(missing)[:10]},indent=2))

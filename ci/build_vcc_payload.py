"""Build the VCC installer payload from the canonical Assets tree (stdlib only)."""
import hashlib
import json
from pathlib import Path
import re
import zipfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/MetaRea_World_GUIDE"
PACKAGE = ROOT / "Packages/net.metarea.worldguide"
OUTPUT = PACKAGE / "Payload~"


def build():
    version = json.loads((PACKAGE / "package.json").read_text(encoding="utf-8-sig"))["version"]
    setup = (SOURCE / "Editor/MetaReaOnlineSetup.cs").read_text(encoding="utf-8-sig")
    if f'GuideVersion = "{version}"' not in setup:
        raise ValueError("GuideVersion does not match package.json")
    files = sorted(p for p in SOURCE.rglob("*") if p.is_file())
    records = []
    guids = set()
    for path in files:
        relative = path.relative_to(SOURCE).as_posix()
        if path.suffix.lower() == ".unity" or "/Scenes/" in "/" + relative:
            raise ValueError(f"Scene files are not distributable: {relative}")
        if path.suffix != ".meta" and not path.with_name(path.name + ".meta").is_file():
            raise ValueError(f"Missing meta: {relative}")
        if path.suffix == ".meta":
            match = re.search(r"^guid: ([a-f0-9]{32})$", path.read_text(encoding="utf-8-sig"), re.M)
            if not match or match[1] in guids:
                raise ValueError(f"Missing or duplicate GUID: {relative}")
            guids.add(match[1])
        records.append({"path": relative, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    OUTPUT.mkdir(exist_ok=True)
    payload = OUTPUT / "files.zip"
    with zipfile.ZipFile(payload, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path, record in zip(files, records):
            info = zipfile.ZipInfo(record["path"], date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, path.read_bytes())
    manifest = {"version": version, "payloadSha256": hashlib.sha256(payload.read_bytes()).hexdigest(), "files": records}
    (OUTPUT / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"version": version, "files": len(records), "sha256": manifest["payloadSha256"]}))


if __name__ == "__main__":
    build()

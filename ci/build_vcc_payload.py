"""Validate package sources and stage an atomic VCC bootstrap distribution."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import zipfile

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / 'Packages/net.metarea.worldguide'
CORE = PACKAGE / 'Core'
USER = ROOT / 'Assets/MetaRea_World_GUIDE'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def deterministic_zip(path, entries):
    with zipfile.ZipFile(path, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name, source in sorted(entries):
            info = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, source.read_bytes())

def validate():
    package = json.loads((PACKAGE/'package.json').read_text(encoding='utf-8-sig'))
    version = package['version']
    if f'GuideVersion = "{version}"' not in (CORE/'Editor/MetaReaOnlineSetup.cs').read_text(encoding='utf-8-sig'):
        raise ValueError('Client and VCC versions differ')
    if package['vpmDependencies'].get('com.vrchat.worlds') != '>=3.10.5':
        raise ValueError('SDK dependency must have a minimum only')
    files = sorted(p for p in CORE.rglob('*') if p.is_file()) + [CORE.with_suffix('.meta'), USER/'README.txt', USER/'README.txt.meta']
    seen = {}
    references = set()
    sdk = json.loads((ROOT/'ci/sdk-reference-guids.json').read_text(encoding='utf-8-sig'))
    if any(not path.startswith(('Packages/com.vrchat.', 'Packages/com.unity.ugui/')) for path in sdk.values()):
        raise ValueError('External-reference allowlist contains a non-SDK reference')
    for path in files:
        relative = path.relative_to(ROOT).as_posix()
        if not path.is_file() or path.is_symlink():
            raise ValueError('Missing or linked source: '+relative)
        if path.suffix.lower() in ('.unity','.dll','.exe') or '/Scenes/' in relative or '/UserContent/' in relative:
            raise ValueError('Scene, binary or user content in distribution: '+relative)
        if path.suffix != '.meta' and not path.with_name(path.name+'.meta').is_file():
            raise ValueError('Missing meta: '+relative)
        if path.suffix == '.meta':
            match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(encoding='utf-8-sig'), re.M)
            if not match or match[1] in seen:
                raise ValueError('Missing or duplicate GUID: '+relative)
            seen[match[1]] = relative
        if path.suffix in ('.prefab','.asset','.mat','.meta','.asmdef'):
            try: references.update(re.findall(r'guid: ([0-9a-f]{32})',path.read_text(encoding='utf-8-sig')))
            except UnicodeError: pass
    builtin = {'0000000000000000e000000000000000','0000000000000000f000000000000000'}
    unexpected = references-seen.keys()-sdk.keys()-builtin
    if unexpected:
        raise ValueError('Unapproved external GUID references: '+','.join(sorted(unexpected)))
    legacy = json.loads((ROOT/'ci/legacy-assets-0.12.0.json').read_text(encoding='utf-8-sig'))
    old_guids = {}
    # Core assets retain their GUID; entry variant remains outside the payload.
    entry=(USER/'MetaRea World GUIDE.prefab').read_text(encoding='utf-8-sig')
    source_guid=re.search(r'^guid: ([0-9a-f]{32})$',(CORE/'Internal/MetaRea World GUIDE.prefab.meta').read_text(),re.M)[1]
    if source_guid not in entry: raise ValueError('User variant no longer refers to package base')
    return version, files, legacy['files'], len(seen), len(references)

def build(output):
    version, files, legacy, guids, refs = validate()
    output = Path(output).resolve()
    if output.exists(): raise ValueError('Choose a new output folder; existing artifacts are not overwritten')
    output.mkdir(parents=True)
    stage=output/'package';stage.mkdir()
    # Only the independent bootstrap is initially importable by Unity. Core is
    # exposed by its transaction after legacy Assets GUIDs are removed.
    for name in ('Editor','Editor.meta','package.json','package.json.meta','README.md','README.md.meta'):
        source=PACKAGE/name;target=stage/name
        if source.is_dir():shutil.copytree(source,target)
        else:shutil.copy2(source,target)
    payload=stage/'Payload~';payload.mkdir()
    records=[{'path':p.relative_to(ROOT).as_posix(),'sha256':digest(p)} for p in files]
    deterministic_zip(payload/'files.zip',[(record['path'],path) for record,path in zip(records,files)])
    manifest={'layout':2,'version':version,'payloadSha256':digest(payload/'files.zip'),'files':records,'legacyFiles':legacy}
    (payload/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    archive=output/f'net.metarea.worldguide-{version}.zip'
    deterministic_zip(archive,[(p.relative_to(stage).as_posix(),p) for p in stage.rglob('*') if p.is_file()])
    report={'version':version,'layout':2,'payloadFiles':len(files),'uniqueGuids':guids,'checkedReferences':refs,'zipSha256':digest(archive),'zipBytes':archive.stat().st_size,'published':False,'runtimeRoot':'Packages/net.metarea.worldguide/Core','userRoot':'Assets/MetaRea_World_GUIDE'}
    (output/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False))

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--output',required=True)
    args=parser.parse_args();build(args.output)

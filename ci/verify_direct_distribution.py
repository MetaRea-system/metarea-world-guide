"""Verify the actual release archive, including GUID continuity with old releases."""
from pathlib import Path
import hashlib,io,json,re,sys,zipfile

archive=Path(sys.argv[1]); old_archives=[Path(p) for p in sys.argv[2:]]
checks=[]
with zipfile.ZipFile(archive) as z:
    names=set(z.namelist())
    manifest=json.loads(z.read('Payload~/manifest.json'))
    package=json.loads(z.read('package.json'))
    assert manifest['layout']==3 and manifest['version']==package['version']
    assert 'Payload~/files.zip' not in names
    assert not any('/UserContent/' in n or n.endswith('.unity') for n in names)
    checks.append('full runtime present without delayed extraction or user content')
    current={}
    for item in manifest['files']:
        path=item['path']
        local=path.removeprefix('Packages/net.metarea.worldguide/') if path.startswith('Packages/') else 'Documentation~/'+Path(path).name
        data=z.read(local)
        assert hashlib.sha256(data).hexdigest()==item['sha256'],path
        current[path]=data
    checks.append(f"all {len(current)} manifest hashes match archive files")
    source='Packages/net.metarea.worldguide/Core/Internal/MetaRea World GUIDE.prefab'
    assert source in current and source+'.meta' in current
    for old in old_archives:
        with zipfile.ZipFile(old) as outer:
            with zipfile.ZipFile(io.BytesIO(outer.read('Payload~/files.zip'))) as previous:
                total=0
                for path,data in current.items():
                    if not path.endswith('.meta') or path not in previous.namelist():continue
                    old_guid=re.search(rb'^guid: ([0-9a-f]{32})',previous.read(path),re.M)
                    new_guid=re.search(rb'^guid: ([0-9a-f]{32})',data,re.M)
                    assert old_guid and new_guid and old_guid[1]==new_guid[1],path
                    total+=1
                checks.append(f'{old.name}: {total} stable GUIDs including source prefab')
print(json.dumps({'version':package['version'],'checks':checks,'sha256':hashlib.sha256(archive.read_bytes()).hexdigest()},ensure_ascii=False))

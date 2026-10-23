"""Split item bundles in the manual UGF collection; dry-run unless --apply.

Uses serialized GUID references conservatively. Unity's next build report remains
the authority for imported dependencies (especially models and sprite atlases).
"""
import argparse
import collections
import csv
import io
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
COLLECTION = ROOT / 'Assets/Plugins/UnityGameFramework/Configs/ResourceCollection.xml'
GUID = re.compile(r'guid: ([0-9a-f]{32})')
ITEM_DIR = 'Assets/ArrowGame/Prefabs/GameObject/'
TEXT_EXTENSIONS = {'.prefab', '.asset', '.mat', '.unity', '.controller', '.overridecontroller', '.anim', '.spriteatlas'}
DEPENDENCY_EXTENSIONS = {'.mat', '.asset', '.png', '.jpg', '.jpeg', '.tga', '.psd', '.fbx', '.obj', '.physicmaterial'}


def family(path):
    if not path.startswith(ITEM_DIR) or not path.endswith('.prefab'):
        return None
    name = Path(path).stem.lower()
    for prefix, bundle in [('box_', 'obj_box'), ('can_', 'obj_can'),
                           ('cansquare_', 'obj_can'), ('canconical_', 'obj_can'),
                           ('stone_', 'obj_stone'), ('stonecircle_', 'obj_stone'),
                           ('ice_', 'obj_ice'), ('icecircle_', 'obj_ice'),
                           ('jamjar_', 'obj_jamjar'), ('tnt', 'obj_tnt'),
                           ('wormhole', 'obj_wormhole'), ('rocket', 'booster_rocket')]:
        if name.startswith(prefix):
            return bundle
    if name in {'table', 'blocker', 'cannonball', 'cannonballhitobjectparticle',
                'grasshitparticle', 'objectgrasshitparticle'}:
        return 'gameplay_core'
    return None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    original = COLLECTION.read_bytes()
    xml = original.decode('utf-8-sig')
    root = ET.fromstring(xml)
    assignments = {a.attrib['Guid']: a.attrib['ResourceName'] for a in root.findall('./ResourceCollection/Assets/Asset')}
    assert len(assignments) == len(root.findall('./ResourceCollection/Assets/Asset')), 'Duplicate GUID assignment'
    paths, guids, metadata = {}, {}, {}
    for meta in (ROOT / 'Assets').rglob('*.meta'):
        content = meta.read_text(encoding='utf-8-sig', errors='replace')
        match = re.search(r'^guid: ([0-9a-f]{32})', content, re.M)
        if match:
            guid = match[1]
            path = meta.relative_to(ROOT).as_posix()[:-5]
            paths[guid], guids[path], metadata[guid] = path, guid, content
    edges = {}

    def dependencies(guid):
        if guid not in edges:
            path = paths.get(guid)
            refs = set(GUID.findall(metadata.get(guid, '')))
            if path and Path(path).suffix.lower() in TEXT_EXTENSIONS:
                refs.update(GUID.findall((ROOT / path).read_text(encoding='utf-8-sig', errors='replace')))
            edges[guid] = refs - {guid}
        return edges[guid]

    def closure(start):
        seen, pending = set(), list(start)
        while pending:
            guid = pending.pop()
            if guid in seen or guid not in paths:
                continue
            seen.add(guid)
            pending.extend(dependencies(guid) - seen)
        return seen

    items = {g: family(p) for g, p in paths.items() if family(p)}
    # Aggregate catalog references intentionally do not determine ownership.
    catalogs = {'Assets/ArrowGame/ScriptableAssets/Flow/ObjectData.asset',
                'Assets/ArrowGame/ScriptableAssets/Flow/ParticleData.asset'}
    # Preserve all non-item prefab/scene/catalog consumers, including UI, and
    # sprite atlas membership. Individual texture/material registrations are
    # not consumers: these are exactly the assets being classified.
    item_bundles = set(items.values()) | {'gameplay_shared'}
    protected_roots = [g for g in assignments if g not in items
                       and assignments[g] not in item_bundles
                       and paths.get(g) not in catalogs
                       and Path(paths.get(g, '')).suffix.lower() in
                       {'.prefab', '.unity', '.asset', '.spriteatlas'}]
    protected_roots += [g for g, p in paths.items() if p.endswith('.spriteatlas')]
    protected = closure(protected_roots)
    owners = collections.defaultdict(set)
    for guid, bundle in items.items():
        for dep in closure([guid]):
            owners[dep].add(bundle)
    desired = dict(items)
    for guid, bundles in owners.items():
        path = paths[guid]
        if guid in items or guid in protected:
            continue
        if not path.startswith(('Assets/ArrowGame/Gameplay/', 'Assets/ArrowGame/Sprites/Gameplay/')):
            continue
        if Path(path).suffix.lower() not in DEPENDENCY_EXTENSIONS:
            continue
        desired[guid] = next(iter(bundles)) if len(bundles) == 1 else 'gameplay_shared'
    changes = {g: b for g, b in desired.items() if assignments.get(g) != b}
    for guid, bundle in changes.items():
        if guid in assignments:
            pattern = r'(<Asset Guid="' + guid + r'" ResourceName=")[^"]+("\s*/>)'
            xml, count = re.subn(pattern, lambda m: m[1] + bundle + m[2], xml)
            assert count == 1, guid
    newline = '\r\n' if b'\r\n' in original else '\n'
    additions = ''.join(f'      <Asset Guid="{g}" ResourceName="{changes[g]}" />{newline}'
                        for g in sorted(changes) if g not in assignments)
    xml = xml.replace('    </Assets>', additions + '    </Assets>')
    resource_names = {r.attrib['Name'] for r in root.findall('./ResourceCollection/Resources/Resource')}
    new_resources = ''.join(f'      <Resource Name="{b}" LoadType="3" Packed="True" />{newline}'
                            for b in sorted(set(desired.values()) - resource_names))
    xml = xml.replace('    </Resources>', new_resources + '    </Resources>')
    updated = ET.fromstring(xml)
    result = {a.attrib['Guid']: a.attrib['ResourceName'] for a in updated.findall('./ResourceCollection/Assets/Asset')}
    assert len(result) == len(updated.findall('./ResourceCollection/Assets/Asset'))
    assert all(result[g] == b for g, b in desired.items())
    assert all(result[g] == b for g, b in assignments.items() if g not in changes)
    assert all(result[g] == assignments[g] for g in assignments
               if paths.get(g, '').startswith('Assets/ArrowGame/Prefabs/UI/'))
    for bundle, count in sorted(collections.Counter(desired.values()).items()):
        print(f'{bundle}: {count} assets')
    print(f'Changed assignments: {len(changes)}; protected dependencies retain their existing ownership.')
    if args.apply:
        if changes:
            COLLECTION.write_bytes(xml.encode('utf-8'))
        output = io.StringIO(newline='')
        writer = csv.writer(output)
        writer.writerow(['Bundle', 'Asset'])
        writer.writerows(sorted((bundle, paths[guid]) for guid, bundle in desired.items()))
        (ROOT / 'Docs/item-bundle-assets.csv').write_text(output.getvalue(), encoding='utf-8-sig')
        print('Applied collection and wrote Docs/item-bundle-assets.csv')


if __name__ == '__main__':
    main()

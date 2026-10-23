"""Validate every shipped level against the address-only gameplay catalogs."""
import collections
import json
from pathlib import Path
import re
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
FLOW = ROOT / 'Assets/ArrowGame/ScriptableAssets/Flow'
SCRIPTS = ROOT / 'Assets/ArrowGame/Scripts/Flow/Gameplay'


class GameplayCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.objects = (FLOW / 'ObjectData.asset').read_text(encoding='utf-8-sig')
        cls.particles = (FLOW / 'ParticleData.asset').read_text(encoding='utf-8-sig')
        rows = re.findall(r'- type: (\d+)\s+size:\s+x: ([\d.]+)\s+y: ([\d.]+)\s+z: ([\d.]+)\s+prefabPath: (.+)', cls.objects)
        cls.keys = [(int(t), float(x), float(y), float(z)) for t, x, y, z, path in rows]
        cls.rows = rows
        cls.effects = {int(t): path for t, path in re.findall(r'- type: (\d+)\s+prefabPath: (.+)', cls.particles)}
        cls.enum = dict((name, int(value)) for name, value in re.findall(r'(\w+) = (\d+)', (SCRIPTS / 'Particles/ParticleType.cs').read_text()))

    def test_no_serialized_prefab_references(self):
        for text in [self.objects, self.particles]:
            self.assertNotRegex(text, r'(prefab|tablePrefab|blockerPrefab):')
        self.assertEqual(len(self.keys), len(set(self.keys)))
        self.assertGreater(len(self.keys), 0)

    def test_all_addresses_exist_and_are_registered_in_ab(self):
        xml = ET.parse(ROOT / 'Assets/Plugins/UnityGameFramework/Configs/ResourceCollection.xml')
        assigned = {a.attrib['Guid'] for a in xml.findall('./ResourceCollection/Assets/Asset')}
        paths = re.findall(r'(?:prefabPath|tablePrefabPath|blockerPrefabPath): (.+)', self.objects + '\n' + self.particles)
        for path in paths:
            with self.subTest(path=path):
                self.assertTrue((ROOT / path).is_file())
                guid = re.search(r'^guid: (\w+)', (ROOT / (path + '.meta')).read_text(), re.M)[1]
                self.assertIn(guid, assigned)

    def test_all_shipped_levels_and_all_stages_have_prefabs(self):
        levels = list((ROOT / 'Assets/ArrowGame/levels/prod-14').glob('Level*.json'))
        self.assertGreaterEqual(len(levels), 1000)
        keys = set(self.keys)
        for file in levels:
            level = json.loads(file.read_text(encoding='utf-8-sig'))
            for stage in level.get('stages', []):
                for item in stage.get('objects', []) or []:
                    size = item['size']
                    with self.subTest(level=file.name, type=item['type'], size=size):
                        self.assertIn((item['type'], size['x'], size['y'], size['z']), keys)

    def test_every_runtime_particle_mapping_has_an_address(self):
        names = set()
        for path in [SCRIPTS / 'Objects/ObjectParticleMaps.cs', SCRIPTS / 'Objects/BreakableObject.cs',
                     SCRIPTS / 'Objects/WormholeObject.cs', SCRIPTS / 'Objects/TntObject.cs',
                     SCRIPTS / 'Level/GameplayAssetCache.cs']:
            names.update(re.findall(r'ParticleType\.(\w+)', path.read_text(encoding='utf-8-sig')))
        for name in names:
            with self.subTest(particle=name):
                self.assertIn(self.enum[name], self.effects)

    def test_catalogs_cover_distinct_resource_families(self):
        families = {Path(row[-1]).stem.split('_')[0] for row in self.rows}
        self.assertTrue({'Box', 'Can', 'Ice', 'Stone', 'JamJar', 'Tnt', 'Wormhole'} <= families)


if __name__ == '__main__':
    unittest.main()

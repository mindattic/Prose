// One-shot script: resolve xref cross-type name conflicts by renaming the loser.
// For each (winner, loser) pair the rename applies to the field the Xref indexer uses:
//   character/synthetic/place/faction/entertainment/pharmaceutical → `name` + aliases
//   vocabulary → `term`
//   contract   → `codename`
//   cyberware/material → `product_name` if set, else `name`
const fs = require('fs');
const path = 'engine/data';

const dirs = {
  character: 'people',
  synthetic: 'synthetics',
  place: 'places',
  faction: 'factions',
  vocabulary: 'vocabulary',
  entertainment: 'entertainment',
  cyberware: 'cyberware',
  pharmaceutical: 'pharmaceuticals',
  material: 'materials',
  contract: 'contracts',
};

// winner-type, winner-id, loser-type, loser-id, loser-new-name, rationale
const decisions = [
  ['Specter',        'character',      '019d6143a6dd7de69343150d0a09b4b4', 'synthetic',      '019d6143aa23797c900950c9065ac2fd', 'Specter-Wraith',           'Character has proper name; synth gets distinguishing suffix'],
  ['Candlewick',     'place',          '2ddabc621f93524f53b3e0d84489486e', 'synthetic',      '019d6143a9f0719faba064f28f266bc4', 'Wickfire',                 'Place is a named location; synth renamed to adjacent motif'],
  ['Kindling',       'place',          '65be16c3138b5d3c3c991e65d84143c4', 'synthetic',      '019d6143aa0875dfa44b833f8496ce67', 'Tinderveil',               'Place is geographic; synth renamed to related fire-motif'],
  ['Ridgeline',      'place',          '7b5d38c9c4faf84177f7156002cf6010', 'synthetic',      '019d6143aa1b7864b657936050eeab74', 'Ridgewalker',              'Place is geographic; synth becomes walker-variant'],
  ['Switchback',     'place',          'c5134e1495795dfe46568112c937ede7', 'synthetic',      '019d6143aa257268b2437229db4d062a', 'Loopback',                 'Place is geographic; synth renamed to routing term'],
  ['Undertow(syn)',  'place',          'd15fda1fdf2c43e3d8d96ba4f06e86e8', 'synthetic',      '019d7606a0097c2cab6b9dcca96b691d', 'Backwash',                 'Place is geographic; synth gets related water-motif'],
  ['The Filament',   'place',          'aeaceaa878a79342301d3b4cbd78a9dc', 'faction',        '019d6143a89070a19c9bb784793f481b', 'The Filament Cartel',      'Place holds canonical name; faction specifies with Cartel'],
  ['The Undertow(f)','place',          '019d6143a9527863ad656674299d38ba', 'faction',        'c7be3b067eabab1cc7565d18e7077434', 'The Undertow Crew',        'Place first; faction becomes the Crew'],
  ['The Threshold',  'place',          'ac9746806c13a9130830bfc66b451fc4', 'faction',        '781e4c1cef4357cbf1a32338d46bb79a', 'The Threshold Pact',       'Place first; faction becomes the Pact'],
  ['The Quiet Room', 'place',          '951e752307ac519e2ef0cfd84d098473', 'faction',        'c826026a3df42ef40baef989edea0f57', 'The Hushed Circle',        'Place is literally a room; faction renamed'],
  ['Cradle',         'synthetic',      '019d6143a9f67a8891b624f496cbd2b6', 'vocabulary',     '019d6143aa9974709e3360c85cf9b8e9', 'cradle-talk',              'Synth entity has identity; vocab becomes specific slang'],
  ['Crucible',       'synthetic',      '019d6143a9f778c49b90326952fc0b4f', 'vocabulary',     '019d6143aa9a7ecb9eebfa0311a4f25b', 'crucible-run',             'Synth entity; vocab becomes action phrase'],
  ['Drift',          'place',          '40765db54d6b29bd1c455677422142f4', 'vocabulary',     '019d6143aaa17cec8b36549dcfb312fe', 'drifting',                 'Place name; vocab becomes gerund'],
  ['Glitch',         'synthetic',      '019d6143aa027526a8c1d5f5e7e521e3', 'vocabulary',     '019d6143aab175c4b07116ae01f47bc1', 'glitching',                'Synth has identity; vocab becomes gerund'],
  ['Lighthouse',     'synthetic',      '019d6143aa0972fa8f85fab2cda4fb46', 'vocabulary',     '019d6143aac37b1793ca48c82f6df4d3', 'lighthouse-watch',         'Synth identity; vocab becomes compound'],
  ['Skinwalker',     'synthetic',      '019d6143aa2173f9bee411e15c371c19', 'vocabulary',     '019d6143aaf272ec8ceb9183572d8c90', 'skin-walking',             'Synth identity; vocab becomes gerund'],
  ['Static',         'synthetic',      '019d6143aa24761d9e15aef3ecccd8fd', 'vocabulary',     '019d6143aaf971f8a65a9bcdb4fb3aa3', 'static-head',              'Synth identity; vocab becomes derogatory compound'],
  ['Street Samurai', 'character',      '019d6143a648787696880f6d38d70075', 'vocabulary',     '019d6143aafb7b6bbcbcb8614f9d228f', 'samurai',                  'Character (Kyle) owns the moniker; vocab becomes shorter archetypal term'],
  ['The Canopy',     'place',          '019d6143a9377fcab7213894f2fbd0ff', 'vocabulary',     '019d6143ab00700a973c31eed8cd952c', 'canopy-walk',              'Place wins; vocab compound'],
  ['The Burnline',   'place',          '019d6143a93677dfa1bb4ee09eb5c704', 'vocabulary',     '019d6143ab007031b8238ca9b9121d88', 'burnline-cross',           'Place wins; vocab compound'],
  ['The Lattice',    'place',          '019d6143a9447e348c7f880d03dfd5af', 'vocabulary',     '019d6143ab01721db127bf13922d061c', 'lattice-run',              'Place wins; vocab compound'],
  ['The Collective', 'faction',        '019d6143a88e7b299200216573c49fa4', 'vocabulary',     '019d6143ab017a4aa8d1bca3b5d3832b', 'collective-speak',         'Organization wins; vocab becomes speech-term'],
  ['The Gatemouth',  'place',          '019d6143a93f747b9a2533e1baa35ffb', 'vocabulary',     '019d6143ab017dc4a00253b45ecb4d00', 'gatemouth-passage',        'Place wins; vocab compound'],
  ['The Perch',      'place',          '019d6143a94b701eb1e64ae9724efd96', 'vocabulary',     '019d6143ab027198a7120822958d19a6', 'perching',                 'Place wins; vocab gerund'],
  ['The Narrows',    'place',          '019d6143a9487b0e9b1563f753f313c9', 'vocabulary',     '019d6143ab0277cb9390a780a026daa7', 'narrows-run',              'Place wins; vocab compound'],
  ['The Undertow(v)','place',          '019d6143a9527863ad656674299d38ba', 'vocabulary',     '019d6143ab037766bd9d060793fcad57', 'undertow-pull',            'Place wins; vocab compound (second Undertow pair)'],
  ['Threshold',      'place',          '019d6143a95572ac9d2d78751976f2cb', 'vocabulary',     '019d6143ab0377d186ca0d0e421eca9f', 'thresholder',              'Place wins; vocab becomes agent noun'],
  ['Toll Collector', 'synthetic',      '019d6143aa2b7b909a450c23770e34a2', 'vocabulary',     '019d6143ab077851b53012b3ec56134e', 'toll-caller',              'Synth identity; vocab becomes distinct role-term'],
  ['Voltage',        'place',          '3025eaa54f79e18c5c9ec8af7df4c26d', 'vocabulary',     '019d6143ab0c7c8bb03691c5aa3200dc', 'voltage-fried',            'Place wins; vocab becomes derogatory compound'],
  ['Lumen',          'synthetic',      '019d6143a64d703ea5beff0c37fea8a9', 'cyberware',      '51e6a09c3d78bf142c90e847d2f3a160', 'Lumine',                   'Emergent AI has identity; cyberware product_name renamed'],
  ['Clean Hands',    'entertainment',  '019d6513b76e734f9702c716a9412f2e', 'vocabulary',     '019d6143aa8f73c3a40609bd58e8f48c', 'clean-handed',             'Media title is specific cultural artifact; vocab becomes adjective'],
  ['The Seam',       'place',          'b43a4971e5b2f34a785112ce21de7392', 'entertainment',  '2e9f5c1d8a4b037f6e2c9d5a1b8f4e70', 'The Seam (series)',        'Place wins; entertainment adds format qualifier'],
  ['Witness',        'synthetic',      '40eea323f1d6a4bf3c62bfc26c771c55', 'entertainment',  '3d9f5a2c8e1b047f6d3a9c5e2f8b1d04', 'Witnessed',                'Synth identity; entertainment becomes past-participle'],
  ['CLEAN SIGNAL',   'entertainment',  '5f2a8e0c1b7d34a9e5c0f6823d9b1e47', 'vocabulary',     '019d6143aa907bab9e99854269c22c4d', 'clean-signal-talk',        'Media title wins; vocab becomes speech-term'],
  ['Stratum',        'cyberware',      'b84e1f62a3075dc9e4b2f0817a6d3c59', 'entertainment',  'c5d2a8f01e7b3946ad5c2e8f1b0d7c39', 'Stratum (anthology)',      'Product name has commercial weight; entertainment qualifies format'],
  ['Obsidian',       'place',          '4c82950b6d63c6a60c2bae3ecd9e9d6c', 'material',       '0db4cbbf483ecaa42037198a1c48cbac', 'Obsidyne',                 'Place wins; material renamed to branded variant'],
  ['Foxfire',        'synthetic',      '019d6143a9ff73fca89f0399919059db', 'pharmaceutical', '019d7850c6ad7f2fbff6595c23b3df3b', 'Foxflame',                 'Synth identity; pharma renamed to related motif'],
  ['LULLABY',        'contract',       '019d6143a7a27444b466bffaed500678', 'synthetic',      '019d6143aa0b7c6c9fc15e993621cd5c', 'Lullaby-Wraith',           'Contract codename is operational designator; synth gets variant'],
  ['TOOTH FAIRY',    'contract',       '019d6143a7a3703cb376aaa175a3cbbc', 'synthetic',      '019d6143aa2b7fdd9bd7b2b784cd69be', 'Tooth-Fairy-Revenant',     'Contract codename; synth gets variant'],
  ['MIRROR MIRROR',  'contract',       '019d6143a7a37a619fcef93b0e85fe14', 'synthetic',      '019d6143aa0e7a6eb7959983618cc2e1', 'Mirrormind',               'Contract codename; synth gets variant'],
];

function load(dirKey, id) {
  const p = `${path}/${dirs[dirKey]}/${id}.json`;
  return { p, data: JSON.parse(fs.readFileSync(p, 'utf8')) };
}

function save(p, data) {
  fs.writeFileSync(p, JSON.stringify(data, null, 2));
}

// For a loser, mutate the primary naming field(s) and remove any alias that still matches
// the winner's conflicting name (case-insensitive) so the new name is truly distinct.
function renameLoser(loserType, data, oldName, newName) {
  const eqs = (a, b) => (a || '').trim().toLowerCase() === (b || '').trim().toLowerCase();

  switch (loserType) {
    case 'vocabulary':
      data.term = newName;
      break;
    case 'contract':
      data.codename = newName;
      break;
    case 'cyberware':
    case 'material':
      if (data.product_name && eqs(data.product_name, oldName)) data.product_name = newName;
      else if (eqs(data.name, oldName)) data.name = newName;
      else data.name = newName;
      break;
    default:
      // character / synthetic / place / faction / entertainment / pharmaceutical
      if (eqs(data.name, oldName)) data.name = newName;
      if (Array.isArray(data.aliases)) {
        data.aliases = data.aliases.filter(a => !eqs(a, oldName));
      }
      break;
  }
  return data;
}

const results = [];
for (const [label, winType, winId, loseType, loseId, newName, reason] of decisions) {
  try {
    const w = load(winType, winId);
    const l = load(loseType, loseId);

    const rawKey = label.replace(/\([^)]+\)$/, '').trim(); // strip (syn)/(v) disambiguators I added
    renameLoser(loseType, l.data, rawKey, newName);
    save(l.p, l.data);

    results.push({
      name: rawKey,
      winner: `${winType}/${winId}`,
      loser: `${loseType}/${loseId}`,
      newName,
      reason
    });
  } catch (e) {
    results.push({ name: label, error: e.message });
  }
}

console.log(JSON.stringify(results, null, 2));

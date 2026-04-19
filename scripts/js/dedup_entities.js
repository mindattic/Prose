#!/usr/bin/env node
/**
 * Deduplicates entity JSON files within each data directory.
 * Groups files by their display name (name or product_name) within each folder.
 * For groups with 2+ files, keeps the richest (largest JSON), archives the rest.
 * Never deletes — always moves to engine/data/archives/YYYY-MM-DD-dedup/
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, '../../engine/data');
const DRY_RUN = process.argv.includes('--dry-run');

const SCAN_DIRS = [
    'ammunition', 'apparel', 'automata', 'consumer_goods', 'contracts',
    'corponations', 'cyberware', 'entertainment', 'equipment', 'factions',
    'genemods', 'lab_specimens', 'materials', 'people', 'pharmaceuticals',
    'places', 'psionics', 'subsidiaries', 'synthetics', 'technology',
    'transportation', 'vocabulary', 'weaponry',
];

// Which field holds the display name for each directory
function getDisplayName(obj) {
    // product_name takes precedence where present
    if (obj.product_name && obj.product_name.trim()) return obj.product_name.trim();
    if (obj.term && obj.term.trim()) return obj.term.trim();      // vocabulary
    if (obj.codename && obj.codename.trim()) return obj.codename.trim(); // contracts
    if (obj.name && obj.name.trim()) return obj.name.trim();
    return null;
}

function richness(obj) {
    // Score = total JSON length (captures populated fields)
    return JSON.stringify(obj).length;
}

const archiveDir = path.join(ROOT, 'archives', `${new Date().toISOString().slice(0,10)}-dedup`);
let totalArchived = 0;
let totalGroups = 0;

for (const dir of SCAN_DIRS) {
    const dirPath = path.join(ROOT, dir);
    if (!fs.existsSync(dirPath)) continue;

    const files = fs.readdirSync(dirPath).filter(f => f.endsWith('.json'));
    const groups = new Map(); // name (lowercased) → [{file, obj, size}]

    for (const file of files) {
        const fullPath = path.join(dirPath, file);
        let obj;
        try { obj = JSON.parse(fs.readFileSync(fullPath, 'utf8')); }
        catch { console.warn(`  SKIP (parse error): ${file}`); continue; }

        const name = getDisplayName(obj);
        if (!name || name.length < 3) continue;

        const key = name.toLowerCase();
        if (!groups.has(key)) groups.set(key, []);
        groups.get(key).push({ file, fullPath, obj, size: richness(obj) });
    }

    for (const [name, entries] of groups) {
        if (entries.length < 2) continue;

        totalGroups++;
        // Sort richest first
        entries.sort((a, b) => b.size - a.size);
        const keeper = entries[0];
        const losers = entries.slice(1);

        console.log(`\n[${dir}] "${entries[0].obj.name ?? name}" — keep ${keeper.file} (${keeper.size} chars), archive ${losers.length}`);

        for (const loser of losers) {
            const destDir = path.join(archiveDir, dir);
            const dest = path.join(destDir, loser.file);
            console.log(`  → archive ${loser.file} (${loser.size} chars)`);

            if (!DRY_RUN) {
                fs.mkdirSync(destDir, { recursive: true });
                fs.renameSync(loser.fullPath, dest);
            }
            totalArchived++;
        }
    }
}

console.log(`\n${'─'.repeat(60)}`);
console.log(`Duplicate groups found: ${totalGroups}`);
console.log(`Files archived:         ${totalArchived}`);
if (DRY_RUN) console.log('(DRY RUN — no files moved)');
else console.log(`Archive location: ${archiveDir}`);

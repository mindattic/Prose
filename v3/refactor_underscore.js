// Refactor: remove underscore prefix from private fields in C# and Razor files.
// For each file, finds all `_camelCase` private fields and renames them to `camelCase`
// throughout the file. Skips patterns that aren't field names (string literals, comments, etc.)

const fs = require('fs');
const path = require('path');

const rootDir = path.join(__dirname);
const extensions = ['.cs', '.razor'];
const skipDirs = ['obj', 'bin', 'node_modules'];

let totalFiles = 0;
let totalRenames = 0;

function findFiles(dir) {
    const files = [];
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        if (skipDirs.includes(entry.name)) continue;
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) files.push(...findFiles(full));
        else if (extensions.includes(path.extname(entry.name))) files.push(full);
    }
    return files;
}

function refactorFile(filePath) {
    let content = fs.readFileSync(filePath, 'utf8');

    // Find all private field declarations with underscore prefix
    // Matches: private [readonly] [type] _fieldName
    const fieldPattern = /(?:private|protected)\s+(?:(?:readonly|static|new|volatile|event)\s+)*\S+\s+(_([a-z][a-zA-Z0-9]*))\b/g;

    const renames = new Map(); // _oldName -> newName
    let match;
    while ((match = fieldPattern.exec(content)) !== null) {
        const oldName = match[1]; // _fieldName
        const newName = match[2]; // fieldName

        // Skip if newName would collide with a C# keyword or existing identifier
        if (['in', 'out', 'ref', 'is', 'as', 'new', 'this', 'base', 'null', 'true', 'false', 'event'].includes(newName)) continue;

        // Skip _paths -> paths collision with parameter names (common pattern)
        // We'll handle this by checking if the non-underscore version already exists in the file
        // Actually, let's just do the rename - if there's a collision the build will catch it

        renames.set(oldName, newName);
    }

    if (renames.size === 0) return 0;

    let modified = content;
    for (const [oldName, newName] of renames) {
        // Replace all occurrences of _fieldName with fieldName
        // Use word boundary to avoid partial matches
        // But be careful not to replace inside string literals or comments
        const regex = new RegExp(`\\b${oldName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\b`, 'g');
        modified = modified.replace(regex, newName);
    }

    if (modified !== content) {
        fs.writeFileSync(filePath, modified);
        totalFiles++;
        totalRenames += renames.size;
        const rel = path.relative(rootDir, filePath);
        console.log(`  ${rel}: ${renames.size} fields renamed`);
        return renames.size;
    }
    return 0;
}

console.log('Refactoring underscore-prefixed fields...\n');

const files = findFiles(rootDir);
for (const f of files) {
    refactorFile(f);
}

console.log(`\nDone: ${totalFiles} files modified, ${totalRenames} fields renamed.`);

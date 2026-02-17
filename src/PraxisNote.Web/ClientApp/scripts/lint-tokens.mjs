#!/usr/bin/env node

/**
 * lint-tokens.mjs
 *
 * Validates that all var(--color-*) references in source files
 * match tokens defined in styles.css.
 *
 * Usage: node scripts/lint-tokens.mjs
 * Exit code: 0 = clean, 1 = invalid tokens found
 */

import { readFileSync, readdirSync } from 'fs';
import { join, relative } from 'path';
import { fileURLToPath } from 'url';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const ROOT = join(__dirname, '..');
const STYLES_PATH = join(ROOT, 'src', 'styles.css');
const SCAN_DIR = join(ROOT, 'src');

// Step 1: Extract all defined --color-* tokens from styles.css
function getDefinedTokens() {
  const css = readFileSync(STYLES_PATH, 'utf-8');
  const tokens = new Set();
  const defRegex = /(--color-[\w-]+)\s*:/g;
  let match;
  while ((match = defRegex.exec(css)) !== null) {
    tokens.add(match[1]);
  }
  return tokens;
}

// Step 2: Recursively find .ts and .css files
function findFiles(dir, extensions) {
  const results = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const fullPath = join(dir, entry.name);
    if (entry.isDirectory() && entry.name !== 'node_modules') {
      results.push(...findFiles(fullPath, extensions));
    } else if (entry.isFile() && extensions.some((ext) => entry.name.endsWith(ext))) {
      results.push(fullPath);
    }
  }
  return results;
}

// Step 3: Extract var(--color-*) usages from a file
function getUsages(filePath) {
  const content = readFileSync(filePath, 'utf-8');
  const lines = content.split('\n');
  const usages = [];
  const usageRegex = /var\((--color-[\w-]+)\)/g;
  for (let i = 0; i < lines.length; i++) {
    let match;
    while ((match = usageRegex.exec(lines[i])) !== null) {
      usages.push({ token: match[1], line: i + 1 });
    }
  }
  return usages;
}

// Main
const defined = getDefinedTokens();
const files = [STYLES_PATH, ...findFiles(join(SCAN_DIR, 'app'), ['.ts', '.css'])];
const violations = [];

for (const file of files) {
  const usages = getUsages(file);
  for (const { token, line } of usages) {
    if (!defined.has(token)) {
      violations.push({ file: relative(ROOT, file), line, token });
    }
  }
}

if (violations.length === 0) {
  console.log('All CSS color tokens are valid.');
  process.exit(0);
} else {
  console.error(`Found ${violations.length} invalid CSS color token(s):\n`);
  for (const v of violations) {
    console.error(`  ${v.file}:${v.line}  ->  ${v.token}`);
  }
  console.error(`\nDefined tokens are in src/styles.css. See THEMING.md for the full reference.`);
  process.exit(1);
}

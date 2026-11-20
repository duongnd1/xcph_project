import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const destination = fileURLToPath(new URL('../Assets/Resources/', import.meta.url));
const groups = { Main: 15100, Daily: 840, Advanced: 2596, Bonus: 46 };
let seed = 0x35ac89ef;
function random(max) {
    seed ^= seed << 13;
    seed ^= seed >>> 17;
    seed ^= seed << 5;
    return (seed >>> 0) % max;
}

function profile(group, index) {
    switch (group) {
        case 'Main':
            if (index < 25) {
                const colors = Math.min(8, 3 + Math.floor(index / 4));
                return [colors, 4, colors * 2 + 2, 2];
            }
            if (index < 100) return [index % 2 ? 10 : 8, 4, index % 2 ? 26 : 21, 2];
            return index % 2 ? [13, 4, 38, 2] : [10, 4, 28, 2];
        case 'Daily': {
            const slot = Math.floor(index % 7 / 2);
            return [[6, 8, 10, 12][slot], 4, [15, 21, 28, 34][slot], 2];
        }
        case 'Advanced': {
            if (index < 502) {
                const colors = 5 + index % 4;
                const capacity = index % 3 ? 6 : 8;
                return [colors, capacity, colors * 4 + (capacity === 8 ? 7 : 3), 2];
            }
            const colors = 9 + index % 5;
            return [colors, 8, 41 + (colors - 9) * 2, 2];
        }
        case 'Bonus':
            return index < 23 ? [10, 4, 31, 3] : [13, 4, 40, 3];
        default:
            throw Error('Unknown group');
    }
}

function runLength(tube) {
    let length = 0;
    const color = tube.at(-1);
    while (length < tube.length && tube[tube.length - 1 - length] === color) length++;
    return length;
}

function solved(board, capacity) {
    return board.every(tube => !tube.length ||
        (tube.length === capacity && runLength(tube) === capacity));
}

// Both features survive arbitrary changes to tube order and color labels.
function signature(board, capacity, colors) {
    const patterns = board.map(tube => {
        const labels = new Map();
        return tube.map(color => {
            if (!labels.has(color)) labels.set(color, labels.size);
            return labels.get(color);
        }).join('');
    }).sort();
    const usage = Array.from({ length: colors }, () => Array(capacity).fill(0));
    for (const tube of board) tube.forEach((color, i) => usage[color][i]++);
    return capacity + ':' + patterns.join(',') + ':' + usage.map(x => x.join('.')).sort().join(',');
}

function grow(colors, capacity, target, spare) {
    const board = Array.from({ length: colors + spare }, (_, i) =>
        i < colors ? Array(capacity).fill(i) : []);
    const moves = [];
    const visited = new Set([board.join('|')]);

    for (let step = 0; step < target; step++) {
        const candidates = [];
        for (let a = 0; a < board.length; a++) {
            const source = board[a];
            if (!source.length) continue;
            const color = source.at(-1);
            const run = runLength(source);
            for (let b = 0; b < board.length; b++) {
                const dest = board[b];
                if (a === b || dest.length === capacity) continue;
                const same = dest.at(-1) === color;
                for (let n = 1; n <= Math.min(run, capacity - dest.length); n++) {
                    if (n === run && n < source.length) continue;
                    if (!dest.length && n === source.length) continue;
                    if (same && source.length !== capacity) continue;
                    const merged = dest.length > 0 && !same;
                    const fills = dest.length + n === capacity;
                    const blocksSplit = dest.length > 0 && runLength(dest) > 1;
                    candidates.push({ a, b, n, score: random(100) + (merged ? 105 : 0)
                        + (fills ? 8 : 0) - (blocksSplit ? 115 : 0)
                        - (same ? 45 : 0) - (n - 1) * 55 });
                }
            }
        }
        candidates.sort((a, b) => b.score - a.score);
        let chosen = false;
        for (const { a, b, n } of candidates) {
            const taken = board[a].splice(-n);
            board[b].push(...taken);
            const key = board.join('|');
            if (visited.has(key)) {
                board[b].splice(-n);
                board[a].push(...taken);
                continue;
            }
            visited.add(key);
            moves.unshift([b, a]);
            chosen = true;
            break;
        }
        if (!chosen) break;
    }
    return { board, moves };
}

function verify(board, moves, capacity) {
    const state = board.map(tube => tube.slice());
    if (solved(state, capacity)) throw Error('Already solved');
    for (const [a, b] of moves) {
        const source = state[a], dest = state[b];
        const color = source.at(-1);
        if (a === b || !source.length || dest.length === capacity ||
            (dest.length && dest.at(-1) !== color)) throw Error('Illegal pour');
        const amount = Math.min(runLength(source), capacity - dest.length);
        dest.push(...source.splice(-amount));
    }
    if (!solved(state, capacity)) throw Error('Solution incomplete');
}

const symbols = '0123456789ABCDEF';
const seen = new Set();
const verifyOnly = process.argv.includes('--verify-only');

function checkCatalog(group, count) {
    const file = path.join(destination, 'Levels' + group + '.txt');
    const rows = fs.readFileSync(file, 'utf8').trimEnd().split(/\r?\n/);
    if (rows.length !== count) throw Error(group + ' incorrect level count');
    const report = new Map();
    for (const [i, line] of rows.entries()) {
        const fields = line.split('|');
        if (fields.length !== 3) throw Error('Invalid level record');
        const [capacityText, layout, solution] = fields;
        const [colors, capacity, target, spare] = profile(group, i);
        const board = layout.split(',').map(t => [...t].map(c => symbols.indexOf(c)));
        const quantities = Array(colors).fill(0);
        for (const tube of board) {
            if (tube.length > capacity) throw Error('Tube over capacity');
            for (const color of tube) {
                if (color < 0 || color >= colors) throw Error('Invalid color');
                quantities[color]++;
            }
        }
        if (Number(capacityText) !== capacity || board.length !== colors + spare ||
            quantities.some(q => q !== capacity) || solution.length % 2) throw Error('Invalid board');
        const moves = solution.match(/../g).map(pair => [...pair].map(c => symbols.indexOf(c)));
        if (moves.length < target - 5 ||
            moves.some(pair => pair.some(n => n < 0 || n >= board.length))) throw Error('Invalid solution');
        const key = signature(board, capacity, colors);
        if (seen.has(key)) throw Error('Duplicate board');
        seen.add(key);
        verify(board, moves, capacity);
        const band = group === 'Main' ? (i < 25 ? '1–25' : i < 100 ? '26–100' : (i % 2 ? '102+ even' : '101+ odd'))
            : group === 'Daily' ? 'Slot ' + (i % 7 + 1)
                : group === 'Advanced' ? (i < 502 ? '1–502' : '503+')
                    : (i < 23 ? '1–23' : '24–46');
        if (!report.has(band)) report.set(band, []);
        report.get(band).push(moves.length);
    }
    for (const [band, lengths] of report) {
        lengths.sort((a, b) => a - b);
        const median = (lengths[Math.floor((lengths.length - 1) / 2)] + lengths[lengths.length >> 1]) / 2;
        console.log(group, band, lengths.length, 'witness median', median);
    }
}

fs.mkdirSync(destination, { recursive: true });
for (const [group, count] of Object.entries(groups)) {
    if (verifyOnly) {
        checkCatalog(group, count);
        continue;
    }
    const lines = [];
    const length = [];
    for (let i = 0; i < count; i++) {
        const [colors, capacity, target, spare] = profile(group, i);
        let accepted = false;
        let longest = 0;
        for (let attempt = 0; attempt < 400; attempt++) {
            const { board, moves } = grow(colors, capacity, target + random(5) - 2, spare);
            longest = Math.max(longest, moves.length);
            if (moves.length < target - 5) continue;
            const mixed = board.reduce((sum, tube) => sum +
                tube.filter((color, j) => j && color !== tube[j - 1]).length, 0);
            if (mixed < Math.floor((target - 3) * 0.57)) continue;
            const key = signature(board, capacity, colors);
            if (seen.has(key)) continue;
            verify(board, moves, capacity);
            seen.add(key);
            length.push(moves.length);
            const layout = board.map(t => t.map(c => symbols[c]).join('')).join(',');
            const solution = moves.map(([a, b]) => symbols[a] + symbols[b]).join('');
            lines.push(capacity + '|' + layout + '|' + solution);
            accepted = true;
            break;
        }
        if (!accepted) throw Error(group + ' level ' + (i + 1) + ' exhausted retries, longest ' + longest);
    }
    fs.writeFileSync(path.join(destination, 'Levels' + group + '.txt'), lines.join('\n') + '\n');
    length.sort((a, b) => a - b);
    console.log(group, count, 'witness moves min/median/max', length[0], length[count >> 1], length.at(-1));
}

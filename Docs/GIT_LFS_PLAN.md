# Git LFS migration plan

**Status: proposed, not executed.** Migrating LFS rewrites history, so it needs the whole
team present. This document is the plan and the reasoning — nothing here has been done.

## Why bother

Measured 2026-09-10:

- `.git` directory: **608 MB**
- Tracked binary assets: **~750 MB** across ~800 files

| Type | Size | Files |
|---|---|---|
| `.png` | 208 MB | 206 |
| `.wav` | 135 MB | 29 |
| `.mp3` | 123 MB | 15 |
| `.asset` (mostly terrain) | 110 MB | 273 |
| `.fbx` | 106 MB | 237 |

Worst individual offenders:

| Size | File |
|---|---|
| 74 MB | `Assets/audio/sounds/envirnment/669180__superstudiobr__brazilian_waterfall_river.wav` |
| 55 MB | `Assets/audio/sounds/envirnment/608359__gavpro__small-stream-sound-track.wav` |
| 23 MB | `Assets/New Terrain 5.asset` |
| 21 MB | `Assets/textures/Ground034_2K-PNG_NormalGL.png` |
| 13 MB | `Assets/models/Environment/prefabs/decor/fbx/fungle mass/fungle mass 2.fbx` |

Git stores a **complete new copy of a binary file on every change**, because it cannot delta
-compress them meaningfully. Every re-export of a 13 MB FBX permanently adds 13 MB to every
clone, forever. On a student project where several people re-export art regularly, this is
the thing that eventually makes the repo painful to clone at all.

## Do the cheap wins first

These need no history rewrite and should happen regardless — do them **before** any LFS
migration, so you don't migrate files you were going to delete anyway.

1. **The two ambience WAVs are 129 MB between them.** Converting them to OGG at a sensible
   bitrate would recover the overwhelming majority of that. They are ambient loops; nobody
   will hear the difference. This is the single highest-value change on this list.
2. **`Assets/New Terrain 3/4/5.asset` (49 MB total)** sit loose in the `Assets` root. Confirm
   they're still referenced by a scene before doing anything else — loose terrain data at the
   project root is usually left over from experiments.
3. **`Ground034_2K-PNG_NormalGL.png` at 21 MB** is an uncompressed 2K PNG normal map. Unity
   compresses it on import anyway, so the source PNG size is pure repo overhead.

## Proposed LFS tracking

```gitattributes
# Art source
*.psd   filter=lfs diff=lfs merge=lfs -text
*.fbx   filter=lfs diff=lfs merge=lfs -text
*.png   filter=lfs diff=lfs merge=lfs -text
*.jpg   filter=lfs diff=lfs merge=lfs -text
*.tga   filter=lfs diff=lfs merge=lfs -text

# Audio
*.wav   filter=lfs diff=lfs merge=lfs -text
*.mp3   filter=lfs diff=lfs merge=lfs -text
*.ogg   filter=lfs diff=lfs merge=lfs -text
```

**Deliberately NOT tracked in LFS:**

- `.unity`, `.prefab`, `.asset`, `.controller`, `.anim`, `.mat` — these are YAML text. They
  diff and merge properly today, and putting them in LFS would throw that away. The `.asset`
  category is large only because of the terrain files; solve those individually rather than
  by moving all 273 `.asset` files into LFS.
- `.cs`, `.meta` — text, small.

## Migration steps

Requires everyone to have pushed and to stop working during the window.

1. Everyone pushes all branches. Confirm nothing is unpushed anywhere.
2. **Resolve `ScriptBreanchfixs` first** — see the warning below. Do not migrate with a live
   unmerged branch outstanding.
3. Back up the remote (clone `--mirror` somewhere safe).
4. Install Git LFS on every machine: `git lfs install`.
5. Add the `.gitattributes` above and commit it.
6. Rewrite history with `git lfs migrate import --include="*.fbx,*.png,..." --everything`.
7. Force-push all branches.
8. **Everyone deletes their local clone and re-clones.** Pulling into an existing clone after
   a history rewrite goes badly; a fresh clone is faster than untangling it.

## Warnings

**Check your LFS quota before starting.** GitHub's free tier includes 1 GB of LFS storage and
1 GB/month of bandwidth. This project would exceed both immediately. Confirm what the account
actually has, or the migration will fail partway and leave the repo in a worse state than it
started.

**`ScriptBreanchfixs` has 15 unmerged commits** of real work (latest 2026-08-26). A history
rewrite while that branch is outstanding risks losing it. Merge or explicitly abandon it
first — see the branch notes in the handover summary.

**Two tracked files have quote characters in their filenames** (around
`Assets/.../…1s feast.asset`). These break several Windows tooling paths — they already
break PowerShell's `Test-Path`. Rename them at some point; do it before the migration so the
rewritten history has clean names.

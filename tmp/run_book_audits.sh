#!/bin/bash
set -uo pipefail
cd "D:/Projects/MindAttic/StreetSamurai"
mkdir -p tmp/audits

declare -A SLUGS
SLUGS[ATTE]="attendance-019ebf4c|glmz"
SLUGS[BCODA]="bushido_coda|glmz"
SLUGS[BLST]="ballast-019f3ac7|glmz"
SLUGS[CRIT]="double-entry-019f76e0|glmz"
SLUGS[CxC]="marrow-chrome-019f0968|glmz"
SLUGS[DWIACE]="death-whispers-in-a-cats-ear-019ec3fe|glmz"
SLUGS[ICFI]="it-came-from-iowa-019f3eb2|glmz"
SLUGS[IxS]="iron-silk-019f43b9|glmz"
SLUGS[MNEMO]="mnemosync-019ee11e|glmz"
SLUGS[MxG]="magenta-gunmetal-019f00a6|glmz"
SLUGS[NxR]="neon-rust-019f06da|glmz"
SLUGS[PXL]="the-door-is-unlocked-2db1c6ca|glmz"
SLUGS[RTR]="read-the-room-019f4990|glmz"
SLUGS[SPRW]="the-number-that-works-019ed367|glmz"
SLUGS[SRZR]="steppin-razor-019ef7be|glmz"
SLUGS[TEST]="the-court-martial-019ed361|glmz"
SLUGS[TLC]="the-long-cut-019f3007|glmz"
SLUGS[TWD]="the-fall-down-019f78f4|glmz"
SLUGS[TWU]="high-five-019f787d|glmz"
SLUGS[UNDR]="underclan-019eff97|glmz"
SLUGS[VATD]="vultures-at-the-door-019ec467|glmz"
SLUGS[LLSS]="lyra-sinterspawn-slayer-019f5bd9|scry"
SLUGS[M101]="m-101-019f69f4|scry"
SLUGS[TRUCE]="tournament-019fc10a|scry"
SLUGS[VIGL]="vigil-s-end-019f5767|scry"

for code in "${!SLUGS[@]}"; do
  IFS='|' read -r slug universe <<< "${SLUGS[$code]}"
  echo "=== $code ($slug, $universe) ==="
  dotnet run --project v3/StreetSamurai.Cli -- --book-audit --slug "$slug" --universe "$universe" --json \
    > "tmp/audits/${code}.json" 2> "tmp/audits/${code}.log"
  echo "  exit=$? done"
done
echo "ALL_DONE"

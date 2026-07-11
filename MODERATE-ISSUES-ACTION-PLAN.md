# GLMZ Contradiction Survey — Action Plan for 3 Moderate Issues

**Survey Date:** 2026-07-11  
**Status:** Phase 3 Complete — Ready for Implementation

---

## Issue 1: PXL Stale Beats (Retired "Nit" Drone References)

**Severity:** MODERATE  
**Status:** Expected (already flagged for 2026-07-11 rewrite)  
**Story:** PXL (Pixel)  
**Beats Affected:** 850, 875, 900 (disabled/stale)

### Problem
Disabled beats reference named drone "Nit" as companion:
- Beat 850: "launched Nit into it"
- Beat 875: "Nit's scout pass"
- Beat 900: "Nit's footage"

Per PXL node bible §3, "Nit" is RETIRED. Drones are tools, not named companions.

### Solution
**These beats are already marked STALE/PENDING REWRITE per 2026-07-11 refactor.**

Action: When PXL is rewritten per refactor plan, remove all "Nit" references and replace with generic drone references (e.g., "the drone's scout pass" instead of "Nit's scout pass").

**Status:** ✅ No action needed now (pending planned rewrite)

---

## Issue 2: CxC Prose — SS-LAW-26 Terminology Update

**Severity:** MODERATE  
**Status:** Flagged for verification  
**Story:** CxC (Crimson & Chrome)  
**Beats Affected:** All 11 disabled beats

### Problem
Node bible marked: "Terminology updated 2026-07-11: 'Exo'/'NSB'/'RFO' → 'Ghost'/'ECT'/'ghosting'"

Old job names are RETIRED per SS-LAW-26 (2026-07-11):
- `Exo` → `Ghost` (ECT operator/street operator)
- `NSB` → `ECT` (context-dependent)
- `RFO` → `ghosting` (context-dependent)

### Solution
**CxC beats are currently disabled.** When prose is re-enabled:

1. Search for instances of old terminology:
   - "Exo" → "Ghost" (when referring to job title or operator class)
   - "NSB" → "ECT" (context-dependent; verify meaning first)
   - "RFO" → "ghosting" (context-dependent; verify meaning first)

2. Replace with canonical terminology per SS-LAW-26

3. Verify context to ensure replacements make sense (these terms may appear in dialogue, titles, or other contexts)

**Action Items:**
- [ ] Re-enable CxC beats
- [ ] Scan full prose for old terminology instances
- [ ] Replace with canonical terms (Ghost, ECT, ghosting)
- [ ] Verify replacements in context
- [ ] Mark complete

**Status:** ⏳ Pending CxC prose re-enable and verification pass

---

## Issue 3: TEST Prose — NSB Cleanup

**Severity:** MODERATE  
**Status:** Flagged for verification  
**Story:** TEST (Testament)  
**Beats Affected:** All 3 disabled beats

### Problem
Node bible references "NSB frame" in test context. Per SS-LAW-26, "NSB" is RETIRED and should be replaced with canonical terminology (likely "ECT" or context-specific term).

### Solution
**TEST beats are currently disabled.** When prose is re-enabled:

1. Search for all instances of "NSB"
2. Determine context (is this a job title? organization? frame type?)
3. Replace with canonical terminology per SS-LAW-26
   - If job title: `NSB` → `Ghost` or `ECT`
   - If organization: verify canonical name
   - If tech term: use canonical "ECT" or specific replacement

4. Verify replacements make sense in context

**Action Items:**
- [ ] Re-enable TEST beats
- [ ] Scan full prose for NSB references
- [ ] Determine context of each reference
- [ ] Replace with canonical terms
- [ ] Mark complete

**Status:** ⏳ Pending TEST prose re-enable and verification pass

---

## Summary

| Issue | Story | Type | Action | Priority |
|-------|-------|------|--------|----------|
| Nit drone | PXL | Stale prose | Remove "Nit" references during planned rewrite | HIGH (planned) |
| Old terminology | CxC | Disabled prose | Verify & update Exo→Ghost, NSB→ECT, RFO→ghosting | MEDIUM |
| NSB cleanup | TEST | Disabled prose | Verify & update NSB references to canonical terms | MEDIUM |

### Next Steps

1. **PXL:** Proceed with scheduled 2026-07-11 rewrite. Remove all "Nit" references per node bible §3.
2. **CxC:** When prose is re-enabled, run SS-LAW-26 terminology pass and update old job names.
3. **TEST:** When prose is re-enabled, run NSB cleanup pass and replace with canonical terms.

**No synchronization errors found.** All three issues are pre-documented stale-prose items, not story contradictions. Canon is consistent; prose updates are pending planned rewrites/re-enables.

---

**Generated:** 2026-07-11  
**Survey Status:** ✅ PHASE 3 COMPLETE

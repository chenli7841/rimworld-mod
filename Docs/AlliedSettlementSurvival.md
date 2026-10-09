# Allied Settlement Survival — stages 1–2

## Stage 1: strength

An independent WorldComponent registers allied settlements within 30 approximate world tiles of any player home map on the root surface. It scans every 2,500 ticks (about one in-game hour). Existing saves register eligible settlements on the next scan. Orbital/underground layers are excluded.

Strength starts at 60, recovers 0.5/day up to 80, and can reach 100 through assistance. A lost alliance or out-of-range settlement freezes without losing its record. The clock advances while frozen, so returning does not grant catch-up recovery. Zero strength never naturally recovers. Records reference the settlement object, not its tile; removing and replacing a settlement does not inherit its state.

Select a registered settlement to see strength and status. Developer mode provides +/-10 controls. Zero is reserved for the later destruction stage; this release does not remove settlements. Crisis scheduling and its 15-day grace period, aid quests, threat sites, overview and configuration are later stages. The saved registration tick and crisis flag reserve their integration points.

## Validation

Run `Tests/Verify-AlliedSettlementSurvival.ps1` for numerical boundary, freeze, crisis, time and localization checks. Build against the installed RimWorld 1.6 assemblies. These checks cannot verify an actual running game.

In-game acceptance checks:

- Load an existing save with a nearby allied settlement; allow one scan and check strength 60.
- Wait one day: approximately 60.5. Save/reload: same value, no duplicate registration.
- Use developer controls above 80: natural recovery neither adds nor subtracts.
- Break the alliance or remove the nearby home map: frozen; re-alliance has no catch-up reward.
- Reach zero: pending collapse, no spontaneous recovery and no world-object removal.
- Check multiple home maps and non-surface maps; only eligible surface proximity counts.
- Recheck Archive quests, civilization research and Pawn AI using the existing save.

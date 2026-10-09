# Allied Settlement Survival — stages 1–2

## Stage 1: strength

An independent WorldComponent registers allied settlements within 30 approximate world tiles of any player home map on the root surface. It scans every 2,500 ticks (about one in-game hour). Existing saves register eligible settlements on the next scan. Orbital/underground layers are excluded.

Strength starts at 60, recovers 0.5/day up to 80, and can reach 100 through assistance. A lost alliance or out-of-range settlement freezes without losing its record. The clock advances while frozen, so returning does not grant catch-up recovery. Zero strength never naturally recovers. Records reference the settlement object, not its tile; removing and replacing a settlement does not inherit its state.

Select a registered settlement to see strength and status. Developer mode provides +/-10 controls. Zero is reserved for the later destruction stage; this release does not remove settlements. Crisis scheduling and its 15-day grace period, aid quests, threat sites, overview and configuration are later stages. The saved registration tick and crisis flag reserve their integration points.

## Stage 2: permanent migration

Move a player caravan onto an eligible allied settlement tile, select the caravan and click **Permanent migration / 永久移民**. Select one adult free colonist or colony slave and confirm permanent departure. Disabled candidates show why they cannot migrate. Registration refreshes when opening this menu.

Each accepted free colonist adds 10 strength and each colony slave adds 5 (capped at 100; the confirmation shows the actual gain). A full or collapsed settlement cannot accept migration. Prisoners, children, downed pawns, mental breaks, quest lodgers/helpers, borrowed and quest-reserved pawns are excluded. At least one other capable adult free colonist must remain as caravan owner. Active settlement maps are excluded.

Inventory transfers to remaining caravan members first. If any item cannot be moved, the pawn stays and no reward is granted; already moved items remain elsewhere in the same caravan. Apparel and equipped weapons stay on the migrant. The original Pawn changes faction and is retained in WorldPawns with KeepForever, preserving identity and relations. Existing world pawns are marked for retention without registering them twice. This does not physically populate an allied settlement map or guarantee future visits.

The world component saves migrated Pawn load IDs separately from settlement records. Re-recruitment, settlement removal or save/reload cannot reset contribution eligibility. No recall feature exists; vanilla or other mods may return the person. Eligibility is checked again at confirmation. Reward is recorded only after faction transfer and world registration succeed.

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
- Bring two adult free colonists, inventory, apparel and equipped weapons to an ally. Transfer one: +10 strength, same pawn identity and relationships, new faction, inventory retained by caravan, equipment retained by migrant.
- Bring a colony slave with a capable free colonist: slave migration must be available and award +5 strength. A non-player slave or prisoner must remain ineligible.
- With only one capable adult left, confirm migration is disabled. Test with only children, prisoners, downed or temporary pawns as the other members.
- Test a recruited former prisoner (allowed), unrecruited prisoner, non-player slave or quest helper (blocked).
- At strength 95, the confirmation and award must be +5; at 100 or zero migration must be blocked.
- Save/reload after migration. Use developer tools to recruit the same pawn again: contribution remains blocked, including at another settlement.
- Break the alliance, move the caravan or alter eligibility while the confirmation is open: confirmation must revalidate and reject without reward.
- Check caravan inventory totals before/after, world-pawn retention after a later save, and caravan return travel. Losing a member reduces carrying capacity; rebalance cargo or bring pack animals if necessary.

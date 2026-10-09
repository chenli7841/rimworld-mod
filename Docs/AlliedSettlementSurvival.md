# Allied Settlement Survival — stages 1–7

## Stage 1: strength

An independent WorldComponent registers allied settlements within 30 approximate world tiles of any player home map on the root surface. It scans every 2,500 ticks (about one in-game hour). Existing saves register eligible settlements on the next scan. Orbital/underground layers are excluded.

Strength starts at 60, recovers 0.5/day up to 80, and can reach 100 through assistance. A lost alliance or out-of-range settlement freezes without losing its record. The clock advances while frozen, so returning does not grant catch-up recovery. Zero strength never naturally recovers. Records reference the settlement object, not its tile; removing and replacing a settlement does not inherit its state.

Select a registered settlement to see strength and status. Developer mode provides +/-10 controls. Stage 6 removes collapsed settlements only after their map and active quest references are clear.

## Stage 2: permanent migration

Move a player caravan onto an eligible allied settlement tile, select the caravan and click **Permanent migration / 永久移民**. Select one adult free colonist or colony slave and confirm permanent departure. Disabled candidates show why they cannot migrate. Registration refreshes when opening this menu.

Each accepted free colonist adds 10 strength and each colony slave adds 5 (capped at 100; the confirmation shows the actual gain). A full or collapsed settlement cannot accept migration. Prisoners, children, downed pawns, mental breaks, quest lodgers/helpers, borrowed and quest-reserved pawns are excluded. At least one other capable adult free colonist must remain as caravan owner. Active settlement maps are excluded.

Inventory transfers to remaining caravan members first. If any item cannot be moved, the pawn stays and no reward is granted; already moved items remain elsewhere in the same caravan. Apparel and equipped weapons stay on the migrant. The original Pawn changes faction and is retained in WorldPawns with KeepForever, preserving identity and relations. Existing world pawns are marked for retention without registering them twice. This does not physically populate an allied settlement map or guarantee future visits.

The world component saves migrated Pawn load IDs separately from settlement records. Re-recruitment, settlement removal or save/reload cannot reset contribution eligibility. No recall feature exists; vanilla or other mods may return the person. Eligibility is checked again at confirmation. Reward is recorded only after faction transfer and world registration succeed.

## Stage 3: crisis scheduling

Each newly registered ally receives a 15-day protection period. The first crisis then starts automatically; each later crisis is scheduled 20–35 days after the previous one resolves or times out. Crisis kind is selected from famine, disease outbreak or hostile threat. At most one crisis per settlement and two worldwide can be active. If both slots are occupied, other due settlements wait without drawing another random interval.

The player receives a letter when a crisis starts. Its 10-day deadline and kind are saved. The settlement inspect string shows the active kind and remaining days. Out-of-range settlements or settlements that cease to be allied pause both their interval and deadline; they resume without catching up when they become eligible again. A timeout lowers strength by 20 (clamped at zero), then schedules a 20–35 day recovery interval. Natural strength recovery pauses during a crisis. Resolving a crisis through the stage 4/5 quest hooks also schedules the same recovery interval. Old saves receive one fresh 15-day protection period when first loaded with this scheduler.

This stage supplies the scheduler and `CompleteCrisis` hook. Food and medical crises are resolved through the aid quests in stage 4; hostile-threat crises are resolved through the combat missions in stage 5.

## Stage 4: food and medicine aid quests

When a famine or disease crisis begins, an automatically accepted quest is generated and linked to the crisis letter. Food deliveries require 150 nutrition from human-edible food; medicine deliveries require 10 units of any medicine definition. The quest tracks partial deliveries across multiple item types and saves its progress. The Caravan's **Deliver food / Deliver medicine** command appears when it is on the target settlement tile. A confirmation names the amount and warns that the goods will be consumed from caravan inventory. Whole items are transferred, so a final food stack may deliver slightly more nutrition than requested.

Completing the requested amount resolves the crisis and starts its recovery interval. If the deadline passes, the scheduler applies the strength loss once and ends the linked quest. The quest points to the allied settlement and shows delivery progress in its description.

## Stage 5: hostile-threat combat missions

A hostile-threat crisis creates a defended Bandit Camp site 2–7 tiles from the allied settlement and links it to an automatically accepted quest. The enemy faction is selected from hostile mechanoids, insects or humanlike factions that can generate a combat group. The site uses 500 threat points and the standard outpost map-generation rules.

Defeating every observed defender (killed or downed) resolves the crisis and schedules its normal recovery interval. Leaving the site while defenders remain does not count as victory. If the 10-day crisis deadline passes, the usual 20 strength penalty is applied once and the quest ends; the hostile outpost remains available to visit. The active quest, site, faction and observed defender references are saved.

## Stage 6: collapse, overview and settings

When strength reaches zero, the settlement becomes terminal and its linked aid or threat quests end. A collapsed settlement is removed only when it has no active map and no ongoing quest part still targets it. The world object is replaced by RimWorld's destroyed-settlement marker, leaving the faction intact. If removal is disabled in settings, it remains as a collapsed settlement. A letter explains the collapse and cleanup condition.

The **Allied Settlements / 盟友据点** main tab lists registered settlements, strength, eligibility, current crisis or next crisis time, and a world-map jump action. The mod settings persist a master enable switch, removal-at-zero switch, coverage radius, initial strength, recovery values, migration rewards, crisis timing and limits, failure loss, aid amounts and threat-site points. Disabling the system pauses recovery and crisis timers; re-enabling it resumes them without catch-up.

## Stage 7: allied trader caravan reinforcement

On RimWorld 1.6.4871, the trader arrival worker calls the inherited `IncidentWorker_NeutralGroup.SpawnPawns(IncidentParms)` method to obtain its native pawn list, then builds the normal `LordJob_TradeWithColony`. The mod patches that generation method only when the runtime worker is `IncidentWorker_TraderCaravanArrival`. In this version, `PawnGroupMakerParms` carries the group kind, faction, trader kind, tile and points; `TraderKindDef` owns the stock generators, and `PawnKindDef` exposes the trader and combat-power roles. The patch leaves the trader kind and native list alone, appending guards before the existing lord is created.

For an allied faction, the hook selects the nearest active, registered same-faction settlement to the incident target tile. If the system is disabled, the faction is not allied, no eligible source exists, the arrival is orbital, or faction guard definitions are unavailable, the vanilla pawn list is returned unchanged. Added guards are generated with RimWorld's `PawnGenerator` from the faction's own Trader pawn-group `guards` options. This preserves faction-specific tech and equipment and does not edit shared `PawnKindDef`s. Strength 25–49 adds one guard; 50–74 adds two and prefers the faction's stronger guard kinds; 75–89 adds three and marks one elite; 90+ adds four and marks up to two elites. Neolithic-and-lower-tech factions keep their native guard kinds and scale mainly by numbers: +4 at 50–74, up to 12 total at 75–89, and 17–22 total guards at 90+.

The world component stores each escort's Pawn reference, source Settlement reference and casualty role. The highest-ranking guard (a faction-leader kind when present, otherwise the strongest guard) is the caravan leader. A narrowly scoped `Pawn.Kill` prefix/postfix pair records only guards belonging to `LordJob_TradeWithColony` and applies the loss only after the pawn is actually dead: 1 strength for a regular guard, 2 for an elite and 3 for the leader. A live pawn leaving the map clears its record, so retreat or normal caravan departure has no penalty. The records are saved through `IExposable`; older saves load with an empty record list. The optional arrival notification was omitted to avoid a second letter alongside the vanilla trader letter.

## Validation

Run `Tests/Verify-AlliedSettlementSurvival.ps1` for numerical boundary, freeze, localization and installed Harmony API checks. Build against the installed RimWorld 1.6 assemblies. These checks cannot verify an actual running game.

In-game acceptance checks:

- Load an existing save with a nearby allied settlement; allow one scan and check strength 60.
- Wait one day: approximately 60.5. Save/reload: same value, no duplicate registration.
- Use developer controls above 80: natural recovery neither adds nor subtracts.
- Break the alliance or remove the nearby home map: frozen; re-alliance has no catch-up reward.
- In a new or updated save, receive no crisis during the 15-day grace period. Confirm the first crisis deadline is 10 days and its kind appears in the letter and settlement inspection.
- Trigger multiple nearby allies: no more than two active crises worldwide and no more than one per settlement. Save/reload while a crisis is active; kind and deadline must remain stable.
- Make a settlement ineligible while its interval or deadline is running; after restoring eligibility, confirm the timer resumes without catching up.
- Let a crisis expire: exactly 20 strength lost once, strength recovery remains paused while active, and next crisis is delayed 20–35 days after timeout.
- Reach zero with an active settlement map: verify quests end, removal waits, and the map can be exited; then verify replacement by a destroyed-settlement marker and preservation of the faction.
- Reach zero while a separate ongoing quest targets the settlement: the linked aid/threat quest should fail, while removal waits for the separate quest to end. Save/reload while collapse is pending and verify the pending state persists.
- Disable automatic removal: zero-strength settlements remain collapsed; re-enable it and verify safe cleanup.
- Open the Allied Settlements main tab; verify rows, crisis countdowns, eligibility, world-map selection and empty-state text in English and Chinese.
- Change every mod setting, save/reload, and verify persistence and its effect on new registrations, future crises, aid quests, migration rewards and threat-site points.
- Disable the whole system during a scheduled interval and during a crisis; verify both timers pause and resume without catch-up.
- Famine and disease crises generate their linked aid quest; deliver partial food and medicine amounts, save/reload and finish the remaining requirement.
- From a caravan on the target tile, deliver food by nutrition and medicine by item count. Verify the confirmation, consumed inventory, quest progress, settlement target and resolution.
- Leave an aid quest incomplete through its 10-day deadline; verify only one penalty, failure of the linked quest and the next recovery interval.
- Trigger a hostile-threat crisis. Confirm the marked Bandit Camp is within 2–7 tiles of the allied settlement, uses a hostile mechanoid, insect or humanlike faction, and is linked to its quest.
- Visit the site and defeat every defender: the quest should resolve the crisis and schedule the normal recovery interval. Save/reload with the site map generated and verify defender progress persists.
- Retreat while defenders remain: the quest must stay active. Let it expire to confirm one 20-point penalty; the outpost should remain on the world map.
- Receive a trader from an active allied settlement. Confirm native merchants and stock are unchanged, guards use only that faction's Trader guard definitions, the nearest same-faction settlement supplies the source, and the letter/arrival behavior remains vanilla.
- Compare civilization tiers at strengths 24, 25, 50, 75 and 90; confirm the guard counts and stronger faction-defined guard kinds increase without changing settlement, raid or player-caravan pawn generation.
- Compare Neolithic trader factions at 49, 50, 75 and 90; verify +4 guards at 50+, up to 12 at 75+, and 17–22 guards at 90+, all using tribal definitions.
- Save and reload while a reinforced trade caravan is present. Kill one ordinary guard, one elite and its leader, verifying exact losses of 1, 2 and 3 once each. Down a guard, then let the caravan leave alive: neither event should reduce strength. Kill an ordinary merchant: it should not be tracked.
- Repeat with no nearby registered same-faction settlement, a non-allied faction, an orbital trader, and the system disabled; each should use the vanilla group unchanged.
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

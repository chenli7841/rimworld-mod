param([string]$GameManagedPath = 'C:/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed')
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $modRoot 'Source/LiAIChat/AlliedSettlementSurvival/SettlementStrengthPolicy.cs')
Add-Type -Path (Join-Path $modRoot 'Source/LiAIChat/AlliedSettlementSurvival/AlliedCaravanReinforcementPolicy.cs')
function Assert-Close([double]$actual, [double]$expected, [string]$message) {
    if ([Math]::Abs($actual - $expected) -gt 0.0001) { throw "$message : $actual != $expected" }
}
$policy = [LiAIChat.AlliedSettlementSurvival.SettlementStrengthPolicy]
$caravanPolicy = [LiAIChat.AlliedSettlementSurvival.AlliedCaravanReinforcementPolicy]
Assert-Close ($policy::Recover(60, 60000, $true, $false)) 60.5 'Daily recovery'
Assert-Close ($policy::Recover(79.9, 60000, $true, $false)) 80 'Recovery cap'
Assert-Close ($policy::Recover(95, 60000, $true, $false)) 95 'Aid above cap must not decay'
Assert-Close ($policy::Recover(60, 600000, $false, $false)) 60 'Frozen settlement'
Assert-Close ($policy::Recover(60, 600000, $true, $true)) 60 'Active crisis'
Assert-Close ($policy::Recover(0, 600000, $true, $false)) 0 'Collapsed settlement must not resurrect'
Assert-Close ($policy::Recover(60, -1000, $true, $false)) 60 'Clock rollback'
Assert-Close ($policy::Clamp(105)) 100 'Strength maximum'
Assert-Close ($policy::Clamp(-20)) 0 'Strength minimum'
$stepped = 60.0
for ($i = 0; $i -lt 24; $i++) { $stepped = $policy::Recover($stepped, 2500, $true, $false) }
Assert-Close $stepped 60.5 'Tick subdivisions'
if ($caravanPolicy::AdditionalGuardCount(24.9, $false, 0, 0) -ne 0 -or
    $caravanPolicy::AdditionalGuardCount(25, $false, 0, 0) -ne 1 -or
    $caravanPolicy::AdditionalGuardCount(50, $false, 0, 0) -ne 2 -or
    $caravanPolicy::AdditionalGuardCount(75, $false, 0, 0) -ne 3 -or
    $caravanPolicy::AdditionalGuardCount(90, $false, 0, 0) -ne 4) {
    throw 'Civilized caravan strength thresholds changed'
}
if ($caravanPolicy::AdditionalGuardCount(49.9, $true, 5, 0) -ne 0 -or
    $caravanPolicy::AdditionalGuardCount(50, $true, 5, 0) -ne 4 -or
    $caravanPolicy::AdditionalGuardCount(75, $true, 10, 0) -ne 2 -or
    $caravanPolicy::AdditionalGuardCount(90, $true, 5, 17) -ne 12 -or
    $caravanPolicy::AdditionalGuardCount(100, $true, 22, 22) -ne 0) {
    throw 'Tribal caravan size thresholds changed'
}
if ($caravanPolicy::EliteGuardCount(74.9) -ne 0 -or
    $caravanPolicy::EliteGuardCount(75) -ne 1 -or
    $caravanPolicy::EliteGuardCount(90) -ne 2) {
    throw 'Elite caravan guard thresholds changed'
}
Assert-Close ($caravanPolicy::DeathPenalty([LiAIChat.AlliedSettlementSurvival.AlliedCaravanGuardRole]::Guard)) 1 'Ordinary guard casualty loss'
Assert-Close ($caravanPolicy::DeathPenalty([LiAIChat.AlliedSettlementSurvival.AlliedCaravanGuardRole]::Elite)) 2 'Elite guard casualty loss'
Assert-Close ($caravanPolicy::DeathPenalty([LiAIChat.AlliedSettlementSurvival.AlliedCaravanGuardRole]::Leader)) 3 'Caravan leader casualty loss'
[xml]$english = Get-Content (Join-Path $modRoot 'Languages/English/Keyed/AlliedSettlementSurvival.xml') -Raw
[xml]$chinese = Get-Content (Join-Path $modRoot 'Languages/ChineseSimplified/Keyed/AlliedSettlementSurvival.xml') -Raw
$enKeys = @($english.LanguageData.ChildNodes | Where-Object NodeType -eq Element | ForEach-Object Name)
$zhKeys = @($chinese.LanguageData.ChildNodes | Where-Object NodeType -eq Element | ForEach-Object Name)
if (Compare-Object $enKeys $zhKeys) { throw 'Translation keys do not match' }
$sources = Get-ChildItem (Join-Path $modRoot 'Source/LiAIChat/AlliedSettlementSurvival') -Filter '*.cs'
foreach ($source in $sources) {
    foreach ($match in [regex]::Matches((Get-Content $source.FullName -Raw), '"(LiASS_[A-Za-z]+)"')) {
        if ($enKeys -notcontains $match.Groups[1].Value) { throw "Missing translation: $($match.Value)" }
    }
}
[void][Reflection.Assembly]::LoadFrom((Join-Path $GameManagedPath 'UnityEngine.CoreModule.dll'))
$gameAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $GameManagedPath 'Assembly-CSharp.dll'))
foreach ($target in @(@('RimWorld.Planet.Settlement','GetInspectString'), @('RimWorld.Planet.Settlement','GetGizmos'), @('RimWorld.Planet.Caravan','GetGizmos'))) {
    $type = $gameAssembly.GetType($target[0], $true)
    $method = $type.GetMethod($target[1])
    if ($null -eq $method -or $method.DeclaringType -ne $type -or $method.GetParameters().Count -ne 0) {
        throw "Harmony target changed: $($target -join '.')"
    }
}
$traderArrival = $gameAssembly.GetType('RimWorld.IncidentWorker_TraderCaravanArrival', $true)
$spawnPawns = $traderArrival.GetMethod('SpawnPawns', [Reflection.BindingFlags]'Instance,Public,NonPublic')
if ($null -eq $spawnPawns -or $spawnPawns.ReturnType.Name -ne 'List`1' -or
    $spawnPawns.GetParameters().Count -ne 1 -or $spawnPawns.GetParameters()[0].ParameterType.Name -ne 'IncidentParms' -or
    $spawnPawns.DeclaringType.FullName -ne 'RimWorld.IncidentWorker_NeutralGroup') {
    throw 'Trader caravan spawn hook changed in the installed RimWorld assemblies'
}
if ($null -eq $gameAssembly.GetType('RimWorld.LordJob_TradeWithColony', $true) -or
    $null -eq $gameAssembly.GetType('RimWorld.PawnGroupKindDefOf', $true).GetField('Trader') -or
    $null -eq $gameAssembly.GetType('Verse.PawnGenerator', $true).GetMethod('GeneratePawn', [Type[]]@(
        $gameAssembly.GetType('Verse.PawnKindDef', $true), $gameAssembly.GetType('RimWorld.Faction', $true),
        $gameAssembly.GetType('RimWorld.Planet.PlanetTile', $true)))) {
    throw 'Trader guard generation or trade-lord APIs changed in the installed RimWorld assemblies'
}
if (-not [Enum]::GetNames($gameAssembly.GetType('RimWorld.Planet.PawnDiscardDecideMode')).Contains('KeepForever')) {
    throw 'Permanent world-pawn retention API unavailable'
}
$worldObject = $gameAssembly.GetType('RimWorld.Planet.WorldObject', $true)
if ($null -eq $worldObject.GetMethod('Destroy', [Type[]]@())) {
    throw 'World-object destruction API unavailable'
}
$destroyedSettlement = $gameAssembly.GetType('RimWorld.WorldObjectDefOf', $true).GetField('DestroyedSettlement')
if ($null -eq $destroyedSettlement) { throw 'Destroyed-settlement marker definition unavailable' }
$settlementType = $gameAssembly.GetType('RimWorld.Planet.Settlement', $true)
if ($null -eq $settlementType.GetProperty('HasMap')) { throw 'Settlement map-lifetime check unavailable' }
$worldObjects = $gameAssembly.GetType('RimWorld.Planet.WorldObjectsHolder', $true)
$ruinLookup = $worldObjects.GetMethod('DestroyedSettlementAt', [Type[]]@($gameAssembly.GetType('RimWorld.Planet.PlanetTile', $true)))
if ($null -eq $ruinLookup) { throw 'Destroyed-settlement marker lookup unavailable' }
'Allied settlement policy, localization and installed Harmony target checks passed.'
'Safe collapse APIs for map checks and destroyed-settlement markers are available.'
'Actual caravan transfer and save/load still require the documented in-game checks.'

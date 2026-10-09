param([string]$GameManagedPath = 'C:/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed')
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $modRoot 'Source/LiAIChat/AlliedSettlementSurvival/SettlementStrengthPolicy.cs')
function Assert-Close([double]$actual, [double]$expected, [string]$message) {
    if ([Math]::Abs($actual - $expected) -gt 0.0001) { throw "$message : $actual != $expected" }
}
$policy = [LiAIChat.AlliedSettlementSurvival.SettlementStrengthPolicy]
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

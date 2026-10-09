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
'Allied settlement policy and localization checks passed.'

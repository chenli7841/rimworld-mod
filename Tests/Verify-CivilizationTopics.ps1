param(
    [string]$GameManagedPath = 'C:/Program Files (x86)/Steam/steamapps/common/RimWorld/RimWorldWin64_Data/Managed'
)
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path $PSScriptRoot -Parent
$gameData = Join-Path (Split-Path (Split-Path $modRoot -Parent) -Parent) 'Data'
function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}
function Assert-Close([double]$actual, [double]$expected, [string]$message) {
    Assert-True ([Math]::Abs($actual - $expected) -lt 0.00001) "$message (actual: $actual)"
}

Add-Type -Path (Join-Path $modRoot 'Source/LiAIChat/Civilization/CivilizationTopicPolicy.cs')
Assert-Close ([LiAIChat.Civilization.CivilizationTopicPolicy]::ResearchEfficiency($false, 100)) 1 'Changing topic must remove the penalty'
Assert-Close ([LiAIChat.Civilization.CivilizationTopicPolicy]::ResearchEfficiency($true, 0)) 1 'First research must have full efficiency'
Assert-Close ([LiAIChat.Civilization.CivilizationTopicPolicy]::ResearchEfficiency($true, 1)) 0.8 'First repeat'
Assert-Close ([LiAIChat.Civilization.CivilizationTopicPolicy]::ResearchEfficiency($true, 2)) 0.64 'Second repeat'
Assert-Close ([LiAIChat.Civilization.CivilizationTopicPolicy]::ResearchEfficiency($true, 100)) 0.4 'Efficiency must retain its floor'
Assert-True ([LiAIChat.Civilization.CivilizationTopicPolicy]::RewardWeight(0) -gt [LiAIChat.Civilization.CivilizationTopicPolicy]::RewardWeight(1)) 'Used bonuses must be less likely'
Assert-True ([LiAIChat.Civilization.CivilizationTopicPolicy]::RewardWeight(1) -gt [LiAIChat.Civilization.CivilizationTopicPolicy]::RewardWeight(2)) 'Repeated use must further reduce weight'
Assert-True ([LiAIChat.Civilization.CivilizationTopicPolicy]::RewardWeight(10000) -gt 0) 'Used bonuses must remain possible'
Assert-True ([LiAIChat.Civilization.CivilizationTopicPolicy]::BonusDays -eq 7) 'Bonus duration changed'
Assert-True ([LiAIChat.Civilization.CivilizationTopicPolicy]::MinimumCooldownDays -eq 2 -and [LiAIChat.Civilization.CivilizationTopicPolicy]::MaximumCooldownDays -eq 4) 'Cooldown bounds changed'

$xmlFiles = @(Get-ChildItem (Join-Path $modRoot 'Defs') -Recurse -Filter '*.xml')
foreach ($file in $xmlFiles) { [xml]$null = Get-Content -LiteralPath $file.FullName -Raw }
[xml]$topicXml = Get-Content (Join-Path $modRoot 'Defs/ResearchProjectDefs/ResearchProjects_CivilizationTopics.xml') -Raw
$projects = @($topicXml.Defs.ResearchProjectDef)
Assert-True ($projects.Count -eq 19) 'Unexpected enabled topic count'
$textNames = @{}
Get-ChildItem (Join-Path $modRoot 'Defs/EarthTextDefs') -Filter '*.xml' | ForEach-Object {
    [xml]$doc = Get-Content $_.FullName -Raw
    foreach ($node in $doc.SelectNodes('/Defs/*/defName')) { $textNames[$node.InnerText] = $true }
}
$statNames = @{}
Get-ChildItem (Join-Path $gameData 'Core/Defs/Stats') -Filter '*.xml' | ForEach-Object {
    [xml]$doc = Get-Content $_.FullName -Raw
    foreach ($node in $doc.SelectNodes('/Defs/StatDef/defName')) { $statNames[$node.InnerText] = $true }
}
$effects = @('Stat','SocialFight','MoodDecline','InjuredMoodRecovery','PainMood','NegativeSocialMemory','ColonyMood','GatheringMood','ReadingGain')
$bonusCount = 0
foreach ($project in $projects) {
    Assert-True ($project.tab -eq 'LiAIChat_CivilizationTopics') "Incorrect tab: $($project.defName)"
    $extension = $project.modExtensions.li
    Assert-True ($extension.Class -eq 'LiAIChat.Civilization.CivilizationTopicExtension') 'Wrong extension type'
    Assert-True ($textNames.ContainsKey([string]$extension.requiredText)) "Missing required text: $($extension.requiredText)"
    Assert-True (-not [string]::IsNullOrWhiteSpace($extension.conclusion)) 'Missing conclusion template'
    $bonuses = @($extension.bonuses.li)
    Assert-True ($bonuses.Count -ge 2) 'Random reward pool needs at least two choices'
    Assert-True (@($bonuses.id | Select-Object -Unique).Count -eq $bonuses.Count) 'Reward ids must be stable and unique within each topic'
    foreach ($bonus in $bonuses) {
        $effect = if ($bonus.effect) { [string]$bonus.effect } else { 'Stat' }
        Assert-True ($effects -contains $effect) "Unknown effect: $effect"
        Assert-True (-not [string]::IsNullOrWhiteSpace($bonus.label)) 'Missing reward description'
        if ($effect -eq 'Stat') { Assert-True ($statNames.ContainsKey([string]$bonus.stat)) "Missing stat: $($bonus.stat)" }
        if ($bonus.factor) { Assert-True ([double]::Parse($bonus.factor, [Globalization.CultureInfo]::InvariantCulture) -gt 0) 'Invalid factor' }
        $bonusCount++
    }
}
Assert-True ($bonusCount -eq 62) 'Unexpected enabled reward count'

# Check Harmony's runtime-only targets against the installed game, not guessed API names.
[Reflection.Assembly]::LoadFrom((Join-Path $GameManagedPath 'UnityEngine.CoreModule.dll')) | Out-Null
$gameAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $GameManagedPath 'Assembly-CSharp.dll'))
$flags = [Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
$targets = @{
    'Verse.ResearchProjectDef' = @('get_CanStartNow','CanBeResearchedAt','get_Description')
    'RimWorld.ResearchManager' = @('SetCurrentProject','AddProgress','ResearchPerformed','FinishProject')
    'RimWorld.Pawn_InteractionsTracker' = @('SocialFightChance')
    'RimWorld.Need_Mood' = @('NeedInterval')
    'RimWorld.Thought' = @('MoodOffset')
    'RimWorld.Thought_Memory' = @('MoodOffset')
    'RimWorld.Thought_MemorySocial' = @('OpinionOffset')
}
foreach ($typeName in $targets.Keys) {
    $type = $gameAssembly.GetType($typeName, $true)
    foreach ($methodName in $targets[$typeName]) {
        Assert-True ($null -ne $type.GetMethod($methodName, $flags)) "Missing Harmony target: $typeName.$methodName"
    }
}
$researchType = $gameAssembly.GetType('RimWorld.ResearchManager')
Assert-True ($null -ne $researchType.GetField('progress', $flags)) 'Missing per-project progress field'
Assert-True ($null -ne $researchType.GetField('currentProj', $flags)) 'Missing current-project field'
foreach ($methodName in @('AddProgress','ResearchPerformed')) {
    $amount = $researchType.GetMethod($methodName, $flags).GetParameters() | Where-Object Name -eq 'amount'
    Assert-True ($null -ne $amount -and $amount.ParameterType -eq [single]) "Research amount signature changed: $methodName"
}

Write-Output "PASS: rotation rules, $($xmlFiles.Count) XML documents, $($projects.Count) topics / $bonusCount rewards, text/stat references, Harmony target API checks."
Write-Output 'These checks do not launch RimWorld or replace the in-game save/load and effect checks listed in Docs/CivilizationTopics.md.'

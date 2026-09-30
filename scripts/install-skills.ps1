param(
    [string]$Destination = (Join-Path $env:USERPROFILE '.agents\skills')
)

$ErrorActionPreference = 'Stop'
$skillSourceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\skills'))
$skillDestinationRoot = [System.IO.Path]::GetFullPath($Destination)
if ($skillDestinationRoot -eq $skillSourceRoot -or $skillDestinationRoot.StartsWith($skillSourceRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw '安装目录不能位于仓库 skills 目录内。'
}
New-Item -ItemType Directory -Path $skillDestinationRoot -Force | Out-Null

# 先检查全部冲突，再创建链接；不覆盖、删除或移动现有技能。
$skillInstallPlan = foreach ($skillFolder in Get-ChildItem -LiteralPath $skillSourceRoot -Directory) {
    if (-not (Test-Path -LiteralPath (Join-Path $skillFolder.FullName 'SKILL.md'))) { continue }
    $skillTargetPath = Join-Path $skillDestinationRoot $skillFolder.Name
    if (Test-Path -LiteralPath $skillTargetPath) {
        $skillExistingItem = Get-Item -LiteralPath $skillTargetPath
        if ($skillExistingItem.LinkType -notin @('Junction', 'SymbolicLink') -or
            [System.IO.Path]::GetFullPath([string]$skillExistingItem.Target) -ne $skillFolder.FullName) {
            throw "已有同名技能未链接到本仓库，保留原目录并停止安装：$skillTargetPath"
        }
    } else {
        [PSCustomObject]@{ Path = $skillTargetPath; Source = $skillFolder.FullName }
    }
}
foreach ($skillInstallItem in $skillInstallPlan) {
    New-Item -ItemType Junction -Path $skillInstallItem.Path -Target $skillInstallItem.Source | Out-Null
    Write-Output "Installed: $($skillInstallItem.Path)"
}
Write-Output "Skills linked from $skillSourceRoot to $skillDestinationRoot."

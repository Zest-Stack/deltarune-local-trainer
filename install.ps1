$ErrorActionPreference='Stop'
$gameRoot='D:\Steam\steamapps\common\DELTARUNE'
$outRoot='C:\Users\Administrator\Documents\Codex\2026-10-31\yio\outputs\DELTARUNE修改器'
$patchRoot='C:\Users\Administrator\Documents\Codex\2026-10-31\yio\work\patched'
$backupName='codex_trainer_backup_20261031'
$backupRoot=Join-Path $gameRoot $backupName
if(Test-Path -LiteralPath $backupRoot){throw 'Backup directory already exists. Refusing to overwrite.'}
$records=@()
foreach($ch in 1..5){
    $relative="chapter$($ch)_windows\data.win"
    $original=Join-Path $gameRoot $relative
    $patch=Join-Path $patchRoot "chapter$ch.win"
    if(!(Test-Path -LiteralPath $patch) -or (Get-Item -LiteralPath $patch).Length -lt 1000000){throw "Missing patch $ch"}
    $records+=@{Relative=$relative;Original=$original;Patch=$patch;Backup=(Join-Path $backupRoot "chapter$ch.win");OriginalHash=(Get-FileHash -LiteralPath $original).Hash;PatchHash=(Get-FileHash -LiteralPath $patch).Hash}
}
New-Item -ItemType Directory -Path $backupRoot | Out-Null
foreach($r in $records){Copy-Item -LiteralPath $r.Original -Destination $r.Backup;if((Get-FileHash -LiteralPath $r.Backup).Hash -ne $r.OriginalHash){throw 'Backup validation failed'}}
$done=@()
try{
    foreach($r in $records){
        if((Get-FileHash -LiteralPath $r.Original).Hash -ne $r.OriginalHash){throw 'Game changed during installation'}
        $temp=$r.Original+'.cdt-install'
        Copy-Item -LiteralPath $r.Patch -Destination $temp
        [IO.File]::Replace($temp,$r.Original,[NullString]::Value)
        $done+=$r
        if((Get-FileHash -LiteralPath $r.Original).Hash -ne $r.PatchHash){throw 'Installed patch validation failed'}
    }
    $lines=$records|ForEach-Object{"$($_.Relative)`t$backupName\$([IO.Path]::GetFileName($_.Backup))`t$($_.OriginalHash)`t$($_.PatchHash)"}
    [IO.File]::WriteAllLines((Join-Path $outRoot 'installed.tsv'),[string[]]$lines)
    [IO.File]::WriteAllLines((Join-Path $backupRoot 'installed.tsv'),[string[]]$lines)
    'Installed and hash-verified all five chapter patches. Save files were not accessed.'
}catch{
    foreach($r in $done){Copy-Item -LiteralPath $r.Backup -Destination $r.Original -Force}
    throw
}

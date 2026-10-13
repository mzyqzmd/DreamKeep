$ErrorActionPreference = 'Stop'
$base = 'http://127.0.0.1:8080/mcp'
$out = 'd:\Unity\Projects\DreamKeep\smoke_result.txt'
if (Test-Path $out) { Remove-Item $out -Force }

function Log([string]$text) { Add-Content -Path $out -Value $text -Encoding UTF8 }

function CallMcp([string]$json, [string]$sid) {
    $h = @{ Accept = 'application/json, text/event-stream'; 'Content-Type' = 'application/json' }
    if ($sid) { $h['mcp-session-id'] = $sid }
    $r = Invoke-WebRequest -Uri $base -Method Post -Body $json -Headers $h -UseBasicParsing -TimeoutSec 600
    $line = ($r.Content -split "`n" | Where-Object { $_ -like 'data: *' } | Select-Object -First 1)
    if ($line) { return ($line -replace '^data: ', '') }
    return $r.Content
}

function CallTool([string]$name, $argsObj, [string]$sid, [int]$id) {
    $payload = @{ jsonrpc = '2.0'; id = $id; method = 'tools/call'; params = @{ name = $name; arguments = $argsObj } } |
        ConvertTo-Json -Depth 12 -Compress
    return (CallMcp $payload $sid)
}

Log ("=== start " + (Get-Date).ToString('HH:mm:ss'))
$init = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"codebuddy","version":"1.0"}}}'
$h0 = @{ Accept = 'application/json, text/event-stream'; 'Content-Type' = 'application/json' }
$r0 = Invoke-WebRequest -Uri $base -Method Post -Body $init -Headers $h0 -UseBasicParsing -TimeoutSec 120
$sid = $r0.Headers['mcp-session-id']
if ($sid -is [System.Array]) { $sid = $sid[0] }
CallMcp '{"jsonrpc":"2.0","method":"notifications/initialized"}' $sid | Out-Null

$playing = CallTool 'execute_code' @{ action = 'execute'; compiler = 'auto'; safety_checks = $false;
    code = 'return UnityEngine.Application.isPlaying ? "playing" : "edit";' } $sid 2
Log ("isPlaying: " + $playing)

if ($playing -notmatch 'playing') {
    Log ("play: " + (CallTool 'manage_editor' @{ action = 'play' } $sid 3))
    Start-Sleep -Seconds 12
}
else { Start-Sleep -Seconds 3 }

$code = Get-Content -Raw 'd:\Unity\Projects\DreamKeep\smoke_code.txt'
Log 'SMOKE:'
Log (CallTool 'execute_code' @{ action = 'execute'; compiler = 'auto'; safety_checks = $false; code = $code } $sid 4)

Log 'CONSOLE_ERRORS:'
Log (CallTool 'read_console' @{ types = @('error'); count = 20 } $sid 5)

Log ("stop: " + (CallTool 'manage_editor' @{ action = 'stop' } $sid 6))
Log ("=== done " + (Get-Date).ToString('HH:mm:ss'))

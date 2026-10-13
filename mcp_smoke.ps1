$ErrorActionPreference = 'Stop'
$base = 'http://127.0.0.1:8080/mcp'

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

$init = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"codebuddy","version":"1.0"}}}'
$h0 = @{ Accept = 'application/json, text/event-stream'; 'Content-Type' = 'application/json' }
$r0 = Invoke-WebRequest -Uri $base -Method Post -Body $init -Headers $h0 -UseBasicParsing -TimeoutSec 120
$sid = $r0.Headers['mcp-session-id']
if ($sid -is [System.Array]) { $sid = $sid[0] }
CallMcp '{"jsonrpc":"2.0","method":"notifications/initialized"}' $sid | Out-Null

Write-Output 'PROBE:'
CallTool 'execute_code' @{ action = 'execute'; compiler = 'auto'; safety_checks = $false;
    code = 'UnityEngine.Application.runInBackground = true; return "probe-ok";' } $sid 3

Start-Sleep -Seconds 8

$code = Get-Content -Raw 'd:\Unity\Projects\DreamKeep\smoke_code.txt'
Write-Output 'SMOKE:'
CallTool 'execute_code' @{ action = 'execute'; compiler = 'auto'; safety_checks = $false; code = $code } $sid 4

Write-Output 'CONSOLE_ERRORS:'
CallTool 'read_console' @{ types = @('error'); count = 20 } $sid 5

Write-Output 'STOP:'
CallTool 'manage_editor' @{ action = 'stop' } $sid 6

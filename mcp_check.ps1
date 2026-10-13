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

# 1) initialize -> session id
$init = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"codebuddy","version":"1.0"}}}'
$h0 = @{ Accept = 'application/json, text/event-stream'; 'Content-Type' = 'application/json' }
$r0 = Invoke-WebRequest -Uri $base -Method Post -Body $init -Headers $h0 -UseBasicParsing -TimeoutSec 120
$sid = $r0.Headers['mcp-session-id']
if ($sid -is [System.Array]) { $sid = $sid[0] }
Write-Output "SESSION=$sid"

# 2) initialized notification
CallMcp '{"jsonrpc":"2.0","method":"notifications/initialized"}' $sid | Out-Null

# 3) refresh_unity (force / all / compile / wait)
$refresh = '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"refresh_unity","arguments":{"mode":"force","scope":"all","compile":"request","wait_for_ready":true}}}'
Write-Output 'REFRESH:'
CallMcp $refresh $sid

# 4) poll editor state until not compiling
for ($i = 0; $i -lt 60; $i++) {
    $raw = CallMcp '{"jsonrpc":"2.0","id":3,"method":"resources/read","params":{"uri":"mcpforunity://editor/state"}}' $sid
    if ($raw -match '"is_compiling"\s*:\s*false') { Write-Output "STATE(iter=$i): $raw"; break }
    Start-Sleep -Seconds 2
}

# 5) console errors
Write-Output 'CONSOLE_ERRORS:'
CallMcp '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"read_console","arguments":{"types":["error"],"count":50}}}' $sid

# 6) timestamps
Write-Output 'TIMESTAMPS:'
Get-ChildItem 'D:\Unity\Projects\DreamKeep\Library\ScriptAssemblies\_DreamKeep.Presentation.dll' |
    Select-Object LastWriteTime, Name | Format-Table -AutoSize | Out-String | Write-Output
Get-ChildItem 'D:\Unity\Projects\DreamKeep\Assets\_Scripts\Presentation\Battle\Pop\PopRoot\UI_PopTextRoot.cs', 'D:\Unity\Projects\DreamKeep\Assets\_Scripts\Presentation\Battle\Hand\UI_HandView.cs' |
    Select-Object LastWriteTime, Name | Format-Table -AutoSize | Out-String | Write-Output

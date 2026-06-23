$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
Add-Type -AssemblyName System.Web | Out-Null

function Clean-Line([string]$s) {
  $s = [regex]::Replace($s, '<[^>]+>', '')
  $s = [System.Net.WebUtility]::HtmlDecode($s)
  $s = [regex]::Replace($s, '\s+', ' ').Trim()
  $s = ($s -replace '[;,]\s*$', '')
  return $s
}

$out = @()
for ($n = 1; $n -le 150; $n++) {
  $html = $null
  for ($a = 0; $a -lt 6; $a++) {
    try {
      $resp = Invoke-WebRequest -UseBasicParsing -TimeoutSec 40 `
        -Uri "https://www.liturgie.nu/psalmen/$n" `
        -Headers @{ 'User-Agent' = 'Mozilla/5.0' }
      $html = [System.Text.Encoding]::UTF8.GetString($resp.RawContentStream.ToArray())
      break
    } catch { Start-Sleep -Seconds (2 * ($a + 1)) }
  }
  if (-not $html) { throw "FAILED at psalm $n" }

  $verses = @()
  $vm = [regex]::Matches($html, 'class="versnummer">(\d+)</a><p class="verstekst">(.*?)<br', 'Singleline')
  foreach ($m in $vm) {
    $vnum = [int]$m.Groups[1].Value
    $vtitle = Clean-Line $m.Groups[2].Value
    $verses += [pscustomobject]@{ number = $vnum; title = $vtitle }
  }
  $count = $verses.Count
  $title = if ($count -gt 0) { $verses[0].title } else { $null }

  $out += [pscustomobject]@{ number = $n; title = $title; numberOfVerses = $count; verses = $verses }
  Write-Host "$n | $count verses | $title"
  Start-Sleep -Milliseconds 500
}

$json = $out | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText((Join-Path (Get-Location) 'psalmen-1773.json'), $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "WROTE psalmen-1773.json entries: $($out.Count)"

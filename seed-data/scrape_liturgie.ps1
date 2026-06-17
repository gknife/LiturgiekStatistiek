$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Web | Out-Null
$out = @()
for ($n = 1; $n -le 150; $n++) {
  $html = $null
  for ($a = 0; $a -lt 6; $a++) {
    try {
      $resp = Invoke-WebRequest -UseBasicParsing -TimeoutSec 40 `
        -Uri "https://www.liturgie.nu/psalmen/$n" `
        -Headers @{ 'User-Agent' = 'Mozilla/5.0' }
      $bytes = $resp.RawContentStream.ToArray()
      $html = [System.Text.Encoding]::UTF8.GetString($bytes)
      break
    } catch { Start-Sleep -Seconds (2 * ($a + 1)) }
  }
  if (-not $html) { throw "FAILED at psalm $n" }
  $count = ([regex]::Matches($html, 'class="versnummer"')).Count
  $title = $null
  $m = [regex]::Match($html, '<p class="verstekst">(.*?)<br', 'Singleline')
  if ($m.Success) {
    $t = $m.Groups[1].Value
    $t = [regex]::Replace($t, '<[^>]+>', '')
    $t = [System.Net.WebUtility]::HtmlDecode($t)
    $t = [regex]::Replace($t, '\s+', ' ').Trim()
    $t = ($t -replace '[;,]\s*$', '')
    $title = $t
  }
  $out += [pscustomobject]@{ number = $n; title = $title; numberOfVerses = $count }
  Write-Host "$n | $count | $title"
  Start-Sleep -Milliseconds 600
}
$json = $out | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText((Join-Path (Get-Location) 'psalmen-1773.json'), $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "WROTE psalmen-1773.json entries: $($out.Count)"
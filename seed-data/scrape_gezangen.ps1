$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Clean-Line([string]$s) {
  $s = [regex]::Replace($s, '<[^>]+>', '')
  $s = [System.Net.WebUtility]::HtmlDecode($s)
  $s = [regex]::Replace($s, '\s+', ' ').Trim()
  $s = ($s -replace '[;,]\s*$', '')
  return $s
}

$slugs = @(
  'lofzang-van-maria',
  'lofzang-van-zacharias',
  'lofzang-van-simeon',
  'tien-geboden',
  'gebed-des-heeren',
  'twaalf-artikelen-berijming-1',
  'twaalf-artikelen-berijming-2',
  'morgenzang',
  'avondzang',
  'bedezang-voor-de-predikatie',
  'bedezang-voor-het-eten',
  'dankzang-na-het-eten'
)

$out = @()
$n = 0
foreach ($slug in $slugs) {
  $n++
  $html = $null
  for ($a = 0; $a -lt 6; $a++) {
    try {
      $resp = Invoke-WebRequest -UseBasicParsing -TimeoutSec 40 `
        -Uri "https://www.liturgie.nu/gezangen/$slug" `
        -Headers @{ 'User-Agent' = 'Mozilla/5.0' }
      $html = [System.Text.Encoding]::UTF8.GetString($resp.RawContentStream.ToArray())
      break
    } catch { Start-Sleep -Seconds (2 * ($a + 1)) }
  }
  if (-not $html) { throw "FAILED at $slug" }

  $verses = @()
  $vm = [regex]::Matches($html, 'class="versnummer">(\d+)</a><p class="verstekst">(.*?)<br', 'Singleline')
  foreach ($m in $vm) {
    $verses += [pscustomobject]@{ number = [int]$m.Groups[1].Value; title = (Clean-Line $m.Groups[2].Value) }
  }
  $h1 = [regex]::Match($html, '<h1[^>]*>(.*?)</h1>', 'Singleline')
  $name = if ($h1.Success) { Clean-Line $h1.Groups[1].Value } else { $slug }
  $count = $verses.Count
  $title = if ($count -gt 0) { "$name" } else { $name }

  $out += [pscustomobject]@{ number = $n; title = $title; numberOfVerses = $count; verses = $verses }
  Write-Host "$n | $slug | $count verses | $name"
  Start-Sleep -Milliseconds 400
}

$json = $out | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText((Join-Path (Get-Location) 'enige-gezangen.json'), $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "WROTE enige-gezangen.json entries: $($out.Count)"

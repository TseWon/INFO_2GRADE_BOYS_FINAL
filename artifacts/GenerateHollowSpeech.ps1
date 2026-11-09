Add-Type -AssemblyName System.Speech
$root = Join-Path $PSScriptRoot 'hollow-creek'
$folder = Join-Path $root 'speech'
[IO.Directory]::CreateDirectory($folder) | Out-Null
$lines = Get-Content -LiteralPath (Join-Path $root 'SpeechLines.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $synth.SelectVoice('Microsoft Heami Desktop')
    $synth.Rate = 0
    $format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
    foreach ($line in $lines) {
        $synth.SetOutputToWaveFile((Join-Path $folder ($line.id + '.wav')), $format)
        $synth.Speak($line.text)
        $synth.SetOutputToNull()
    }
    Write-Output ('Generated ' + $lines.Count + ' temporary Korean narration WAV files.')
} finally { $synth.Dispose() }

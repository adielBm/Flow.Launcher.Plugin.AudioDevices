Get-AudioDevice -List | Where-Object { $_.Type -eq 'Playback' } | ForEach-Object { 
    "$($_.Index) | $($_.Name) | $($_.Default)" 
}
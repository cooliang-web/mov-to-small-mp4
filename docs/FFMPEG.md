# FFmpeg

This app uses FFmpeg for video conversion.

The build scripts expect `ffmpeg.exe` in one of these places:

- Next to `MovToSmallMp4.exe`
- `tools\ffmpeg\bin\ffmpeg.exe`
- Any folder on `PATH`

Run this command to download a Windows build into `tools\ffmpeg\bin`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\fetch-ffmpeg.ps1
```

FFmpeg is not developed by this project. See the FFmpeg license information before redistributing packaged builds.


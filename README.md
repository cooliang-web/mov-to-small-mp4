# MOV to Small MP4

A small Windows desktop tool for turning MOV files into smaller MP4 files. It supports FFmpeg-based frame preview, choosing a start and end point, and exporting an H.264 MP4 with size-focused presets.

## Features

- Open `.mov`, `.mp4`, `.m4v`, and `.qt` files.
- Preview video frames before cutting, without depending on the system video player.
- Mark the current preview time as the start or end point.
- Export a smaller `.mp4` using FFmpeg.
- Works offline after FFmpeg is placed next to the app.

## Windows

Build the app:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-windows-exe.ps1
```

Download FFmpeg into the local tool folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\fetch-ffmpeg.ps1
```

Create a release package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package.ps1
```

The package is created under `dist\MovToSmallMp4-windows.zip`.

The Windows package may include `ffmpeg.exe`. See `THIRD_PARTY_NOTICES.md` for the third-party license notice.

## Privacy

The app processes videos locally. It does not upload video files or collect analytics.

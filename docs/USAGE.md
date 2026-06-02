# Usage

1. Start `MovToSmallMp4.exe`.
2. Click `Open video` and choose a MOV file.
3. Drag the timeline to preview video frames.
4. Click `Set start` and `Set end` while previewing.
5. Choose a size preset.
6. Click `Export MP4`.

The smallest preset uses a lower resolution and stronger compression. It is best for sharing. The clearer preset keeps more detail but creates a larger file.

The preview is generated from FFmpeg frames. This avoids depending on system MOV codecs, so the app should still open on machines where the old Windows media preview components are unavailable.

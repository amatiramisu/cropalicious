# Cropalicious

I needed a tool to quickly capture screenshots at specific resolutions for building image datasets. Most screenshot tools either don't let you set exact dimensions or make you crop afterwards, which is tedious when you need hundreds of images at precise sizes.

Cropalicious shows a live preview rectangle that follows your mouse, snaps to screen boundaries, and captures exactly the dimensions you need with a single click.

## Features

- Preset sizes (1024×1024, 1216×832, etc.) optimized for common image dataset formats
- Custom size presets you can save and reuse
- Global hotkey (Ctrl+Shift+C by default) for quick captures
- Live preview overlay shows exactly what will be captured
- Left-click to capture, right-click to cancel
- Multi-monitor support

## Download

Grab the latest release from the [Releases](../../releases) page. Just download `Cropalicious-v1.0.0.zip`, extract, and run the `.exe` - no installation needed.

**Requirements:** .NET 9.0 Runtime (Desktop). If you don't have it, Windows will prompt you to download it automatically when you run the app.

## Building from Source

Requires .NET 9.0 SDK or later.

```bash
git clone https://github.com/yourusername/cropalicious.git
cd cropalicious/Cropalicious
dotnet build -c Release
dotnet run
```

## Usage

1. Launch Cropalicious and pick a preset size or add your own custom dimensions
2. A transparent overlay appears showing a preview rectangle
3. Move your mouse to position it where you want
4. Left-click to capture, right-click to cancel (or press Escape)
5. Screenshots save to `Pictures\Cropalicious` by default (configurable)

The app runs in the system tray - use the global hotkey (Ctrl+Shift+C) to trigger captures from anywhere.

## Configuration

- **Output folder**: Choose where screenshots are saved
- **Hotkey**: Change the global capture shortcut
- **Custom sizes**: Add your own preset dimensions with labels
- **Stay on top**: Keep the main window above other apps

## License

MIT

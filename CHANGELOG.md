# Changelog

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). `0.x` versions are alpha. Builds are labelled `0.0.1-<git hash>` (shown in the window title and in **Help → About**).

## [0.0.1]

First public release.

- Automatic keyboard layout switching based on the foreground program (Windows focus hook, no polling).
- `program → layout` rules, a default layout, and an "Enabled" switch.
- Open-windows list that hides Windows system processes (with an option to show them).
- English user interface, with a menu bar: **File → Quit**, **Settings** (default layout, start with Windows, start minimized in the tray, keep running in the tray on close) and **Help → About**.
- Tray icon; option to make the close button quit instead of minimizing to the tray.
- Optional start with Windows.
- `KBSWITCH_CONFIG` environment variable to use another config file (testing).
- The process list is only loaded when the window is shown, never during a hidden `--tray` start.

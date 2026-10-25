# Changelog

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). `0.x` versions are alpha. Builds are labelled `0.0.x-<git hash>` (shown in the window title and in **Help → About**).

## [0.0.2]

The project is renamed from **KbSwitch** to **AutoKbSwitch** (the old name was already used by other software). Nothing else changes in behaviour, but a few names change with it:

- the executable is now `AutoKbSwitch.exe`;
- the settings folder is now `%APPDATA%\AutoKbSwitch` (copy your old `config.json` there to keep your rules);
- the "Start with Windows" registry value is now named `AutoKbSwitch` (untick and tick the option again once to move it);
- the environment variable for an alternative config file is now `AUTOKBSWITCH_CONFIG`;
- the repository is now `github.com/crazyball/AutoKbSwitch` (old links keep redirecting).

## [0.0.1]

Released as KbSwitch.

First public release.

- Automatic keyboard layout switching based on the foreground program (Windows focus hook, no polling).
- `program → layout` rules, a default layout, and an "Enabled" switch.
- Open-windows list that hides Windows system processes (with an option to show them).
- English user interface, with a menu bar: **File → Quit**, **Settings** (default layout, start with Windows, start minimized in the tray, keep running in the tray on close) and **Help → About**.
- Tray icon; option to make the close button quit instead of minimizing to the tray.
- Optional start with Windows.
- `KBSWITCH_CONFIG` environment variable to use another config file (testing).
- The process list is only loaded when the window is shown, never during a hidden `--tray` start.

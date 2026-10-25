# Security Policy

## Reporting a vulnerability

Please do **not** open a public issue for a security problem.

Use GitHub's [private vulnerability reporting](https://github.com/crazyball/AutoKbSwitch/security/advisories/new) (*Security* tab → *Report a vulnerability*) or email holocronfactory@gmail.com.

## Scope

AutoKbSwitch makes no network connection of its own (the link in Help → About just opens the project page in your browser), never reads other programs' memory and injects nothing. It only writes `%APPDATA%\AutoKbSwitch\config.json` and, if the option is enabled, the `AutoKbSwitch` value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

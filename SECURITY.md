# Security Policy

## Reporting a vulnerability

Please do **not** open a public issue for a security problem.

Use GitHub's [private vulnerability reporting](https://github.com/crazyball/KbSwitch/security/advisories/new) (*Security* tab → *Report a vulnerability*) or email holocronfactory@gmail.com.

## Scope

KbSwitch makes no network connection of its own (the link in Help → About just opens the project page in your browser), never reads other programs' memory and injects nothing. It only writes `%APPDATA%\KbSwitch\config.json` and, if the option is enabled, the `KbSwitch` value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

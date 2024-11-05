<div align="center">

# PowerToys Run: Clipboard Manager
  
[![GitHub release](https://img.shields.io/github/v/release/CoreyHayward/PowerToys-Run-ClipboardManager?style=flat-square)](https://github.com/CoreyHayward/PowerToys-Run-ClipboardManager/releases/latest)
[![GitHub all releases](https://img.shields.io/github/downloads/CoreyHayward/PowerToys-Run-ClipboardManager/total?style=flat-square)](https://github.com/CoreyHayward/PowerToys-Run-ClipboardManager/releases/)
[![GitHub release (latest by date)](https://img.shields.io/github/downloads/CoreyHayward/PowerToys-Run-ClipboardManager/latest/total?style=flat-square)](https://github.com/CoreyHayward/PowerToys-Run-ClipboardManager/releases/latest)
[![Mentioned in Awesome PowerToys Run Plugins](https://awesome.re/mentioned-badge-flat.svg)](https://github.com/hlaueriksson/awesome-powertoys-run-plugins)

</div>

Simple [PowerToys Run](https://learn.microsoft.com/windows/powertoys/run) plugin for easily searching and pasting the clipboard history.

![ClipboardManager Demonstration](/images/ClipboardManager.gif)

## Requirements

- PowerToys minimum version 0.77.0
- Windows Clipboard History enabled `Windows key + V`

## Installation

- Download the [latest release](https://github.com/CoreyHayward/PowerToys-Run-ClipboardManager/releases/) by selecting the architecture that matches your machine: `x64` (more common) or `ARM64`
- Close PowerToys
- Extract the archive to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`
- Open PowerToys

## Usage
- Select/Place cursor where text should be pasted 
- Open PowerToys Run
- Input: "c: <search query>"
- Select the result (ENTER)
- \<text\> is pasted into the selected location

### Clear Clipboard History
To clear clipboard history you can use the following shortcut:
- "c:-"

## Configuration
The paste behaviour can be changed via the settings to either:
- Directly paste the contents (Default)
- Copy the selected item to the clipboard

# Settings Menu Setup Guide

## Scripts Created
1. **SettingsManager.cs** - Singleton untuk manage dan save semua settings
2. **SettingsUI.cs** - Script untuk UI panel settings dengan sliders

## Setup Instructions

### 1. Scene Preparation
- Create new empty GameObject di Canvas: nama `SettingsManager`
- Drag `SettingsManager.cs` ke GameObject tersebut

### 2. Settings Panel UI
Create UI hierarchy di Canvas:

```
Canvas
├── SettingsPanel (Panel)
│   ├── Header (TextMeshProUGUI)
│   ├── Content (Vertical Layout Group)
│   │   ├── MouseSensitivity (HorizontalLayout)
│   │   │   ├── Label (TextMeshProUGUI)
│   │   │   ├── Slider
│   │   │   └── ValueText (TextMeshProUGUI)
│   │   ├── MasterVolume (HorizontalLayout)
│   │   ├── MusicVolume (HorizontalLayout)
│   │   ├── SFXVolume (HorizontalLayout)
│   │   ├── WalkSpeed (HorizontalLayout)
│   │   ├── SprintSpeed (HorizontalLayout)
│   │   ├── BobSpeed (HorizontalLayout)
│   │   ├── BobAmount (HorizontalLayout)
│   │   └── Buttons (HorizontalLayout)
│   │       ├── Save Button
│   │       ├── Reset Button
│   │       └── Close Button
```

### 3. Assign to SettingsUI Script
Drag SettingsPanel dan semua sliders + buttons ke script SettingsUI:
- settingsPanel → SettingsPanel GameObject
- mouseSensitivitySlider → Slider di MouseSensitivity
- mouseSensitivityText → Value text di MouseSensitivity
- (dan seterusnya untuk semua slider)
- closeButton → Close Button
- saveButton → Save Button
- resetButton → Reset Button

### 4. Open Settings Button
Create button di UI utama untuk open settings:
- Assign button ke SettingsUI.OpenSettings() method

### 5. Hotkey (Opsional)
Sudah set Escape untuk close settings. Bisa tambah tombol lain untuk open settings.

## Settings Yang Bisa Diatur
- **Mouse Sensitivity** (0.1 - 20)
- **Master Volume** (0% - 100%)
- **Music Volume** (0% - 100%)
- **SFX Volume** (0% - 100%)
- **Walk Speed** (1 - 15)
- **Sprint Speed** (5 - 25)
- **Camera Bob Speed** (1 - 20)
- **Camera Bob Amount** (0 - 1)

## How It Works
1. SettingsManager save ke PlayerPrefs dengan JSON format
2. Saat game start, settings auto-load dari save
3. mouselook.cs dan PlayerMovement.cs dinamis mengambil settings dari SettingsManager
4. Perubahan slider langsung apply realtime
5. Save button menyimpan ke PlayerPrefs

## Example Simple UI Setup
Bisa bikin panel horizontal yang simple dengan:
- Title: "SETTINGS"
- Slider + label untuk tiap setting
- 3 Buttons: Save, Reset, Close
- Panel disabled by default, di-enable saat open settings

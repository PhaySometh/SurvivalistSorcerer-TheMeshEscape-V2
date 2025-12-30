<div align="center">

# 🧙‍♂️ Survivalist Sorcerer: The Mesh Escape

### A 3D Action-Adventure Survival Game

![Unity](https://img.shields.io/badge/Unity-2022.3.47f1-black?logo=unity&style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS-blue?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Completed-success?style=for-the-badge)

*Escape the mesh realm by defeating waves of enemies and conquering the final boss!*

</div>

> **📝 Note to Lecturers:**
> This repository is a migrated version of our [original project repository](https://github.com/PhaySometh/SurvivalistSorcerer-TheMeshEscape.git). We moved to this new `-V2` repository to resolve critical GitHub LFS (Large File Storage) bandwidth/storage limits that prevented us from pushing further updates. All final code and assets are contained here.

---

## 📥 Downloads & Demo

| Resource | Link |
|----------|------|
| 🎮 **Game Build (Windows)** | [BuiltGame/Window/](BuiltGame/Window/) |
| 🍎 **Game Build (macOS)** | [BuiltGame/MacOS/](BuiltGame/MacOS/) |
| 🎬 **Demo Video** | [DemoVideo/](DemoVideo/) |
| ☁️ **OneDrive Mirror** | [Download from OneDrive](https://cadtedu-my.sharepoint.com/:f:/g/personal/someth_phay_student_cadt_edu_kh/IgDjvu6px5liSIlpq9xknnsyAeS8cZU2rcg8fjRwjgS4fHQ?e=n9D94p) |

---

## 🚀 Installation Guide

### 🪟 Windows Installation

1. **Download** the file from `BuiltGame/Window/Survivor-Surcerer - Window.zip`
2. **Extract** the ZIP file to your preferred location
3. **Open** the extracted folder
4. **Run** `Survivalist Sorcerer.exe` to start the game
5. **Enjoy!** 🎮

> **Note:** If Windows SmartScreen appears, click "More info" → "Run anyway"

---

### 🍎 macOS Installation

The macOS build is split into 3 parts due to file size limits. Follow these steps:

1. **Download** all 3 files from `BuiltGame/MacOS/`:
   - `Survivor-Surcerer-Mac.zip.partaa`
   - `Survivor-Surcerer-Mac.zip.partab`
   - `Survivor-Surcerer-Mac.zip.partac`

2. **Combine** the files using Terminal:
   ```bash
   cd ~/Downloads  # or wherever you downloaded the files
   cat Survivor-Surcerer-Mac.zip.part* > Survivor-Surcerer-Mac.zip
   ```

3. **Extract** the combined ZIP file

4. **First Launch** - Right-click the app → "Open" → Click "Open" in the dialog
   > macOS may block the app since it's not from the App Store

5. **If still blocked**, run this in Terminal:
   ```bash
   sudo xattr -rd com.apple.quarantine "/path/to/Survivalist Sorcerer.app"
   ```

6. **Enjoy!** 🎮

---

## 📖 About The Game

**Survivalist Sorcerer - The Mesh Escape** is a third-person action-adventure survival game where players control a wizard trapped in the mysterious "Mesh Realm". 

**Objective:** Survive 5 waves of enemies and defeat the final boss to escape!

### ✨ Key Features
- 🌊 **Wave-Based Combat** - 5 progressive waves with increasing difficulty
- 🔮 **Magic Combat System** - Light spells & Heavy spells with auto-targeting
- 👾 **4 Enemy Types** - Slimes, Turtles, Skeletons, and Golems
- 🐂 **Epic Boss Fight** - Face the Bull Boss in the final battle
- ⚙️ **3 Difficulty Modes** - Easy, Medium, Hard

---

## 🎮 Controls

| Action | Key |
|--------|-----|
| **Move** | `W` `A` `S` `D` |
| **Sprint** | `Left Shift` + Movement |
| **Jump** | `Space` |
| **Crouch** | `C` |
| **Light Spell** | `Left Mouse Button` |
| **Heavy Spell** | `Right Mouse Button` |
| **Aim** | Mouse Movement |
| **Pause** | `Esc` |

---

## 👹 Enemy Types

| Enemy | Difficulty | Description |
|-------|------------|-------------|
| 🟢 Slime | ⭐☆☆☆☆ | Basic enemy, slow & weak |
| 🐢 Turtle | ⭐⭐☆☆☆ | Medium enemy with shell defense |
| 💀 Skeleton | ⭐⭐⭐☆☆ | Agile enemy with moderate damage |
| 🗿 Golem | ⭐⭐⭐⭐☆ | Tanky enemy with high health |
| 🐂 Bull Boss | ⭐⭐⭐⭐⭐ | Final boss with extreme power |

---

## 💻 System Requirements

### Minimum
- **OS:** Windows 10 / macOS 10.14+
- **Processor:** Intel Core i3 / AMD Ryzen 3
- **Memory:** 4 GB RAM
- **Graphics:** Intel HD Graphics 4000 / NVIDIA GTX 460
- **Storage:** 2 GB available

### Recommended
- **OS:** Windows 11 / macOS 12+
- **Processor:** Intel Core i5 / AMD Ryzen 5
- **Memory:** 8 GB RAM
- **Graphics:** NVIDIA GTX 1050 / AMD RX 560

---

## 🛠️ Technical Details

- **Engine:** Unity 2022.3.47f1 (LTS)
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Scripting:** C#
- **AI Navigation:** Unity NavMesh

### Project Structure
```
├── Assets/              # Unity game assets
│   ├── Scripts/         # Game logic (Player, AI, UI, Systems)
│   ├── Scenes/          # Game scenes (Menu, Loading, Gameplay, Credits)
│   ├── Prefabs/         # Player, Enemies, Effects prefabs
│   └── Audio/           # Music and sound effects
├── BuiltGame/           # Compiled game executables
│   ├── Window/          # Windows build (.zip)
│   └── MacOS/           # macOS build (split .zip parts)
├── DemoVideo/           # Gameplay demo video
├── Packages/            # Unity package manifest
└── ProjectSettings/     # Unity project settings
```

---

## 👥 Team Members

**Lecturer:** VA Hongly

| Name | Role |
|------|------|
| **PHAY Someth** | Team Leader |
| **KHUN Sophavisnuka** | Developer |
| **TET Elite** | Developer |
| **TEP Somnang** | Developer |
| **CHOENG Rayu** | Developer |

---

## 📜 License

This project was created for educational purposes as part of the Game Development course at **CADT** (Cambodia Academy of Digital Technology).

---

<div align="center">

**🎮 CADT - Game Development Course - Year 3 🎮**

*December 2025*

</div>

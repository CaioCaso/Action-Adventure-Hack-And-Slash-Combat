# Action-Adventure Hack And Slash Combat System

## Action-Adventure Hack And Slash Combat System

Watch the video to see how the combat system works:

[![Watch the combat system demo](https://img.youtube.com/vi/xjXnmePI6Bs/0.jpg)](https://www.youtube.com/watch?v=xjXnmePI6Bs&t=1s)

An advanced modular combat system built in Unity and C#, designed for action-adventure games with dynamic directional movement and precise state management.

---
<p align="center">
  Made by <a href="https://github.com/CaioCaso"><strong>Caio Caso</strong></a>
</p>


## 🎮 Key Features

* **Directional Movement & Evade:** Omnidirectional response mapping using keyboard/mouse or gamepad inputs, featuring dynamic directional evasions (Forward, Backward, Left, Right).
* **Dual-Targeting System:** Includes **Soft-Lock** (with automatic target switching and *Zanshin* / stare-down behavior after defeating targets) and **Hard-Lock** mechanics.
* **Combo Architecture:** Multi-step light attack chain, heavy attacks, and a fully realized **Charge Attack** with holding buffers and full-charge verification.
* **Gap Closer Logic:** Distance-based automatic gap closing that bridges approach velocity with combat states.
* **Robust State & Hitbox Management:** Driven by a custom state pattern (`CombatPlayerState`) interacting with modular hitboxes and a specialized 22-animation state tree.

---

## 🛠️ Technical Implementation

- **Engine:** Unity (C#)
- **Architecture:** State Pattern (`PlayerState` inheritance)
- **Input Handling:** Unity Input System integration for multi-platform support (Gamepad & Keyboard/Mouse)

---

## 📁 Repository Structure

```text
Action-Adventure-Hack-And-Slash/
│
├── Scripts/
│   ├── CombatPlayerState.cs  # Core combat state machine & logic
│   ├── HitboxManager.cs      # Hitbox activation, bone mapping & finishers
│   ├── LockOnSystem.cs       # Hard-lock, soft-lock & Zanshin state management
│   ├── Player.cs             # Player entity, damage reaction & target detection
│   └── PlayerInputManager.cs # Input system wrappers
│
└── Animations/
    ├── ISEKAIRIFT_BASE_MOVEMENT_ANIMATOR.controller # Combat Animator Controller
    └── *.anim                                      # Action, dodge & movement animations

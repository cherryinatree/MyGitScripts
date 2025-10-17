# Unity Systems Library — Modular Scripts for Rapid Prototyping & Production

A curated collection of **modular, drop-in C# systems** for Unity projects. The repo is organized by domain (Player, UI, Networking, etc.) so you can **grab only what you need** or use it as a **starter architecture** for new games.

> **Compatibility:** Unity **New Input System** 

---

## Table of Contents
- [Highlights](#-highlights)
- [Folder Map](#-folder-map)


## ✨ Highlights
- **Plug-and-play scripts** with clean inspectors and sensible defaults  
- **Action-based player architecture** (`PlayerAction` + concrete actions like movement, interact, etc.)  
- **Separation of concerns**: Player, Game Management, AI, World, UI, Data, Networking, Editor, Tools, Monetization  
- **Production-friendly**: events, ScriptableObjects (where appropriate), extension methods, and lightweight utilities  
- **Opt-in**: Import per folder or use the whole library

---

## 📂 Folder Map

1 Player System/
2 Game Management/
3 NPC and Enemy Systems/
4 World and Object Systems/
5 UI and HUD/
6 Data and Utility Systems/
7 Network and Multiplayer Systems/
8 Editor & Workflow Scripts/
9 Tools/
10 Monetization/


### 1) Player System
- **Core architecture** for player-driven behaviors.
- Base classes like `PlayerAction` and example concrete actions (movement, jumping, interaction).
- Keep the **player brain** thin and **actions modular** — enable/disable abilities by toggling components.

### 2) Game Management
General Purpose Game Management in development

### 3) NPC and Enemy Systems
- AI behaviors and utilities: perception (NN), State Machine, patrol/chase/attack states, spawning/pooling hooks.

### 4) World and Object Systems
- Environment and interactables: doors, pickups, triggers, destructibles.

### 5) UI and HUD
- UI controls and HUD widgets

### 6) Data and Utility Systems
- Cross-cutting helpers: save/load patterns.

### 7) Network and Multiplayer Systems
- Adapters and helpers to wire actions/world events to a networking layer (NGO/Mirror/custom).
- Focus on **authority, ownership, RPC/events**, and sync primitives.

### 8) Editor & Workflow Scripts

### 9) Tools
- A*, PerlinNoise, WaveFunction, Time.

### 10) Monetization
- Opt-in wrappers for IAP/ads and feature flags to keep monetization logic **decoupled** from gameplay.
- Can be **disabled entirely** without touching core systems.



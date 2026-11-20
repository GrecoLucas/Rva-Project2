# Arcane Restoration: The Forgotten City

> A VR wizard cleans a polluted, forgotten city with gesture magic and brings it back to life.

**Status:** in development · RVA (Virtual and Augmented Reality), Master at FEUP

---

## Table of contents

1. [Concept](#concept)
2. [Experience flow](#experience-flow)
3. [Magic system](#magic-system)
4. [Recycling and sorting](#recycling-and-sorting)
5. [How the city reacts](#how-the-city-reacts)
6. [Navigation](#navigation)
7. [Menus and widgets](#menus-and-widgets)
8. [Grading criteria mapping](#grading-criteria-mapping)
9. [Tech stack](#tech-stack)
10. [Planned architecture](#planned-architecture)
11. [Roadmap](#roadmap)
12. [Stretch ideas](#stretch-ideas)
13. [Scope and risks](#scope-and-risks)
14. [How to run and test](#how-to-run-and-test)
15. [Credits](#credits)

---

## Concept

The player is a wizard who arrives in a **real, modern city**: a downtown plaza with shops, a bus stop, a fountain and a canal. The city has been neglected. The streets are covered in trash, the water is murky, a cloud of smog hangs over the market, the shops are closed and nobody walks around. Everything is grey and silent.

The wizard's job is to **clean the city**. With a magic wand the player lifts trash, sorts it into the correct recycling bins, sweeps away dust and smog, and purifies the polluted water. Every item cleaned and correctly sorted raises the city's **cleanliness**. As cleanliness rises, quality of life comes back: colour returns to the facades, street lights and shop signs turn on, shops reopen, music starts playing and people come back to the plaza.

Only the wand and the magic are fantastical. The city itself is a real, contemporary one, and nature is not the main point: a few street trees and planters may perk up, but the reward is a **liveable city**, not a forest.

**Message:** recycling, environmental responsibility, hope, and the positive impact of small actions. The experience is **non-violent** by design: the wizard only cleans and sorts.

---

## Experience flow

```
Start menu
   │
   ▼
Grey, dirty, silent plaza
   │   hint boards near each zone teach its gesture
   ▼
Clean the four zones, in any order
   │   cleanliness rises → colour, lights and people return
   ▼
City 100% clean
   │
   ▼
Finale: raise the wand → wave of light → the plaza comes fully to life
   │
   ▼
End summary (items sorted, sorting accuracy, time)
```

### Plaza layout

One small, polished plaza with four zones around it. Each zone has its own set of recycling bins (an *ecoponto*).

```
        ┌──────────────────────────┐          ┌──────────────────────────┐
        │       MAIN STREET        │          │   BUS STOP & PLAYGROUND  │
        │ cans, bottles, paper,    │          │ glass bottles, cans,     │
        │ cardboard, dust          │          │ cigarette butts, graffiti│
        └────────────┬─────────────┘          └─────────────┬────────────┘
                     │        ┌──────────────────────┐      │
                     └────────│     CENTRAL PLAZA    │──────┘
                     ┌────────│ start · finale spot  │──────┐
                     │        └──────────────────────┘      │
        ┌────────────┴─────────────┐          ┌─────────────┴────────────┐
        │     FOUNTAIN & CANAL     │          │      MARKET SQUARE       │
        │ floating trash,          │          │ organic waste, cardboard,│
        │ polluted water           │          │ overflowing bins, smog   │
        └──────────────────────────┘          └──────────────────────────┘
```

| Zone | What is dirty | Main mechanics |
|---|---|---|
| **Main Street** | Litter (cans, plastic bottles, paper, cardboard), dust and grime | Telekinesis + sorting, Whirlwind |
| **Bus Stop & Playground** | Glass bottles, cans, cigarette butts, graffiti and grime on walls | Telekinesis + sorting, Purify Water (wash walls) |
| **Fountain & Canal** | Floating trash, dark polluted water | Telekinesis (fish out trash), Purify Water |
| **Market Square** | Organic waste (fruit peels, leftovers), cardboard boxes, overflowing bins, a smog cloud | Telekinesis + sorting, Whirlwind (clear smog) |

The gestures are taught by **in-world hint boards** placed at the entrance of each zone. Each board shows the gesture as a short looping animation, plus the ecopontos colour legend. There is no narrator or guide character.

---

## Magic system

The core of the experience, and the most important grading criterion (interaction, 8 pts). The player does **not** press buttons to cast spells. The **controller is the wand**: the trigger holds a spell, and the movement of the wand is the spell.

| Mechanic | Gesture | Detection (heuristic) | Effect | Feedback |
|---|---|---|---|---|
| **Telekinesis** (Levitate & Sort) | Point at an item, hold the trigger, move the wand | Ray hit on a `WasteItem` while the trigger is held | The item floats at the end of the ray and follows the wand. The thumbstick pushes it away or pulls it closer. Releasing over a bin drops it in, and a quick flick throws it | Soft rumble while holding, a floating label with the material, wand glows in the bin colour |
| **Whirlwind** | Hold the trigger and draw a circle in the air | Accumulated angle of the wand tip around its centroid (on the plane facing the player) ≥ 300°, inside a radius range and a time window | A small whirlwind sucks up scattered dust and small debris into one pile, and clears smog clouds | Haptic pulse on recognition, swirl particles, wind SFX |
| **Purify Water** | Aim at water (or a dirty wall) and hold the trigger, then sweep | Ray hit on `PollutedWater` / `DirtySurface` while the trigger is held | A cleansing stream; the hit area blends from murky to clear (shader mask / progress) | Continuous rumble, water stream particles, water SFX |
| **Finale** | At 100% cleanliness, hold the trigger and raise the wand upward | Vertical displacement of the wand above a threshold within a short time | A wave of light spreads over the plaza and the city comes fully to life | Strong haptic burst, music swell |

### Wand feedback
- **Colour:** the wand tip and its trail change colour with the active mode (telekinesis, whirlwind, water). While an item is held, the wand takes the colour of the bin the item belongs to.
- **Haptics:**
  - a short pulse when a gesture is recognized;
  - a continuous rumble while channelling;
  - a single pulse on a correct sort;
  - a double buzz on a wrong sort.
- **Sound:** each spell has its own SFX. A chime plays on a correct sort.

### Mode selection
The active mode (Telekinesis / Whirlwind / Purify Water) is chosen with a **radial selector** on the wand's thumbstick or a wrist menu. Gestures are only checked for the active mode, which keeps false positives low.

### Why simple heuristics
Gestures are detected with **simple geometric heuristics** on the wand's trajectory (accumulated angle, displacement, speed, ray hits) instead of a template recognizer or machine learning. They are predictable, easy to tune and cheap on the Quest.

---

## Recycling and sorting

Trash is sorted into the Portuguese **ecopontos**:

| Bin | Colour | Accepts | Example items |
|---|---|---|---|
| Embalagens | 🟡 Yellow | Plastic and metal packaging | Plastic bottles, cans, bags |
| Papel / Cartão | 🔵 Blue | Paper and cardboard | Newspapers, boxes, flyers |
| Vidro | 🟢 Green | Glass | Glass bottles, jars |
| Orgânicos | 🟤 Brown | Organic waste | Fruit peels, food leftovers |
| Indiferenciado | ⚫ Grey | Everything else | Cigarette butts, dirty napkins |

- Every `WasteItem` has a `WasteCategory`, and every `RecyclingBin` accepts one category.
- While an item is levitated, a **floating label** shows its name, material and the colour of the correct bin. The sorting teaches something along the way.
- **Correct bin:** the item is absorbed with a sparkle and a chime, the wand pulses, and the counter and zone cleanliness go up.
- **Wrong bin:** soft feedback, with **no penalty**. The bin spits the item back out with a red flash and a buzz, and the correct bin glows briefly.
- Sorting **accuracy** is tracked and shown in the end summary.

---

## How the city reacts

Each zone (`CleanableZone`) reports a **cleanliness** value from 0 to 1. The `CityCleanlinessManager` combines them into the city's total and raises events that the rest of the world listens to.

| Cleanliness drives… | How |
|---|---|
| **Colour** | Saturation goes from grey to full colour (post-processing volume weight / material parameter), per zone and globally |
| **Lights** | Street lights and shop signs turn on |
| **Shops** | Shutters open, awnings unfold, windows light up |
| **Air** | The smog fades and the sky clears |
| **People** | Pedestrians, cyclists and pigeons return to the plaza |
| **Sound** | Ambient city sounds and music layers fade in as the city improves |
| **Greenery** (subtle) | Street trees and planters perk up slightly, but nature stays a detail |
| **Quality of life meter** | A city-wide indicator on the HUD |

Each change happens **in real time** with particles, animations and colour transitions, so every action has a visible consequence.

---

## Navigation

- **Smooth locomotion** with the left thumbstick.
- **Snap or smooth turn** with the right thumbstick (selectable in settings).
- **Comfort vignette** while moving, which can be turned on or off.
- The plaza is bounded, so the player can't walk out of the playable area.

Built on the locomotion providers from the XR Interaction Toolkit Starter Assets.

---

## Menus and widgets

- **Start menu** (world-space): Start, Restart, Settings (turn mode, vignette, dominant hand), Quit.
- **Wrist HUD:**
  - active mode;
  - city cleanliness %;
  - items sorted;
  - sorting accuracy %;
  - per-zone progress.
- **Hint boards:** one per zone, showing the gesture and the ecopontos legend.
- **End summary:** total items sorted, accuracy, time, and a before/after of the plaza.

Built with world-space uGUI canvases + XR Interaction Toolkit UI interaction.

---

## Grading criteria mapping

| Criterion | How the project answers it |
|---|---|
| **Interaction (8 pts)** | Gesture-based magic instead of buttons: telekinesis, circle whirlwind, aim-and-sweep water stream, raise-the-wand finale. Sorting with feedback. Haptics on the controller |
| **Modeling & Animation (5 pts)** | Trash items, ecopontos, water, shops, street lights and pedestrians (Asset Store, credited). Grey→colour transformation, opening shutters, returning people, all reacting to the player |
| **Creativity (5 pts)** | Magic in a real modern city, combined with a recycling message. The before/after of the plaza is the visual highlight |
| **Navigation** | Smooth locomotion + snap/smooth turn, comfort vignette |
| **Menus & widgets** | Start menu, wrist HUD, hint boards, end summary |

---

## Tech stack

| | |
|---|---|
| Engine | Unity **6000.4.0f1** |
| Render pipeline | URP |
| XR | XR Interaction Toolkit 3.4.1, OpenXR (Meta Quest feature), XR Hands (installed, not used for spells) |
| UI | uGUI + TextMeshPro |
| Target device | **Meta Quest 3 / 3S** (Android build) |
| Input | Quest Touch controllers (controller = wand) |

---

## Planned architecture

```
Assets/_Project/
├── Scenes/            # Main plaza scene, menu
├── Scripts/
│   ├── Core/          # Game state, events, settings
│   ├── Magic/         # Wand, gestures, spells
│   ├── World/         # Zones, waste, bins, water, city reactions
│   └── UI/            # Menus, HUD, hint boards
├── Prefabs/
├── Art/               # Imported Asset Store packs (kept separate and credited)
├── Materials/
└── Audio/
```

Key components:

| Component | Responsibility |
|---|---|
| `Wand` | Tip position, active mode, colour and trail |
| `GestureTracker` | Records the wand-tip trajectory while the trigger is held and runs the heuristics (circle, raise) |
| `TelekinesisController` | Lifts, holds (spring/damper follow), pushes/pulls and throws `WasteItem`s |
| `WhirlwindSpell` | Pulls dust and debris into a pile and clears smog |
| `WaterPurifySpell` | Stream emission and cleaning of `PollutedWater` / `DirtySurface` |
| `WasteItem` | Holds its `WasteCategory`, display name and label |
| `RecyclingBin` | Accepts or rejects items and triggers the feedback |
| `CleanableZone` | Tracks the cleanliness of one zone |
| `CityCleanlinessManager` | Combines the zones and raises events for colour, lights, people and audio |
| `GestureHintBoard` | Shows the looping gesture demo for its zone |
| `HapticsService` | Haptic patterns (pulse, rumble, double buzz) |
| `AudioDirector` | Music and ambient layers driven by cleanliness |

---

## Roadmap

> ⚠ **Confirm the delivery date with the professor.** The assignment PDF says 18-Dec-2025, which is probably a typo.

### Phase 0: Setup
- [ ] Quest 3 build working (Android profile, Meta OpenXR)
- [ ] Testing without a headset through the XR Interaction Simulator
- [ ] `_Project` folder structure
- [ ] Greybox plaza with the four zones and the player rig

### Phase 1: Telekinesis and sorting (core loop)
- [ ] `Wand` with its modes, colour and trail
- [ ] `TelekinesisController`: lift, follow, push/pull, release and throw
- [ ] `WasteItem` + `WasteCategory` + floating label
- [ ] `RecyclingBin` with correct and wrong feedback (VFX, SFX, haptics)
- [ ] One zone playable end to end

### Phase 2: Spells and zones
- [ ] `GestureTracker` + Whirlwind (circle) with dust piles and smog clearing
- [ ] Purify Water stream + water / dirty-wall shader with a clean mask
- [ ] Mode selector (radial or wrist)
- [ ] `CleanableZone` + `CityCleanlinessManager` with progress events
- [ ] All four zones greyboxed and playable

### Phase 3: Art pass
- [ ] Stylized low-poly **modern city** assets: buildings, shops, street props, bus stop, fountain, canal
- [ ] Trash items and ecoponto models
- [ ] Grey→colour transformation, lights, shop shutters, clearing smog
- [ ] Pedestrians, cyclists and pigeons returning (animated)

### Phase 4: Experience and UI
- [ ] Gesture hint boards per zone
- [ ] Finale gesture and the wave of light
- [ ] Start menu, settings and wrist HUD
- [ ] Music and ambient layers driven by cleanliness
- [ ] End summary screen

### Phase 5: Polish and delivery
- [ ] Quest performance: draw calls, baked lighting, LODs, object pooling (stable 72+ fps)
- [ ] Tune gesture thresholds and telekinesis feel through playtests
- [ ] Bug fixing
- [ ] Report, credits and video

---

## Stretch ideas

- Short **recycling facts** on the item labels (e.g. "glass can be recycled endlessly").
- **Pollution sources** to shut off: a leaking pipe in the canal, a smoking chimney near the market.
- A **timed challenge mode** with a score.
- A **day/night cycle** linked to progress.
- **Magic flight** as an extra navigation mode.
- A **hand-tracking** variant of the gestures.

---

## Scope and risks

**One small, polished plaza beats a large, empty city.**

| Risk | Mitigation |
|---|---|
| Gestures with false positives or misses | Only check the gestures of the active mode, tune thresholds through playtests, give clear haptic and visual feedback |
| Telekinesis that feels floaty or hard to control | Spring/damper follow, push/pull on the thumbstick, a magnetic intake zone on the bins |
| Performance on the Quest | Low-poly assets, baked lighting, pooling, keep an eye on draw calls from the start |
| Inconsistent art style | Stick to stylized low-poly packs from a small number of authors |
| Scope creep | Finish the core loop (Phase 1) before adding content; everything else goes to stretch ideas |

---

## How to run and test

### Requirements
- Unity **6000.4.0f1** with the Android Build Support module.
- A Meta Quest 3 / 3S for testing on device. Headsets can be booked at the GIG: <https://s.up.pt/rucw> (mention you are an RVA student).

### In the Editor, without a headset
The project uses the **XR Interaction Simulator** (XR Interaction Toolkit sample), which is added automatically in Play mode.

- Enabled in `Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset`.
- **Initialize XR on Startup** is off for Windows/Standalone (Project Settings → XR Plug-in Management), so the Editor doesn't look for a headset. Turn it back on to test with a headset connected to the PC (Quest Link).

Controls:

| Action | Input |
|---|---|
| Move | `W` `A` `S` `D` |
| Down / up | `Q` / `E` |
| Look around | Hold the right mouse button + move the mouse |
| Other bindings (switch between headset and controllers, trigger, grip) | Shown in the on-screen panel |

### On the Quest
1. Switch the build profile to **Android**.
2. Make sure **OpenXR → Meta Quest** is enabled in XR Plug-in Management (Android tab).
3. Connect the headset over USB (developer mode on) and choose **Build And Run**.

---

## Credits

Third-party assets used in the project:

| Asset | Author | Link | License |
|---|---|---|---|
| *TBD* | | | |

### Team

*Team members: TBD*

Course: **RVA (Virtual and Augmented Reality)**, Master at FEUP.

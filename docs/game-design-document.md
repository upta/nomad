# Project Blueprint: Nomad

## 1. Overall Goal

Nomad is a cooperative, top-down 2D sci-fi survival game for up to 8 players. Players act as a highly interdependent crew piloting a modular gravity ship fleeing an encroaching galactic cataclysm known as **The Stellar Night**. The core experience blends localized crew-management tension with high-stakes route navigation, played entirely in real-time co-op from a flat, top-down perspective.

The primary design goal is to create systemic tension through pure logistics, resource allocation, and team organization, completely eliminating direct character-vs-monster combat.

## 2. Game Start & Progression

### 2.1 Hull, Room Selection, & Upgrades

Before launching a run, the crew collaborates at the terminal setup phase to configure their mobile colony:

- **Hull Template Selection:** The team selects a base starship hull shape. The hull determines the total grid footprint, structural armor, and the specific number of open slot sockets available for modular components.
- **Modular Room Slotting:** Players choose which pre-defined, specialized room types to drop into the hull's sockets (e.g., opting for an expanded Hydroponics room over an extra Workshop room). This choice establishes the ship's baseline capabilities and physical floor plan.
- **System Upgrades:** While not fully fleshed out for the initial prototype, the ship's rooms can be upgraded over the course of a campaign. By using scrap material, specialized components, or trading at stations, the crew can upgrade existing rooms to improve processing speed, power efficiency, or maximum threshold capacities.

### 2.2 Character Specializations (Conceptual / Non-Final)

Every player selects a designated role prior to deployment. Specialization alters the group dynamic by granting targeted functional bonuses to core systems without applying artificial penalties to basic player movement or interaction. These roles serve as initial gameplay concepts and are subject to change during playtesting:

- **Engineer:** Enhances machine repair speed and breaker stability.
- **Scientist:** Speeds up short-range node scans and long-range probe compilation.
- **Chef:** Generates high-efficiency food meals from raw station yields to optimize metabolic meters.
- **Pilot:** Maximizes ship evasion rates or reduces jump-drive warm-up times.
- **Logistics:** Features an expanded hotbar capacity to carry more raw materials or cargo crates at once.
- **Tactical:** Boosts the targeting speed, accuracy, or cooling rates of automated defense systems.
- **Doctor:** Accelerates crew healing rates and treats advanced physical injuries or environmental afflictions.
- **Diplomat / Merchant:** Unlocks better resource conversion rates and unique interaction choices at trading hubs.

## 3. The Core Loop

The gameplay experience repeats a tight, real-time cycle centered around navigation and node interaction:

1. **Scan & Vote:** The crew uses the Bridge Star Chart to evaluate adjacent sectors, balancing potential resource payouts against the physical distance of the creeping Stellar Night.
2. **Jump:** The ship jumps to the selected coordinate and anchors.
3. **Engage Node Activities:** The crew coordinates to tackle whatever specific opportunities or hazards are present at that unique node location.
4. **Escape:** As the real-time boundary of the Stellar Night approaches, the crew must secure all vital assets on board and execute a jump before the node becomes completely frozen and lethal.

## 4. Node Variance & Sample Activities

Nodes feature structural and environmental diversity. Different nodes have entirely different hazards, resources, and events available. Instead of a rigid phase split, players organically self-organize based on the immediate demands of their current node:

- **Planetside Resource Harvesting:** The ship lands on a physical planetary surface grid. Players leave the airlocks to manually mine raw minerals, siphon fuel deposits, or gather planetary biomass directly into their hotbars.
- **Abandoned Wreck Salvage:** The ship docks with a derelict space structure. Crew members cross over to explore dead corridors, searching for rare components or abandoned supplies to haul back to cargo bay storage.
- **Trading Posts:** Safe-zone nodes populated by neutral factions, automated depots, or passing merchant vessels. Here, the crew can take a breath to exchange excess raw materials for scarce resources, purchase packaged food or fuel, or acquire critical room upgrades.
- **Automated System Defense:** Incoming local wildlife swarms or volatile cosmic phenomena track the ship's signature. The ship's point-defense weapons engage these threats automatically, but they act as massive resource sinks. Players must spend this time rushing to load fresh ammunition boxes into the weapon assemblies and adjusting breaker power routing to keep the guns online.
- **Core Maintenance & Triage:** Inside a quiet node, the crew takes the opportunity to refine stockpiled ore at workshop benches, cook long-term rations, patch slow hull breaches, or restore power to auxiliary systems.

## 5. Primary Verbs (The Core Actions)

If an individual feature idea does not directly enhance or serve one of these five verbs, it is cut from production to preserve scope.

- **Move:** Navigate characters across the flat 2D grid interior of the ship and exterior environments.
- **Harvest:** Stand next to resource nodes (ore veins, wreckage debris) and interact to extract raw elements directly into the hotbar.
- **Refine/Craft:** Interact with automated workshop benches to combine raw elements into consumables (Ammunition Crates, Fuel Cells, Biomass, Probes).
- **Toggle:** Physically flip interactive wall-breaker switches to cut or route power to entire rooms during a ship crisis.
- **Load:** Interact with system structures to deposit items directly from the hotbar (e.g., inserting ammo into weapons, fuel into reactors, or biomass into cloning bays).

## 6. Mechanics & Features

### 6.1 Inventory & Logistics

- **Strict Hotbar Limit:** Players possess a fixed-size hotbar with zero hidden back-end inventory or backpack capacity.
- **Logistical Weight:** Every item — whether a basic hand tool, raw chunk of iron ore, or a heavy ammunition box — occupies exactly one standard slot. Because carrying capacity is severely limited, the physical logistics of moving items throughout the grid carries massive operational importance, requiring direct teamwork to stock systems or secure cargo quickly.

### 6.2 Life Support & Sustenance

- **The Oxygen Tether:** Characters possess a compact personal oxygen tank that constantly depletes when exposed to vacuums, unpressurized hull breaches, or toxic environments. Standing inside a fully pressurized, powered room automatically refills the tank. Spacesuits can be equipped to expand tank capacity at the cost of baseline movement speed.
- **The Food Reserve:** A secondary, slower-burning hunger meter representing individual metabolic strain. Must be filled periodically via processed meals or emergency rations.

### 6.3 Room-Centric Ship Systems

- **Abstract Grid Routing:** Power and atmosphere are calculated strictly at the individual room level (e.g., Reactor Room, Bridge) rather than through micro-managed physical tile wiring networks.
- **Physical Master Breakers:** Every room contains an interactable manual breaker switch. Triaging the ship's limited energy grid requires players to physically sprint through corridors to flip switches off for non-essential compartments (like the Kitchen) to keep power flowing to critical systems.

### 6.4 Death, Damage, & Ghost Spectating

- **Health & Hazards:** Characters take environmental damage from suffocating due to oxygen loss, starving, standing too close to active fires, or being attacked by hostile localized creatures during away operations.
- **Cloning Bay Respawning:** Character death is non-terminal but carries severe logistical consequences. Dead crew members are remade at the ship's central Cloning Bay node at the expense of rare, finite Biomass resources.
- **Ghost State:** If the ship lacks the power required to run the Cloning Bay, or if the crew's Biomass pool reaches zero, deceased players enter an intangible Ghost Mode. Ghosts can float unrestricted across the map to act as scouts or lookouts, but cannot interact with physical objects until the crew harvests enough resources to power a respawn.

### 6.5 Navigation & The Stellar Night

- **Dynamic Fog (Star Chart):** The Bridge map console only reveals immediate adjacent paths clearly. Distant sectors are heavily obscured by environmental noise, displaying ambiguous tags like "Unidentified Signal" rather than specific resource data.
- **Long-Range Probes:** The crew can spend valuable metal composites to craft disposable physical probes. Launching a probe from the Bridge console completely clears the fog of war on targeted distant nodes to allow strategic route mapping.
- **Real-Time Threat Wall:** The Stellar Night moves constantly across the Star Chart. Staying too long at a single node allows the darkness to swallow the coordinate, immediately doubling the ship's power consumption and inflicting continuous structural damage to exterior systems.

## 7. Aesthetics & UI

### 7.1 Visual Style

The game utilizes a top-down, clean 2D vector style heavily inspired by **RimWorld**. Characters are rendered as flat, simple cohesive sprites. To maintain a realistic, low-friction production pipeline, the project explicitly rejects complex frame-by-frame sprite sheets and independently rotating skeletal joint animations. Characters move, tilt, and slide as a single piece, prioritizing readable mechanics over complex anatomical articulation.

### 7.2 Interface Paradigm

- **Contextual Overlay Model:** The core screen space remains completely un-cluttered, rendering only minimalist personal vital bars (Health, Hunger, Oxygen) and the player hotbar along the perimeter.
- **Terminal Interaction:** Intricate configuration systems — such as the Power Grid Router, Star Chart, or Fabricator Queue — do not utilize persistent floating menus. They are accessed exclusively by walking a character up to a physical terminal in the world and interacting with it, which initializes a clean, focused display overlay.

## 8. Project Exclusions (Out of Scope)

- **Direct Character Combat:** No handheld firearm systems, melee weapons, enemy combat AI pathfinding for tactical skirmishes, or player-aimed directional shooting mechanics. All ship defense is abstractly automated via automated defense systems.
- **Tile-by-Tile Structure Building:** No placing individual wall tiles, routing custom floor wiring paths, or laying manual pipe networks tile-by-tile during gameplay. The floorplan is hard-baked directly into the chosen hull template and its preset room boundaries.
- **Persistent Character Archiving:** Characters belong to a single run only. No multi-session persistent leveling data, permanent stat saving across separate distinct runs, or global meta-progression frameworks. Every jump campaign begins with a completely fresh, baseline crew state.

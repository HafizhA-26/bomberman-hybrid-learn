
# Bomberman Smart Agent

<div align="center">
  <img src="https://github.com/HafizhA-26/hh-portfolio-assets/blob/733783024673986dcb5ae56980e701b3ecee3842/BombermanPortoAssets/Thumbnail.gif?raw=true" alt="ML-Agent against Rule-Based Agent" width=90% height=auto>
</div>


A 3D Bomberman environment built in Unity demonstrating advanced Reinforcement Learning architectures. This project implements a **Proximal Policy Optimization (PPO)** agent trained via Environment-Based Curriculum Learning and Domain Randomization. Rather than memorizing static layouts, the agent is forced to adapt through three phases of escalating spatial complexity-scaling from two to six distinct arena topologies—while competing against procedurally randomized rule-based opponents. The result is a robust, highly adaptive AI capable of dynamic spatial reasoning and real-time asymmetric self-play.

<hr>

## Tech Stack & Features

- **Engine:** Unity 3D 6000.0.78f1

- **AI Framework:** Unity ML-Agents 4.0.3 

- **Architecture:** State-Machine based entity controller, Decoupled Grid System.

- **Features:** Classic Bomberman Game Mechanic, Interactive Playable Environment, Training Agent Environment, & Simple Leaderboard Data Integration.

## Architecture

### 1. Simple State Machine
![Agent State Machine Diagram](https://github.com/HafizhA-26/hh-portfolio-assets/blob/ac75ee0f9a93cd9a83d3f3c8a8dbb7ad0e6f9fee/BombermanPortoAssets/AgentFSM.png?raw=true)

AI agent in this project uses a simple state machine via EntityState. EntityState will prevent conflicting logic when a certain condition happens to the Agent (such as overlaping animation, training reset bug, etc). Because of the simplicity the agent logic in this project, the State Design pattern does not really need to be implemented right now. 

### 2. Core Mechanic Architecture

For handling each arena, these scripts are mainly responsible to handle the core Bomberman mechanic:

- `MatchDirector.cs`: Orchestrates a full match/episode from start to finish. It builds the level, wires every entity's move and bomb-placement requests to `GridStateManager` for validation, listens for bomb and explosion events from `BombManager` to resolve prop destruction and character deaths, tracks win conditions, and either auto-resets the arena for another training episode or ends the session for player-vs-agent play. Effectively the top-level conductor tying the level, grid state, bomb system, and characters together into one running match.

- `GridStateManager.cs`: The single source of truth for the arena's logical grid state. It tracks tile types and substates, every entity's current grid position, active bombs, and destructible props, and exposes the queries and mutations that MatchDirector and the AI agents rely on: movement validation, bomb placement and blast propagation, respawn tile selection, and building the localized observation snapshot (`GameplayState`) fed to both the rule-based and ML agents. All coordinates are logical (row/column); it also handles conversion to world space for anything that needs it.

- `LevelBuilder`: Procedurally constructs the arena at match start from a `LevelTilemapData` asset. It instantiates the floor grid and the gameplay objects (walls, crates, player, enemies) at their correct positions, converting grid coordinates to world space along the way, and hands off the resulting object/tile-state grids to `GridStateManager` for logical tracking. Runs once per match via `MatchDirector.StartMatch`.

- `BombManager`: Owns the pooled bomb and explosion visual objects and manages their lifecycle. It spawns bombs on request, reuses pooled instances instead of instantiating repeatedly, and relays each bomb's detonation, tick, and finish events up to `MatchDirector` — tagged with the placing entity and the affected grid positions — since `BombHandler` itself only knows about visuals and timing, not game logic.

## AI Training & Reward Function

<div align="left">
  <img src="https://github.com/HafizhA-26/hh-portfolio-assets/blob/ac75ee0f9a93cd9a83d3f3c8a8dbb7ad0e6f9fee/BombermanPortoAssets/TrainingThumbnail.gif?raw=true" alt="Training phase ML-Agent against Rule-Based Agent" width=80% height=auto>
</div>

### 1. Observation Space

Both Rule-based and ML-Agent could observe the gameplay via `GameplayState`. The only difference is Rule-Based agen use it directly as its rule preferences, while ML-Agent must convert it first to more observable format and decide its action based on it. In General `GameplayState` consists of:

| Attribute | Format | Description |
| --------- | ------ | ----------- |
| Observation Radius | `int` | Radius around the agent to be observed |
| Entity Position | `GridPos` | Current agent's tile position in grid space. |
| Player Position | `GridPos` | Current agent's enemy tile position in grid space. |
| Nearby Condition | `Dictionary<GridPos, TileState>` | All observed tiles position and condition/state.
| Bomb Timer Norm | `Dictionary<GridPos, float>` | All nearby bombs position and its normalized timer (0 at placement - 1 at detonation).

Meanwhile, ML-Agent will format above parameter first to its observable format like the illustration below


<div align="left">
  <img src="https://github.com/HafizhA-26/hh-portfolio-assets/blob/45aab707a27035a5ef2a08ea032e722e3a50220f/BombermanPortoAssets/MLObservation.png?raw=true" alt="ML-Agent observation" width=80% height=auto>
</div>


### 2. Action Space

Both Rule-based and ML-Agent have the same action space using `ActionType` such as below

| Type | Value |
|------ |------ |
| Idle | 0 |
| MoveUp | 1 |
| MoveDown | 2 |
| MoveLeft | 3 |
| MoveRight | 4 |
| PlaceBomb | 5 |

### 3. Reward Function

For ML-Agent, the reward below is the most optimal reward function I found when fine-tuning the reward while observering the agent's behavior results.

| Condition | Reward Value | Description |
| ----------- | ----------- | ----------- |
| Step Penalty | -0.003 | Penalty for each step alive, to encourages agent playing efficiently.
| Destroy a Prop | +0.1 | Reward for destroying a destructible prop, currently only *Crate* that able to destroy.
| Normal Kill | +4 | Kill agent's enemy by agent's own bomb.
| Friendly Fire | -0.5 | Kill agent's friend (same CharacterType characters in same arena) by agent's own bomb
| Normal Dead | -1 | Dead by enemy's bomb explosion |
| Suicide Dead | -4 | Dead by own bomb explosion |
| Place Bomb | +0.01 | Small reward for placing a bomb at allent |
| Good Bomb Placement | +0.15 | Reward for placing a bomb near the enemy (`<= Offensive Distance`) |
| Bumped Move | -0.02 | Penalty for forcing movement action through a non-Walkable tile more than 2 times |
| Invalid Bomb Placement | -0.01 | Penalty for forcing place a bomb when agent's bomb count is 0 |


## How to Run Locally



## Let's Fight the Agent Here!

[Playable Link](https://bomberman-smart-agent.pages.dev)

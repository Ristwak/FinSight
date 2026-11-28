# 📈 FINSIGHT — Financial Simulation Game

**Developed in Unity | Created and Owned by Ristwak Pandey**

## 🧠 Overview

FINSIGHT is a scenario-based financial decision-making game designed for MBA and postgraduate students.

The game immerses players in realistic business and financial situations where they must make strategic decisions under pressure. It helps develop an understanding of financial statements, risk management, profitability analysis, and business decision-making through interactive gameplay.

FINSIGHT combines financial learning with time-based challenges, dynamic scenarios, and interactive visual feedback.

## 🎯 Learning Objectives

- Analyze businesses using financial ratios and performance metrics.
- Understand the impact of business decisions on profitability.
- Learn to navigate financial crises and market uncertainties.
- Experience time-bound financial decision-making.
- Interpret cumulative profit trends through real-time graph visualization.

## 🕹️ Gameplay Mechanics

### ✅ Core Gameplay Loop

1. Select a financial scenario, such as VC Investment or Consumer Market.
2. Progress through four levels within each scenario.
3. Answer five questions in each level.
4. Evaluate financial information and choose the most appropriate decision.
5. Earn profits for correct answers; incorrect answers result in zero gain.
6. Complete the scenario within a global timer of approximately 10–12 minutes.

### ⚠️ Crisis Mode — 15% Trigger Probability

Crisis Mode introduces unexpected financial challenges during gameplay.

- Randomly triggered after levels.
- Features rapid-fire True/False questions.
- Profit decreases over time, rewarding quick and accurate decisions.
- Crisis-related profit changes are reflected in the cumulative profit graph.

## 📈 Features

- 📊 **Dynamic Profit Graph:** Real-time visualization of cumulative profit.
- 💥 **Random Crisis Events:** Unexpected financial challenges and rapid decisions.
- ⏱️ **Time-Based Gameplay:** Global scenario timer with pause and resume support.
- 🧠 **Data-Driven Questions:** Financial scenarios and questions loaded dynamically through JSON files.
- 🎮 **Interactive Game Flow:** Transitions between Home, Gameplay, and Crisis interfaces.
- 🎨 **UI and Visual Feedback:** Interactive interface designed to support financial learning and engagement.

## 🔧 Technology Stack

- **Game Engine:** Unity
- **Programming Language:** C#
- **UI Framework:** Unity UI and TextMeshPro
- **Data Management:** JSON-based dynamic data loading
- **Graph Visualization:** LineRenderer
- **Gameplay Architecture:** Modular scenario and level handling
- **Game Flow:** Scene-based navigation and panel transitions

## 👨‍💻 Ownership and Intellectual Property

FINSIGHT is an independently owned game project created by **Ristwak Pandey**.

I am the creator and owner of the original FINSIGHT game, including its original source code, gameplay logic, game systems, architecture, design, and original project content.

All intellectual property rights associated with my original work in this project are retained by me. No company, employer, or other organization is represented as the owner of this project through this repository.

Third-party software, libraries, packages, fonts, and assets, where used, remain subject to their respective licenses and ownership terms.

## 📌 Project Information

- **Project Name:** FINSIGHT
- **Project Type:** Educational Financial Simulation Game
- **Target Audience:** MBA and Postgraduate Students
- **Engine:** Unity
- **Language:** C#
- **Project Status:** Developed in Unity

---

# Technical Review — Gameplay Progression and Event Handling

## 1. Overview

This update focuses on improving game progression, crisis and war event handling, borrowing mechanics, and callback management.

The objective is to make gameplay transitions more reliable, prevent duplicate event triggers, preserve player answers during borrowing, and ensure the game can recover correctly after a failed war event.

## 2. Changes Implemented

### A. LevelLoader.cs — Gameplay and Borrowing

- Added pending-answer state management to preserve the selected answer while the player is borrowing.
- Separated answer processing into `CompletePendingAnswer()`.
- Ensured streak calculations happen after the answer is fully processed.
- Updated borrowing completion logic to resume the pending answer.
- Added game-over cleanup for pending answer and callback state.
- Improved consistency between player balance, bank balance, and UI updates.

### B. CrisisManager.cs — Crisis Event Handling

- Improved crisis completion and callback handling.
- Added callback cleanup to prevent repeated invocation.
- Connected crisis completion with the borrowing workflow.
- Improved handling of missing LevelLoader references.
- Ensured the crisis event can complete without leaving the navigation flow blocked.

### C. WarManager.cs — War Event Handling

- Improved war completion and failure handling.
- Added cleanup of the war completion callback.
- Updated failed-war handling to reset the game and resume the new game flow.
- Improved handling of missing UI references and event data.
- Maintained the streak-based war survival condition.

### D. DynamicLevelUI.cs — Navigation and Event Transitions

- Prevented duplicate crisis and war event triggers.
- Added transition-state checks.
- Updated crisis callback handling to avoid triggering the borrowing panel twice.
- Improved navigation recovery after event completion.

### E. GameLoader.cs

- No changes required in this update.
- Existing scenario loading and question distribution logic remains unchanged.

## 3. Gameplay Flow Improvements

The updated flow is designed to handle these situations:

1. Player selects an answer.
2. The answer is evaluated and the balance is updated.
3. If the balance becomes negative, borrowing is triggered.
4. Once the balance is recovered, the pending answer is processed.
5. The result panel and streak calculation are updated.
6. Crisis events return control to the normal gameplay flow after completion or borrowing.
7. War success continues gameplay, while war failure resets the game and starts a fresh progression flow.

## 4. Error Handling and Reliability

The changes introduce or improve:

- Null-reference checks for important UI and manager references.
- Callback cleanup after event completion.
- Protection against repeated event triggers.
- Handling of missing crisis question and war event data.
- Cleanup of pending gameplay state during game-over and reset conditions.

## 5. Validation Status

**Code review:** Changes reviewed from the repository diff.

**Unity compilation:** Not independently verified.

**Runtime testing:** Not performed.

The following scenarios should be considered important validation cases:

- Normal question completion.
- Incorrect answer resulting in negative balance.
- Successful borrowing and answer continuation.
- Bank depletion and game-over handling.
- Crisis completion with and without borrowing.
- War success and war failure.
- Game reset followed by fresh level progression.

## 6. Overall Assessment

The update addresses several gameplay-flow issues involving asynchronous event callbacks, borrowing transitions, and game progression.

The changes aim to improve state consistency and prevent the game from becoming stuck after crisis or war events.

Further runtime validation is still required before considering the implementation fully verified.

---

**Review Status:** Code changes prepared; runtime validation pending.

**Repository:** FinSight

**Affected Components:** LevelLoader, CrisisManager, WarManager, DynamicLevelUI

---

**Created and Owned by Ristwak Pandey.**

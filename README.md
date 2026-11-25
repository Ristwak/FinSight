# Technical Review — FinSight

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

**Code review:** Performed against the supplied script versions and the proposed final changes.

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

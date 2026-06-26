Issues:
- Some assertions are being used purely to learn when/why something is null. These will almost certainly fail when Hide and Seek does nothing wrong at some point. Remove before release.
- Find and fix the problem where Rain Meadow switches to challenge mode in arena if the tab name has a ' in it.
- Sometimes the game mode time lives longer than the lobby (reference from OnlineManager.lobby). This happened once from a kick.
- Configurables in the lobby UI do not save.
- Figure out when and why GetOnlineCreature() returns null. Currently some code relies on it not returning null.
- The Hide and Seek tab has UI elements which poorly overlap. It needs to be redesigned.
- The current way to switch teams via hotkeys breaks some logic as the player is left in the initial seeker list but not the seeker list. There needs to be a special list and RPCs for this debugging tool.
- Seekers and initial seekers need to be reselected every arena session end.

Features:
- Change Rain Meadow's scoring to reflect seeker and hider scores.
- Implement all tag results.
- Seekers should not be able to see, move, or tag during the safety timer. (look in GameplayOverrides.cs something like HaltPlayerMovement)
- Hiders can change their colors during the safety timer.
- Hiders may not hide inside walls. (move them to the last airpocket if in wall for too long)
- Consider some way to prevent stun locking of seekers.
- Hiders and seekers should have different banned slugcats.
- Protect against all weapon friendly fire.
- Prevent the host from starting the game when, for example, no one is willing to seek or when no seeker is selected (on other settings).
- Add support for story.

Refactors:
- Update Options class design after making story and arena game modes.
- Figure out naming of RPCs and how they should behave.
- Decide on how to abbreviate types like OnlineCreature.

Misc:
- Write external arena documentation for Rain Meadow after finishing.
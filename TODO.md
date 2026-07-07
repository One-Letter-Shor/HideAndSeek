Fixes:
- Some assertions are being used purely to learn when/why something is null. These will almost certainly fail at some point even if Hide and Seek does nothing wrong. Remove before release.
- Find and fix the problem where Rain Meadow switches to challenge mode in arena if the tab name has a ' in it.
- Sometimes the game mode time lives longer than the lobby (reference from OnlineManager.lobby). This happened once from a kick.
- Configurables in the lobby UI do not save.
- Figure out when and why GetOnlineCreature() returns null. Currently some code relies on it not returning null.
- The Hide and Seek tab has UI elements which poorly overlap. It needs to be redesigned.

Features:
- Add support for story.
- Implement all tag results.
- Seekers should not be able to see, move, or tag during the safety timer. (look in GameplayOverrides.cs something like HaltPlayerMovement)
- Hiders should be able to change their colors in game. For balancing purposes they should only be able to change colors during fixed times. Perhaps during the hiding timer works well.
- Hiders may not hide inside walls. (move them to the last airpocket if in wall for too long)
- Consider some way to prevent stun locking of seekers.
- Hiders and seekers should have different banned slugcats.
- Protect against all weapon friendly fire.
- Seekers and initial seekers need to be reselected every arena session end.
- Sort infected seekers by order of infection.
- Add some addition scoring for hiders that live long.
- Add chat messages when tagged.
- 1/10000 chance to explode when hit by rock (which results in tag). Make custom chat message too.

Refactors:
- Update Options class design after making story and arena game modes.
- Figure out naming of RPCs and how they should behave.
- Decide on how to abbreviate types like OnlineCreature.

Testing:
- Make clients send RPCs incorrectly to ensure the host handles them correctly.
- Play with high ping to ensure code is reliable.
- Ensure there is not too much network usage. A lot of lobby data fields can easily be optimized to bytes and ushorts.
- Ensure the scoring for seekers and hiders is balanced.

Misc:
- Write external arena documentation for Rain Meadow after finishing.
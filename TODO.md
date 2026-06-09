Issues:
- Some assertions are being used purely to learn when/why something is null. These will almost certainly fail when Hide and Seek does nothing wrong at some point. Remove before release.
- Find and fix the problem where Rain Meadow switches to challenge mode in arena if the tab name has a ' in it.
- Update Options class design after making story and arena game modes. 
- Figure out naming of RPCs and how they should behave.
- Decide on how to abbreviate types like OnlineCreature.
- Sometimes the game mode time lives longer than the lobby (reference from OnlineManager.lobby). This happened once from a kick.
- Change Rain Meadow's scoring to reflect seeker and hider scores.
- Configurables in the lobby UI do not save.
- Figure out when and why GetOnlineCreature() returns null. Currently some code relies on it not returning null.

Features:
- Implement all tag results.
- Implement all seeker selections.
- Seekers should not be able to see, move, or tag during the safety timer.
- Hiders can change their colors during the safety timer.
- Hiders may not hide inside walls. (move them to the last airpocket if in wall for too long)
- Consider some way to prevent stun locking of seekers.
- Consider some way to prevent hiding in excessively offscreen/foreground covered areas.
- Hiders and seekers should have different banned slugcats.
- Add additional color picker for seekers. (modify Rain Meadow)
- Seeker colors must be clamped to high saturation.
- Add support for story.

Misc:
- Write external arena documentation for Rain Meadow after finishing.
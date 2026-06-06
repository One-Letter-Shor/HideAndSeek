Issues:
- Find and fix the problem where Rain Meadow switches to challenge mode in arena if the tab name has a ' in it.
- Update Options class design after making story and arena game modes. 
- Figure out naming of RPCs and how they should behave.
- Decide on how to abbreviate types like OnlineCreature.
- Use .isAvatar checks when dealing with players (such as seeker checks).
- Sometimes the game mode time lives longer than the lobby (reference from OnlineManager.lobby). This happened once from a kick.
- Change Rain Meadow's scoring to reflect seeker and hider scores.

Features:
- Seekers should not be able to see during the safety timer.
- Seeker colors must be fairly saturated and fairly light. (high contrast)
- Hiders can change their colors during the safety timer.
- Hiders may not hide inside walls. (move them to the last airpocket if in wall for too long)
- Consider some way to prevent stun locking of seekers.
- Consider some way to prevent hiding in excessively offscreen/foreground covered areas.
- Consider reporting mods such as DMS and SB Camera Scroll.
- Hiders and seekers should have different banned slugcats.
- Add support for story.

Misc:
- Write external arena documentation for Rain Meadow after finishing.
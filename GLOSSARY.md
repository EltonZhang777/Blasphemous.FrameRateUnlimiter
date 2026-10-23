# Frame Rate Policy

This context describes the player-configured frame-rate behavior for Blasphemous 1.

## Language

**Fixed frame-rate target**:
A single FPS value requested by the policy. It does not guarantee that the game will render at that rate.
_Avoid_: guaranteed FPS, minimum actual FPS

**Variable frame-rate interval**:
An inclusive lower-to-upper range of requested FPS targets that adapts in response to recent observed performance. The requested target stays within the range; its lower endpoint does not guarantee minimum rendered FPS.
_Avoid_: guaranteed minimum FPS, frame-rate floor

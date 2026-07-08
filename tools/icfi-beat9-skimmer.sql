SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- ICFI beat 9: skimmer character (terrain-agnostic, fast) + Pip-in-the-wind moment

UPDATE Beats
SET Text = REPLACE(REPLACE(Text,
    'the morning air cold enough to want the layer she''d forgotten to grab off the rack.

The first drone wreckage',
    'the morning air cold enough to want the layer she''d forgotten to grab off the rack.

The 88 ran flat and empty at this hour, and for one open stretch she let the throttle go. The repulsor field carried a fixed geometry two feet above the surface and did not negotiate with terrain. Asphalt, road shoulder, soft gravel, drainage break at the Dixon perimeter — same hum, same height, no adjustment. At full throttle it moved like something very fast that had also decided. She pulled back before the drone wreckage.

The first drone wreckage'),
    'his ears stayed forward and his tail was going against her forearm.

Forty miles in.',
    'his ears stayed forward and his tail was going against her forearm.

She brought the skimmer back up to speed. He immediately shoved his head out the left side into the slipstream, ears flat, mouth wide, tongue lolling in the full loose commitment of a dog who had assessed the situation and found it acceptable.

She let him.

Forty miles in.'),
    UpdatedAt = GETUTCDATE()
FROM Beats
WHERE Id = '019F3EB2-6438-7003-8243-927D5D7D6D8A';

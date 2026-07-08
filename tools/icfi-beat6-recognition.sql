SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Beat 6: insert sensor-scan + Wes recognizes the parts BEFORE the winch man section
-- Anchor: "Wes saw it from the Crown about two minutes out. He knew right away."
-- Insert: what exactly he knew, and why

UPDATE Beats
SET Text = REPLACE(Text,
    N'Wes saw it from the Crown about two minutes out. He knew right away.

He leaned forward',
    N'Wes saw it from the Crown about two minutes out. He knew right away.

The sensor array moved first.

He''d watched it angle down toward Pip and toward the corn and toward the road all morning, the two forward apertures tracking whatever was below the hull. Now it swung toward the flatbed and locked. The whole array rotated, precise, and held.

The hull changed under him. A vibration he''d felt once before: in the clearing at dawn, when the scythe-arm kept returning to the void in the dead machine''s chassis. The same frequency. The same short interval of it.

He looked at the flatbed''s load. Machine-salvage brackets in the sun. Stripped housings. Copper conduit. And there, half-covered under a section of housing panel: a color he recognized. Not from thirteen years of maintenance records. From before that. From being three years old in the north field and looking up at something that didn''t have a name yet but was the biggest and most permanent thing in his world and always would be.

The dead machine''s hull plating. That exact weathered brass-to-bone shade.

He understood what was on the flatbed. And he understood, in the half-second before anything else happened, that the machine had understood it too.

He leaned forward'),
    UpdatedAt = GETUTCDATE()
FROM Beats
WHERE Id = '019F3EB2-6438-7820-8915-07D8E0AF4FAA';

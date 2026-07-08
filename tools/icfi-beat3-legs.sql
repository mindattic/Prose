SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Beat 3: add leg anatomy description as Wes watches machine approach
-- Insert after "The brass hull was going gold in the morning light."

UPDATE Beats
SET Text = REPLACE(Text,
    N'The brass hull was going gold in the morning light. He watched the Crown approach.',
    N'The brass hull was going gold in the morning light. He watched the legs first: two long lateral ones, thin for the forty tons they carried, the knee joint at mid-height bending the wrong way -- inverted, heron-geometry, the lower section swinging forward on each stride while the upper section held. The rear support leg shorter, balancing. All three folding and unfolding across the county road with the patience of something that had been doing this for years. The body rode above them, a rounded mass, the forward sensor array at the face catching the low sun. From up here he could see the chassis ports where the manipulators retracted: closed now, all of them, the hull smooth.

He watched the Crown approach.'),
    UpdatedAt = GETUTCDATE()
FROM Beats
WHERE Id = '019F3EB2-6438-7474-924E-1D9D1925E8AB';

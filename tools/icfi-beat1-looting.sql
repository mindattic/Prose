SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Beat 1: insert (1) looting marks, (2) Schism-twist, (3) maggots — three new paragraphs
-- after "he hadn't known to look." and before "The living machine was beside it."

UPDATE Beats
SET Text = REPLACE(Text,
    N'He hadn''t come to the clearing in a week, and he hadn''t known to look.

The living machine was beside it.',
    N'He hadn''t come to the clearing in a week, and he hadn''t known to look.

He moved closer and stopped. The hull had tool marks on it. Straight cuts at the panel seams, the kind a circular saw left when it was working fast and not caring about the surface. A mounting bracket pulled away from the chassis the wrong way: pried, not unbolted. And in the lower section of the hull, a rectangular void. An empty recess, twenty inches by fourteen, where a module had been seated in its bracket and was now gone. The edges of the void were clean. Removed, not torn.

He looked at the primary strut at the forward joint. The strut was wrong. Not broken. Not bent from the collapse. Twisted: the metal going in two directions at once, the geometry of it impossible if you knew what a strut was supposed to look like, which he did. He''d read maintenance manuals since he was eight. He knew what mechanical failure looked like. He knew what stress fractures looked like and what overload looked like and what impact looked like. This wasn''t any of those. This was the strut doing something metal didn''t do on its own.

He was looking at the strut when he saw the movement at the hull seam.

Thin. A slow line of them, moving through the gap where the forward panel had warped away from the chassis in the same wrongward direction as the strut. Small enough that in the pre-dawn he almost missed them. He stood very still.

He''d grown up watching this machine work the north field. He''d read every manual his father kept in the barn. He thought he knew what a Behemoth was. What it contained.

He did not know what to do with what he was looking at.

He stood there for a long moment. Then he looked at the living machine, still nudging the dark hull. The mourning wail that hadn''t stopped.

If something was inside the dead machine that could be eaten. Then something inside had been alive.

He didn''t finish the thought. He put it away in the same place he put other things that didn''t have words yet.

Someone had been in the field. Someone had taken something out of it.

The living machine was beside it.'),
    UpdatedAt = GETUTCDATE()
FROM Beats
WHERE Id = '019F3EB2-6438-7381-901D-FB023A9011FD';

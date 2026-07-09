SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

UPDATE Beats
SET Text = N'Kito Bramley''s grandmother opened the door before Yemina had finished knocking, a care record already set on the kitchen table within reach, waiting for a person whose job it was to look at it. The burnout flag was six weeks old, in its own field, phrased the same way it was phrased in every record: *elevated neuretics activity, developmental range, monitor quarterly.*',
    UpdatedAt = GETUTCDATE()
WHERE Id = '019EC176-C425-7323-B5C5-D94CC6B570B5';

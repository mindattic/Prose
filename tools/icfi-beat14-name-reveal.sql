SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

UPDATE Beats
SET Text = REPLACE(Text,
    N'Wes watched the horizon for a while after.

"How far to your farm?" CJ said.',
    N'Wes watched the horizon for a while after.

"What do you call it?" CJ said.

He didn''t look at her. "Lord Long Legs."

She looked at him.

"I was five," he said.

"How far to your farm?" CJ said.'),
    UpdatedAt = GETUTCDATE()
WHERE Id = '019F3F12-AF1F-7D6C-A4ED-5D67331CB356';

SELECT @@ROWCOUNT AS RowsFixed;

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Fix: replace physical "comms key / One button" with neuretics sub-vocal gesture
UPDATE Beats
SET Text = REPLACE(Text,
    N'CJ pressed the comms key.
One button. The channel went to static.',
    N'CJ closed the channel.
One sub-vocal gesture. The thread went flat.'),
    UpdatedAt = GETUTCDATE()
WHERE Id = '019F3F12-AF1F-7D6C-A4ED-5D67331CB356';

SELECT @@ROWCOUNT AS RowsFixed;

SET QUOTED_IDENTIFIER ON;
UPDATE Beats
SET Text = REPLACE(
    Text,
    'She had used *Tweeze* because she pulled things out clean. Whatever she took from you, you didn''t know it was gone until it was already somewhere else.',
    'She had used *Sift* because she worked through everything you had and left the rest in order. Whatever she took, you didn''t know it was gone until you went looking and found the pile short.'
)
WHERE Id = '019EE6F2-C80F-7B02-B097-F3F2D74D50E1';
SELECT @@ROWCOUNT AS RowsUpdated;

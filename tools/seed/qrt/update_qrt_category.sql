SET QUOTED_IDENTIFIER ON;
GO
UPDATE Settings SET Json = '{"PriceUsd":0.99,"CategoryPaths":[["Literature & Fiction","Horror","Short Stories"]],"KdpSelect":true,"Drm":false,"AiTextOption":"Entire work, with extensive editing","AiTextTool":"Claude","AiImagesOption":"One or a few AI-generated images, with minimal or no editing","AiImagesTool":"ChatGPT","AiTranslationsOption":"None"}',
UpdatedAt = SYSUTCDATETIME()
WHERE [Key] = 'kdp.newbook.QRT';
GO

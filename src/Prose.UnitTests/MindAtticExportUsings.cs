// ProseInline, the inline-markup parser shared by the exporters and the editor, now lives in the
// shared MindAttic.Export library (migrated 2026-10-08, WO:01a11e53-ec02-766e-a2bc-b66a5158ad04).
// This alias keeps every existing ProseInline reference compiling against the one implementation.
global using ProseInline = MindAttic.Export.Text.ProseInline;

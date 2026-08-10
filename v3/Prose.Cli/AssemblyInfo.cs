// Test-only assembly access. Lets the unit-test project reach internal members
// (query-building helpers, etc.) without inflating the CLI's public API surface.
// Keep the friend list minimal — only test assemblies. Mirrors Prose.Core/AssemblyInfo.cs.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Prose.UnitTests")]

// Test-only assembly access. Lets the unit-test project reach internal members
// (parser helpers, internal records, etc.) without inflating the public API
// surface. Keep the friend list minimal — only test assemblies.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("StreetSamurai.UnitTests")]

namespace StreetSamurai.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// Gear cluster — Weapon / Equipment / Cyberware / Apparel / Ammunition /
// Pharmaceutical / Genemod / Material / Transportation / ConsumerGood.
//
// Every type has a similar spine: identity / classification scalars + lists of
// strings that resolve to references where canon exists. KnownUsers always
// resolves to Character entities; BaseTechnologies always resolves to Technology;
// CompatibleWeapons always resolves to Weapon; AmmunitionType always resolves to
// Ammunition. The alias is preserved so display works even when canon is
// missing.
// ─────────────────────────────────────────────────────────────────────────────

public class Weapon
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public string Legality    { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Description { get; set; } = "";
    public string Specifications { get; set; } = "";
    public string TacticalUse { get; set; } = "";
    public string CulturalContext { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<WeaponAlias>          Aliases          { get; set; } = new List<WeaponAlias>();
    public ICollection<WeaponBaseTechnology> BaseTechnologies { get; set; } = new List<WeaponBaseTechnology>();
    public ICollection<WeaponKnownUser>      KnownUsers       { get; set; } = new List<WeaponKnownUser>();
    public ICollection<WeaponAmmunitionType> AmmunitionTypes  { get; set; } = new List<WeaponAmmunitionType>();
    public ICollection<WeaponStoryHook>      StoryHooks       { get; set; } = new List<WeaponStoryHook>();
}
public class WeaponAlias { public long Id { get; set; } public Guid WeaponId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Weapon? Weapon { get; set; } }
public class WeaponBaseTechnology { public long Id { get; set; } public Guid WeaponId { get; set; } public int Position { get; set; } public Guid? TechnologyId { get; set; } public string Alias { get; set; } = ""; public Weapon? Weapon { get; set; } public Entity? Technology { get; set; } }
public class WeaponKnownUser { public long Id { get; set; } public Guid WeaponId { get; set; } public int Position { get; set; } public Guid? CharacterId { get; set; } public string Alias { get; set; } = ""; public Weapon? Weapon { get; set; } public Entity? Character { get; set; } }
public class WeaponAmmunitionType { public long Id { get; set; } public Guid WeaponId { get; set; } public int Position { get; set; } public Guid? AmmunitionId { get; set; } public string Alias { get; set; } = ""; public Weapon? Weapon { get; set; } public Entity? Ammunition { get; set; } }
public class WeaponStoryHook { public long Id { get; set; } public Guid WeaponId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Weapon? Weapon { get; set; } }

public class Equipment
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public string Legality    { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string BrandName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Description { get; set; } = "";
    public string TacticalUse { get; set; } = "";
    public string CulturalContext { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<EquipmentAlias>          Aliases          { get; set; } = new List<EquipmentAlias>();
    public ICollection<EquipmentBaseTechnology> BaseTechnologies { get; set; } = new List<EquipmentBaseTechnology>();
    public ICollection<EquipmentKnownUser>      KnownUsers       { get; set; } = new List<EquipmentKnownUser>();
    public ICollection<EquipmentSpecification>  Specifications   { get; set; } = new List<EquipmentSpecification>();
    public ICollection<EquipmentStoryHook>      StoryHooks       { get; set; } = new List<EquipmentStoryHook>();
}
public class EquipmentAlias { public long Id { get; set; } public Guid EquipmentId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Equipment? Equipment { get; set; } }
public class EquipmentBaseTechnology { public long Id { get; set; } public Guid EquipmentId { get; set; } public int Position { get; set; } public Guid? TechnologyId { get; set; } public string Alias { get; set; } = ""; public Equipment? Equipment { get; set; } public Entity? Technology { get; set; } }
public class EquipmentKnownUser { public long Id { get; set; } public Guid EquipmentId { get; set; } public int Position { get; set; } public Guid? CharacterId { get; set; } public string Alias { get; set; } = ""; public Equipment? Equipment { get; set; } public Entity? Character { get; set; } }
public class EquipmentSpecification { public long Id { get; set; } public Guid EquipmentId { get; set; } public string KeyName { get; set; } = ""; public string Value { get; set; } = ""; public Equipment? Equipment { get; set; } }
public class EquipmentStoryHook { public long Id { get; set; } public Guid EquipmentId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Equipment? Equipment { get; set; } }

public class Cyberware
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public string BodyLocation { get; set; } = "";
    public string Legality    { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string BrandName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Description { get; set; } = "";
    public string InstallationRequirements { get; set; } = "";
    public string RejectionRisk { get; set; } = "";
    public string Maintenance { get; set; } = "";
    public string Specifications { get; set; } = "";
    public string CulturalContext { get; set; } = "";
    public string StreetPrice { get; set; } = "";
    public string LicensedPrice { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<CyberwareItemAlias>     Aliases     { get; set; } = new List<CyberwareItemAlias>();
    public ICollection<CyberwareItemSideEffect> SideEffects { get; set; } = new List<CyberwareItemSideEffect>();
    public ICollection<CyberwareItemKnownUser> KnownUsers  { get; set; } = new List<CyberwareItemKnownUser>();
    public ICollection<CyberwareItemStoryHook> StoryHooks  { get; set; } = new List<CyberwareItemStoryHook>();
}
public class CyberwareItemAlias { public long Id { get; set; } public Guid CyberwareId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Cyberware? Cyberware { get; set; } }
public class CyberwareItemSideEffect { public long Id { get; set; } public Guid CyberwareId { get; set; } public int Position { get; set; } public string Effect { get; set; } = ""; public Cyberware? Cyberware { get; set; } }
public class CyberwareItemKnownUser { public long Id { get; set; } public Guid CyberwareId { get; set; } public int Position { get; set; } public Guid? CharacterId { get; set; } public string Alias { get; set; } = ""; public Cyberware? Cyberware { get; set; } public Entity? Character { get; set; } }
public class CyberwareItemStoryHook { public long Id { get; set; } public Guid CyberwareId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Cyberware? Cyberware { get; set; } }

public class Apparel
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Description { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<ApparelAlias>      Aliases    { get; set; } = new List<ApparelAlias>();
    public ICollection<ApparelStoryHook>  StoryHooks { get; set; } = new List<ApparelStoryHook>();
}
public class ApparelAlias { public long Id { get; set; } public Guid ApparelId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Apparel? Apparel { get; set; } }
public class ApparelStoryHook { public long Id { get; set; } public Guid ApparelId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Apparel? Apparel { get; set; } }

public class Ammunition
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Caliber     { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public string Legality    { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Description { get; set; } = "";
    public string Specifications { get; set; } = "";
    public string CulturalContext { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<AmmunitionAlias>             Aliases           { get; set; } = new List<AmmunitionAlias>();
    public ICollection<AmmunitionCompatibleWeapon>  CompatibleWeapons { get; set; } = new List<AmmunitionCompatibleWeapon>();
    public ICollection<AmmunitionVariant>           Variants          { get; set; } = new List<AmmunitionVariant>();
    public ICollection<AmmunitionStoryHook>         StoryHooks        { get; set; } = new List<AmmunitionStoryHook>();
}
public class AmmunitionAlias { public long Id { get; set; } public Guid AmmunitionId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Ammunition? Ammunition { get; set; } }
public class AmmunitionCompatibleWeapon { public long Id { get; set; } public Guid AmmunitionId { get; set; } public int Position { get; set; } public Guid? WeaponId { get; set; } public string Alias { get; set; } = ""; public Ammunition? Ammunition { get; set; } public Entity? Weapon { get; set; } }
public class AmmunitionVariant { public long Id { get; set; } public Guid AmmunitionId { get; set; } public int Position { get; set; } public string VariantName { get; set; } = ""; public Ammunition? Ammunition { get; set; } }
public class AmmunitionStoryHook { public long Id { get; set; } public Guid AmmunitionId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Ammunition? Ammunition { get; set; } }

public class Pharmaceutical
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Subcategory { get; set; } = "";
    public string Legality    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Description { get; set; } = "";
    public string MethodOfUse { get; set; } = "";
    public string Duration { get; set; } = "";
    public string AddictionRisk { get; set; } = "";
    public string StreetPrice { get; set; } = "";
    public string CulturalContext { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<PharmAlias>      Aliases     { get; set; } = new List<PharmAlias>();
    public ICollection<PharmEffect>     Effects     { get; set; } = new List<PharmEffect>();
    public ICollection<PharmSideEffect> SideEffects { get; set; } = new List<PharmSideEffect>();
    public ICollection<PharmStoryHook>  StoryHooks  { get; set; } = new List<PharmStoryHook>();
}
public class PharmAlias { public long Id { get; set; } public Guid PharmaceuticalId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Pharmaceutical? Pharmaceutical { get; set; } }
public class PharmEffect { public long Id { get; set; } public Guid PharmaceuticalId { get; set; } public int Position { get; set; } public string Effect { get; set; } = ""; public Pharmaceutical? Pharmaceutical { get; set; } }
public class PharmSideEffect { public long Id { get; set; } public Guid PharmaceuticalId { get; set; } public int Position { get; set; } public string Effect { get; set; } = ""; public Pharmaceutical? Pharmaceutical { get; set; } }
public class PharmStoryHook { public long Id { get; set; } public Guid PharmaceuticalId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Pharmaceutical? Pharmaceutical { get; set; } }

public class Genemod
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Description { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<GenemodAlias>     Aliases    { get; set; } = new List<GenemodAlias>();
    public ICollection<GenemodStoryHook> StoryHooks { get; set; } = new List<GenemodStoryHook>();
}
public class GenemodAlias { public long Id { get; set; } public Guid GenemodId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Genemod? Genemod { get; set; } }
public class GenemodStoryHook { public long Id { get; set; } public Guid GenemodId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Genemod? Genemod { get; set; } }

public class Material
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Description { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<MaterialAlias>     Aliases    { get; set; } = new List<MaterialAlias>();
    public ICollection<MaterialStoryHook> StoryHooks { get; set; } = new List<MaterialStoryHook>();
}
public class MaterialAlias { public long Id { get; set; } public Guid MaterialId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Material? Material { get; set; } }
public class MaterialStoryHook { public long Id { get; set; } public Guid MaterialId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Material? Material { get; set; } }

public class Transportation
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Description { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<TransportationAlias>     Aliases    { get; set; } = new List<TransportationAlias>();
    public ICollection<TransportationStoryHook> StoryHooks { get; set; } = new List<TransportationStoryHook>();
}
public class TransportationAlias { public long Id { get; set; } public Guid TransportationId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public Transportation? Transportation { get; set; } }
public class TransportationStoryHook { public long Id { get; set; } public Guid TransportationId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public Transportation? Transportation { get; set; } }

public class ConsumerGood
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name.</summary>
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Tier        { get; set; } = "";
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Description { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";
    public Entity? Entity { get; set; }
    public ICollection<ConsumerGoodAlias>     Aliases    { get; set; } = new List<ConsumerGoodAlias>();
    public ICollection<ConsumerGoodStoryHook> StoryHooks { get; set; } = new List<ConsumerGoodStoryHook>();
}
public class ConsumerGoodAlias { public long Id { get; set; } public Guid ConsumerGoodId { get; set; } public int Position { get; set; } public string Value { get; set; } = ""; public ConsumerGood? ConsumerGood { get; set; } }
public class ConsumerGoodStoryHook { public long Id { get; set; } public Guid ConsumerGoodId { get; set; } public int Position { get; set; } public string Hook { get; set; } = ""; public ConsumerGood? ConsumerGood { get; set; } }

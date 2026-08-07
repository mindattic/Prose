using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Assesses crew capability against contract requirements.
/// Grades whether an assembled team has the right makeup for a job.
/// Identifies capability gaps that predict failure points.
/// Suggests pharmaceutical/equipment solutions for gaps (e.g., Lingo-Patch for linguistics).
/// </summary>
public class CrewAssessmentService
{
    private readonly DatabaseService db;
    private readonly PharmaceuticalRepository pharmaRepo;
    private readonly EquipmentRepository equipRepo;

    public CrewAssessmentService(DatabaseService db, PharmaceuticalRepository pharmaRepo, EquipmentRepository equipRepo)
    {
        this.db = db;
        this.pharmaRepo = pharmaRepo;
        this.equipRepo = equipRepo;
    }

    /// <summary>
    /// Assess a crew against a contract's requirements.
    /// Returns a grade (A-F) and detailed capability analysis.
    /// </summary>
    public CrewAssessment Assess(ContractData contract, List<string> crewNames)
    {
        var assessment = new CrewAssessment
        {
            ContractCodename = contract.Codename,
            CrewMembers = crewNames,
        };

        var crewCapabilities = new CrewCapabilities();
        var memberProfiles = new List<CrewMemberProfile>();

        foreach (var name in crewNames)
        {
            var character = db.FindCharacter(name);
            if (character == null) continue;

            var profile = new CrewMemberProfile { Name = name, Role = character.Role };

            // Estimate capabilities from character data
            var roleLower = (character.Role ?? "").ToLowerInvariant();
            var descLower = (character.Description ?? "").ToLowerInvariant();
            var augDesc = (character.Augmentations ?? "").ToLowerInvariant();
            var combined = $"{roleLower} {descLower} {augDesc}";

            // Score each capability based on role/description keywords
            profile.Combat = ScoreCapability(combined, ["combat", "fighter", "samurai", "sniper", "bodyguard", "bounty", "muscle", "breacher", "demolition", "weapon"]);
            profile.Stealth = ScoreCapability(combined, ["infiltrat", "stealth", "ghost", "shadow", "silent", "sneak", "covert"]);
            profile.Hacking = ScoreCapability(combined, ["hack", "netrunner", "intrusion", "decrypt", "data thief", "cyber", "code"]);
            profile.Social = ScoreCapability(combined, ["face", "social", "diplomat", "broker", "fixer", "charm", "negotiate", "con"]);
            profile.Medical = ScoreCapability(combined, ["medic", "doctor", "surgeon", "medical", "trauma", "heal", "pharma"]);
            profile.Tech = ScoreCapability(combined, ["tech", "rigger", "drone", "engineer", "mechanic", "repair", "fabricat"]);
            profile.Transport = ScoreCapability(combined, ["wheelman", "driver", "pilot", "getaway", "transport", "vehicle"]);
            profile.Demolitions = ScoreCapability(combined, ["demolit", "explosive", "bomb", "blast", "breach"]);
            profile.Surveillance = ScoreCapability(combined, ["surveillance", "recon", "scout", "sensor", "monitor", "counter-surveil"]);
            profile.Linguistics = ScoreCapability(combined, ["linguist", "translator", "language", "interpret", "polyglot"]);

            memberProfiles.Add(profile);

            // Aggregate crew capabilities (take best in each category)
            crewCapabilities.Combat = Math.Max(crewCapabilities.Combat, profile.Combat);
            crewCapabilities.Stealth = Math.Max(crewCapabilities.Stealth, profile.Stealth);
            crewCapabilities.Hacking = Math.Max(crewCapabilities.Hacking, profile.Hacking);
            crewCapabilities.Social = Math.Max(crewCapabilities.Social, profile.Social);
            crewCapabilities.Medical = Math.Max(crewCapabilities.Medical, profile.Medical);
            crewCapabilities.Tech = Math.Max(crewCapabilities.Tech, profile.Tech);
            crewCapabilities.Transport = Math.Max(crewCapabilities.Transport, profile.Transport);
            crewCapabilities.Demolitions = Math.Max(crewCapabilities.Demolitions, profile.Demolitions);
            crewCapabilities.Surveillance = Math.Max(crewCapabilities.Surveillance, profile.Surveillance);
            crewCapabilities.Linguistics = Math.Max(crewCapabilities.Linguistics, profile.Linguistics);
        }

        assessment.CrewCapabilities = crewCapabilities;
        assessment.MemberProfiles = memberProfiles;

        // Compare crew to requirements
        var req = contract.RequiredCapabilities;
        var gaps = new List<CapabilityGap>();

        CompareCapability(gaps, "Combat", req.Combat, crewCapabilities.Combat);
        CompareCapability(gaps, "Stealth", req.Stealth, crewCapabilities.Stealth);
        CompareCapability(gaps, "Hacking", req.Hacking, crewCapabilities.Hacking);
        CompareCapability(gaps, "Social", req.Social, crewCapabilities.Social);
        CompareCapability(gaps, "Medical", req.Medical, crewCapabilities.Medical);
        CompareCapability(gaps, "Tech", req.Tech, crewCapabilities.Tech);
        CompareCapability(gaps, "Transport", req.Transport, crewCapabilities.Transport);
        CompareCapability(gaps, "Demolitions", req.Demolitions, crewCapabilities.Demolitions);
        CompareCapability(gaps, "Surveillance", req.Surveillance, crewCapabilities.Surveillance);
        CompareCapability(gaps, "Linguistics", req.Linguistics, crewCapabilities.Linguistics);

        assessment.Gaps = gaps;

        // Suggest solutions for gaps
        foreach (var gap in gaps)
            gap.Suggestions = SuggestSolutions(gap.Capability, gap.Deficit);

        // Calculate overall grade
        var totalRequired = req.Combat + req.Stealth + req.Hacking + req.Social + req.Medical + req.Tech + req.Transport + req.Demolitions + req.Surveillance + req.Linguistics;
        var totalDeficit = gaps.Sum(g => g.Deficit);

        if (totalRequired == 0)
            assessment.Grade = "A";
        else
        {
            var score = 1.0 - ((double)totalDeficit / totalRequired);
            assessment.Grade = score switch
            {
                >= 0.9 => "A",
                >= 0.75 => "B",
                >= 0.6 => "C",
                >= 0.4 => "D",
                _ => "F",
            };
        }

        assessment.Summary = assessment.Grade switch
        {
            "A" => "Crew exceeds requirements. High confidence in mission success.",
            "B" => "Crew meets most requirements. Minor gaps addressable with equipment or pharmaceuticals.",
            "C" => "Crew has notable capability gaps. Success depends on planning and improvisation.",
            "D" => "Crew is underqualified. Significant gaps will likely cause mission complications.",
            "F" => "Crew lacks critical capabilities. Mission failure is probable without major adjustments.",
            _ => ""
        };

        return assessment;
    }

    private static int ScoreCapability(string text, string[] keywords)
    {
        var hits = keywords.Count(k => text.Contains(k));
        return Math.Min(hits * 3, 10);
    }

    private static void CompareCapability(List<CapabilityGap> gaps, string name, int required, int actual)
    {
        if (required <= 0) return;
        if (actual >= required) return;
        gaps.Add(new CapabilityGap
        {
            Capability = name,
            Required = required,
            Actual = actual,
            Deficit = required - actual,
        });
    }

    private List<string> SuggestSolutions(string capability, int deficit)
    {
        var suggestions = new List<string>();

        // Pharmaceutical solutions
        var pharmas = pharmaRepo.GetAll();
        var equipments = equipRepo.GetAll();

        switch (capability.ToLowerInvariant())
        {
            case "linguistics":
                suggestions.Add("Lingo-Patch — dermal pharmaceutical that provides temporary language comprehension (4-8 hours)");
                suggestions.Add("Neural translation overlay via BCI — real-time but detectable");
                break;
            case "combat":
                suggestions.Add("Combat stimulants — adrenal boosters, reflex accelerators (temporary, side effects)");
                suggestions.Add("Hire additional combat specialist");
                break;
            case "stealth":
                suggestions.Add("Chameleon geneware or optical camouflage equipment");
                suggestions.Add("Blindspot E.L.F. if available in the target zone");
                break;
            case "hacking":
                suggestions.Add("Pre-built intrusion deck with automated exploit libraries");
                suggestions.Add("Hire a netrunner — this gap cannot be pharmacologically bridged");
                break;
            case "social":
                suggestions.Add("Confidence-building pharmaceuticals — social anxiety suppressors");
                suggestions.Add("BCI-assisted deception coaching (real-time lie optimization)");
                break;
            case "medical":
                suggestions.Add("Automated trauma kit with AI diagnostic assist");
                suggestions.Add("Emergency medical nanite injectors (expensive, single-use)");
                break;
            case "tech":
                suggestions.Add("Portable diagnostic suite with guided repair overlays");
                break;
            case "transport":
                suggestions.Add("Autonomous vehicle rental — self-driving escape vehicle on standby");
                break;
            case "demolitions":
                suggestions.Add("Pre-fabricated shaped charges with timer programming — reduces skill requirement");
                break;
            case "surveillance":
                suggestions.Add("Micro-drone swarm — autonomous surveillance with minimal operator skill");
                break;
        }

        if (deficit >= 5)
            suggestions.Add($"CRITICAL: {capability} gap of {deficit} cannot be fully addressed with supplements. Recruit a specialist or abort.");

        return suggestions;
    }
}

public class CrewAssessment
{
    public string ContractCodename { get; set; } = "";
    public List<string> CrewMembers { get; set; } = [];
    public CrewCapabilities CrewCapabilities { get; set; } = new();
    public List<CrewMemberProfile> MemberProfiles { get; set; } = [];
    public List<CapabilityGap> Gaps { get; set; } = [];
    public string Grade { get; set; } = "";
    public string Summary { get; set; } = "";
}

public class CrewMemberProfile
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public int Combat { get; set; }
    public int Stealth { get; set; }
    public int Hacking { get; set; }
    public int Social { get; set; }
    public int Medical { get; set; }
    public int Tech { get; set; }
    public int Transport { get; set; }
    public int Demolitions { get; set; }
    public int Surveillance { get; set; }
    public int Linguistics { get; set; }
}

public class CapabilityGap
{
    public string Capability { get; set; } = "";
    public int Required { get; set; }
    public int Actual { get; set; }
    public int Deficit { get; set; }
    public List<string> Suggestions { get; set; } = [];
}

using RimWorld;
using Verse;

[DefOf]
public static class LiAIChatJobDefOf
{
    public static JobDef LiAIChat_StudyAncientArchive;

    public static JobDef LiAIChat_IdentifyAncientArchive;

    public static JobDef LiAIChat_InvestigateArchiveTerminal;

    static LiAIChatJobDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(
            typeof(LiAIChatJobDefOf));
    }
}
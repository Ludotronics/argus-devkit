using UnrealBuildTool;

public class ArgusEditor : ModuleRules
{
    public ArgusEditor(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
        PublicDependencyModuleNames.AddRange(new string[] { "Core", "ArgusRuntime" });
        PrivateDependencyModuleNames.AddRange(new string[] { "UnrealEd" });
    }
}

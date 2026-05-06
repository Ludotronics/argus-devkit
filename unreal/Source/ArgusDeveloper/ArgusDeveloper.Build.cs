using UnrealBuildTool;

public class ArgusDeveloper : ModuleRules
{
    public ArgusDeveloper(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
        PublicDependencyModuleNames.AddRange(new string[] { "Core", "CoreUObject", "Engine", "Json", "ArgusRuntime" });
    }
}

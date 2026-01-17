namespace Models.Enums;

public enum DarwingOperation
{
    Idle,
    LoadSolution,
    BuildSolution,
    TestUnmutatedSolution,
    DiscoveringMutants,
    BuildingMutatedSolution,
    TestMutants
}

using Models.Enums;

namespace ModelsTests;

public class MutationDescriptionTests
{
    [Test]
    public void EnsureAllCategoriesHaveADescription()
    {
        foreach (MutationCategory category in Enum.GetValues<MutationCategory>())
        {
            string desc = category.GetDescription();
            if (string.IsNullOrWhiteSpace(desc) || desc == category.ToString())
            {
                Assert.Fail($"Mutation category {category} must have a description.");
            }
        }
    }

    [Test]
    public void EnsureAllImplementationsHaveADescription()
    {
        foreach (SpecificMutation mutation in Enum.GetValues<SpecificMutation>())
        {
            string desc = mutation.GetDescription();
            if (string.IsNullOrWhiteSpace(desc) || desc == mutation.ToString())
            {
                Assert.Fail($"Mutation category {mutation} must have a description.");
            }
        }
    }
}

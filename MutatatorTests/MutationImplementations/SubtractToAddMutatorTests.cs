using Mutator.MutationImplementations;

namespace MutatorTests.MutationImplementations;

public class SubtractToAddMutatorTests
{
    private SubtractToAddMutator _mutator;

    [SetUp]
    public void SetUp()
    {
        _mutator = new SubtractToAddMutator();
    }

    [Test]
    public void GivenAddNode_WhenMutate_ThenGivesSubtractNodeWithSameLeftAndRight()
    {
        //TODO: Not going to bother testing this until after ive introduced the mutation switching syntax
    }
}

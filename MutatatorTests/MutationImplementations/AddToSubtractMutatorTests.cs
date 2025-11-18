using Mutator.MutationImplementations;

namespace MutatorTests.MutationImplementations;

public class AddToSubtractMutatorTests
{
    private AddToSubtractMutator _mutator;

    [SetUp]
    public void SetUp()
    {
        _mutator = new AddToSubtractMutator();
    }

    [Test]
    public void GivenAddNode_WhenMutate_ThenGivesSubtractNodeWithSameLeftAndRight()
    {
        //TODO: Not going to bother testing this until after ive introduced the mutation switching syntax
    }
}

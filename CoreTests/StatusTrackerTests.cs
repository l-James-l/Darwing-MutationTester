using Core;
using Models.Enums;
using Models.Events;
using NSubstitute;

namespace CoreTests;

public class StatusTrackerTests
{
    private StatusTracker _statusTracker;
    private IEventAggregator _eventAggregator;

    private DarwingOperationStatesChangedEvent _statusUpdateEvent;

    [SetUp]
    public void Setup()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();

        _statusUpdateEvent = Substitute.For<DarwingOperationStatesChangedEvent>();
        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Returns(_statusUpdateEvent);

        _statusTracker = new StatusTracker(_eventAggregator);
    }

    [Test]
    public void InitialStatus_IsIdle()
    {
        var status = _statusTracker.CheckStatus(DarwingOperation.Idle);
        Assert.That(status, Is.EqualTo(OperationStates.Ongoing));
    }

    [Test]
    public void StartOperation_ValidTransition_UpdatesStatus()
    {
        bool result = _statusTracker.TryStartOperation(DarwingOperation.LoadSolution);
        Assert.That(result, Is.True);
        OperationStates status = _statusTracker.CheckStatus(DarwingOperation.LoadSolution);
        Assert.That(status, Is.EqualTo(OperationStates.Ongoing));
        status = _statusTracker.CheckStatus(DarwingOperation.Idle);
        Assert.That(status, Is.EqualTo(OperationStates.NotStarted));
        _statusUpdateEvent.Received(1).Publish(DarwingOperation.LoadSolution);
    }

    [Test]
    public void StartOperation_InvalidTransition_DoesNotUpdateStatus()
    {
        // Attempt to start BuildingMutatedSolution before previous steps
        bool result = _statusTracker.TryStartOperation(DarwingOperation.BuildingMutatedSolution);
        Assert.That(result, Is.False);
        OperationStates status = _statusTracker.CheckStatus(DarwingOperation.BuildingMutatedSolution);
        Assert.That(status, Is.EqualTo(OperationStates.NotStarted));
        _statusUpdateEvent.DidNotReceiveWithAnyArgs().Publish(default);
    }

    [Test]
    public void StartOperation_Regressing_ResetsSubsequentStatuses()
    {
        // Progress to TestUnmutatedSolution
        _statusTracker.TryStartOperation(DarwingOperation.LoadSolution);
        _statusTracker.TryStartOperation(DarwingOperation.BuildSolution);
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution);
        // Now regress back to LoadSolution
        _statusTracker.TryStartOperation(DarwingOperation.LoadSolution);
        // Check that subsequent operations are reset
        Assert.That(_statusTracker.CheckStatus(DarwingOperation.BuildSolution), Is.EqualTo(OperationStates.NotStarted));
        Assert.That(_statusTracker.CheckStatus(DarwingOperation.TestUnmutatedSolution), Is.EqualTo(OperationStates.NotStarted));
    }

    [Test]
    public void CheckStatus_UnknownOperation_ReturnsNotStarted()
    {
        var status = _statusTracker.CheckStatus((DarwingOperation)999);
        Assert.That(status, Is.EqualTo(OperationStates.NotStarted));
    }

    [Test]
    public void StartOperation_ThenFinishOperationWithSuccess_StatesAsExpected()
    {
        bool result = _statusTracker.TryStartOperation(DarwingOperation.LoadSolution);
        Assert.That(result, Is.True);
        Assert.That(_statusTracker.CheckStatus(DarwingOperation.LoadSolution), Is.EqualTo(OperationStates.Ongoing));
        _statusUpdateEvent.Received(1).Publish(DarwingOperation.LoadSolution);
        _statusTracker.FinishOperation(DarwingOperation.LoadSolution, true);
        Assert.That(_statusTracker.CheckStatus(DarwingOperation.LoadSolution), Is.EqualTo(OperationStates.Succeeded));
        _statusUpdateEvent.Received(2).Publish(DarwingOperation.LoadSolution);
    }

    [Test]
    public void StartOperation_ThenFinishOperationWithFailure_StatesAsExpected()
    {
        _statusTracker.TryStartOperation(DarwingOperation.LoadSolution);
        Assert.That(_statusTracker.CheckStatus(DarwingOperation.LoadSolution), Is.EqualTo(OperationStates.Ongoing));
        _statusUpdateEvent.Received(1).Publish(DarwingOperation.LoadSolution);
        _statusTracker.FinishOperation(DarwingOperation.LoadSolution, false);
        Assert.That(_statusTracker.CheckStatus(DarwingOperation.LoadSolution), Is.EqualTo(OperationStates.Failed));
        _statusUpdateEvent.Received(2).Publish(DarwingOperation.LoadSolution);
    }

    [Test]
    public void GivenOperationNotStarted_WhenFinishOperation_ThenThrowException()
    {
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _statusTracker.FinishOperation(DarwingOperation.TestMutants, true);
        });
        Assert.That(ex.Message, Is.EqualTo("Cannot finish operation TestMutants because current operation is Idle"));
    }

    [Test]
    public void FullIntendedOperationOrder_WorksAsExpected()
    {
        var operations = new[]
        {
            DarwingOperation.LoadSolution,
            DarwingOperation.BuildSolution,
            DarwingOperation.TestUnmutatedSolution,
            DarwingOperation.DiscoveringMutants,
            DarwingOperation.BuildingMutatedSolution,
            DarwingOperation.TestMutants
        };
        foreach (var operation in operations)
        {
            bool started = _statusTracker.TryStartOperation(operation);
            Assert.That(started, Is.True);
            Assert.That(_statusTracker.CheckStatus(operation), Is.EqualTo(OperationStates.Ongoing));
            _statusUpdateEvent.Received(1).Publish(operation);
            _statusTracker.FinishOperation(operation, true);
            Assert.That(_statusTracker.CheckStatus(operation), Is.EqualTo(OperationStates.Succeeded));
            _statusUpdateEvent.Received(2).Publish(operation);
        }
    }
}

ARCADE AI SYSTEM - SETUP GUIDE
==============================

This package is a modular foundation for:
- Hierarchical parent/child AI states
- Customer activity planning
- Customer queues
- Generic arcade stations
- Worker station assignment
- Customer review memory
- Arcade star rating
- Arcade capacity
- Daily customer traffic/spawning

Namespace:
Cherry.ArcadeAI

Recommended Folder:
Assets/Resources/Scripts/World And Object Systems/ArcadeAI/
or wherever you keep your gameplay systems.


1) MANAGERS
-----------

Create an empty GameObject called:

Arcade Managers

Add these components:
- ArcadeStationRegistry
- ArcadeCapacityManager
- ArcadeReputationManager
- ArcadeTrafficManager

Optional:
- ArcadeReviewBillboardDebug


2) STATIONS
-----------

For every thing customers can use, create an ArcadeStation.

Examples:
- Checkout counter:
  stationType = Checkout
  allowUseWithoutWorker = false

- Ice cream counter:
  stationType = IceCream
  allowUseWithoutWorker = false

- Pizza counter:
  stationType = Pizza
  allowUseWithoutWorker = false

- Arcade cabinet:
  stationType = ArcadeGame
  allowUseWithoutWorker = true

- Dining table:
  stationType = Dining
  allowUseWithoutWorker = true

Each station should have:
- servicePoint
- workerStandPoint if a worker can work there
- queuePoints list if customers should line up


3) CUSTOMER PREFAB
------------------

On the root customer prefab, add:
- NavMeshAgent
- ArcadeStateMachine
- CustomerBrain
- CustomerReviewMemory

Example hierarchy:

Customer
├── ArcadeStateMachine
├── CustomerBrain
├── CustomerReviewMemory
└── States
    ├── Enter Arcade
    │   └── CustomerEnterArcadeState
    ├── Choose Activity
    │   └── CustomerChooseActivityState
    ├── Use Station Parent
    │   └── ArcadeParentState
    │       ├── Go To Station
    │       │   └── CustomerGoToStationState
    │       ├── Wait In Queue
    │       │   └── CustomerWaitInQueueState
    │       └── Use Station
    │           └── CustomerUseStationState
    └── Leave Arcade Parent
        └── ArcadeParentState
            ├── Walk To Exit
            │   └── CustomerWalkToExitState
            └── Post Review
                └── CustomerPostReviewState

Wire the states like this:

ArcadeStateMachine.initialState = Enter Arcade

Enter Arcade.nextState = Choose Activity

Choose Activity:
- goToStationState = Use Station Parent
- leaveArcadeState = Leave Arcade Parent

Use Station Parent:
- firstChildState = Go To Station
- nextState = Choose Activity

Go To Station.nextState = Wait In Queue
Go To Station.failedState = Choose Activity

Wait In Queue.nextState = Use Station
Wait In Queue.failedState = Choose Activity

Use Station.nextState = null
Use Station.failedState = Choose Activity

Note:
Child states are allowed to jump to top-level states.
The base ArcadeAIState detects whether the target is inside the same parent or outside it.

Leave Arcade Parent:
- firstChildState = Walk To Exit
- nextState = null

Walk To Exit.nextState = Post Review

Post Review:
- destroyCustomerRoot = true


4) WORKER PREFAB
----------------

On the worker root, add:
- NavMeshAgent
- ArcadeStateMachine
- WorkerBrain

Example hierarchy:

Worker
├── ArcadeStateMachine
├── WorkerBrain
└── States
    ├── Go To Assigned Station
    │   └── WorkerGoToAssignedStationState
    └── Wait At Station
        └── WorkerWaitAtStationState

Wire:
ArcadeStateMachine.initialState = Go To Assigned Station
Go To Assigned Station.nextState = Wait At Station

Assign the worker by setting WorkerBrain.assignedStation in the inspector,
or call:

workerBrain.AssignToStation(station);


5) TRAFFIC / DAILY SPAWNING
---------------------------

On ArcadeTrafficManager:
- Add customer prefab(s)
- Add spawn point(s)

Call:
ArcadeTrafficManager.Instance.StartArcadeDay();

when the arcade opens.

Call:
ArcadeTrafficManager.Instance.EndArcadeDay();

when the arcade closes.

The daily target visitor count is based on:
ArcadeCapacityManager.MaxCustomersAllowed
x
ArcadeReputationManager rating multiplier


6) REVIEWS
----------

A customer's final state should be CustomerPostReviewState.

That state:
- generates the review
- posts it to ArcadeReputationManager
- updates customer capacity
- destroys/despawns the customer

Other systems can add complaints like this:

customer.reviewMemory.AddComplaint(
    "The arcade was dirty",
    15,
    ArcadeReviewIssueType.StoreDirty
);

Or positives:

customer.reviewMemory.AddPositive("The pizza was good");


7) NOTES FOR YOUR PROJECT
-------------------------

This is intentionally not locked to your current store/day system.
To integrate it with Cherry.DayAndTime, call:

ArcadeReputationManager.Instance.SetCurrentDay(currentDay);

from your existing day system.

If you already register customers entering/leaving elsewhere,
either remove those calls there or avoid double counting.
ArcadeTrafficManager marks spawned customers as already counted.

This system is meant to be a sturdy skeleton.
You can add more specific states later:
- BrowsePrizeCounterState
- ChooseFoodOrderState
- CarryPizzaToGameState
- EatWhilePlayingState
- WorkerCleanMessState
- WorkerRestockStationState
- WorkerRepairGameState

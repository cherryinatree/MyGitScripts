using UnityEngine;

namespace Cherry.ArcadeAI
{
    public enum CustomerActivityType
    {
        None,
        BuyItem,
        PlayGame,
        GetIceCream,
        GetPizza,
        Eat,
        EatWhilePlaying,
        Checkout,
        Leave
    }

    public enum ArcadeStationType
    {
        General,
        Checkout,
        ArcadeGame,
        IceCream,
        Pizza,
        Dining,
        PrizeCounter,
        Cleaning,
        Restock
    }

    public enum WorkerRole
    {
        General,
        Cashier,
        IceCreamWorker,
        PizzaWorker,
        GameAttendant,
        Cleaner,
        Restocker,
        PrizeCounterWorker
    }

    public enum ArcadeReviewIssueType
    {
        Positive,
        NotEnoughGames,
        StoreDirty,
        WaitedTooLong,
        FoodTookTooLong,
        NoWorkerAvailable,
        GameBroken,
        TooCrowded,
        TooExpensive,
        NoSeatAvailable,
        FoodUnavailable,
        GoodGames,
        GoodFood,
        CleanArcade,
        FastService
    }
}

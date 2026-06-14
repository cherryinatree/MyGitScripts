using System.Collections.Generic;
using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// A lightweight locator for arcade stations.
    /// Stations register themselves on enable.
    /// </summary>
    public class ArcadeStationRegistry : MonoBehaviour
    {
        public static ArcadeStationRegistry Instance { get; private set; }

        [SerializeField] private List<ArcadeStation> stations = new List<ArcadeStation>();

        public IReadOnlyList<ArcadeStation> Stations => stations;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Register(ArcadeStation station)
        {
            if (station == null)
                return;

            if (!stations.Contains(station))
                stations.Add(station);
        }

        public void Unregister(ArcadeStation station)
        {
            if (station == null)
                return;

            stations.Remove(station);
        }

        public int CountStationsOfType(ArcadeStationType stationType)
        {
            int count = 0;

            for (int i = 0; i < stations.Count; i++)
            {
                if (stations[i] != null && stations[i].stationType == stationType && stations[i].gameObject.activeInHierarchy)
                    count++;
            }

            return count;
        }

        public ArcadeStation FindBestStationForActivity(CustomerActivityType activityType)
        {
            ArcadeStationType desiredType = GetStationTypeForActivity(activityType);
            ArcadeStation best = null;
            int bestScore = int.MaxValue;

            for (int i = 0; i < stations.Count; i++)
            {
                ArcadeStation station = stations[i];

                if (station == null || !station.gameObject.activeInHierarchy)
                    continue;

                if (station.stationType != desiredType)
                    continue;

                if (!station.CanAcceptCustomer())
                    continue;

                int score = station.QueueCount;

                if (station.isBroken)
                    score += 100;

                if (station.isDirty)
                    score += 10;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = station;
                }
            }

            return best;
        }

        public ArcadeStation FindFirstStationOfType(ArcadeStationType stationType)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                ArcadeStation station = stations[i];

                if (station != null && station.stationType == stationType && station.gameObject.activeInHierarchy)
                    return station;
            }

            return null;
        }

        public static ArcadeStationType GetStationTypeForActivity(CustomerActivityType activityType)
        {
            switch (activityType)
            {
                case CustomerActivityType.PlayGame:
                case CustomerActivityType.EatWhilePlaying:
                    return ArcadeStationType.ArcadeGame;

                case CustomerActivityType.GetIceCream:
                    return ArcadeStationType.IceCream;

                case CustomerActivityType.GetPizza:
                    return ArcadeStationType.Pizza;

                case CustomerActivityType.Eat:
                    return ArcadeStationType.Dining;

                case CustomerActivityType.BuyItem:
                case CustomerActivityType.Checkout:
                    return ArcadeStationType.Checkout;

                default:
                    return ArcadeStationType.General;
            }
        }
    }
}

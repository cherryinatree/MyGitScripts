using System;
using System.Collections.Generic;
using UnityEngine; // for Random.value (Unity RNG)

namespace Checkout
{
    public enum Outcome
    {
        NextDollar,   // ceil to $1
        NextFive,     // ceil to $5
        NextTen,      // ceil to $10
        NextTwenty,   // ceil to $20
        NextHundred   // ceil to $100
    }

    [Serializable]
    public struct Bills
    {
        public int n100, n20, n10, n5, n1;
        public int TotalBills => n100 + n20 + n10 + n5 + n1;

        public override string ToString()
            => $"$100×{n100}, $20×{n20}, $10×{n10}, $5×{n5}, $1×{n1} (bills: {TotalBills})";
    }

    public static class Cash
    {
        // -------- Core: Change & Breakdown --------

        /// <summary>
        /// Returns the change due when the customer hands whole-dollar cash.
        /// Example: ChangeDue(46.52m, 60) => 13.48
        /// </summary>
        public static float ChangeDue(float total, int tenderedWholeDollars)
        {
            int minWhole = (int)Math.Ceiling(total);
            if (tenderedWholeDollars < minWhole)
                throw new ArgumentException($"Tendered must be >= {minWhole} (whole dollars).");
            return tenderedWholeDollars - total;
        }

        /// <summary>
        /// Fewest-bills breakdown using $100/$20/$10/$5/$1.
        /// </summary>
        public static Bills Breakdown(int wholeDollars)
        {
            if (wholeDollars < 0) throw new ArgumentOutOfRangeException(nameof(wholeDollars));
            Bills b = default;
            int r = wholeDollars;

            b.n100 = r / 100; r -= b.n100 * 100;
            b.n20 = r / 20; r -= b.n20 * 20;
            b.n10 = r / 10; r -= b.n10 * 10;
            b.n5 = r / 5; r -= b.n5 * 5;
            b.n1 = r;

            return b;
        }

        /// <summary>
        /// If you prefer entering bill counts directly, this sums them.
        /// </summary>
        public static int AmountFromBills(int n100, int n20, int n10, int n5, int n1)
            => 100 * n100 + 20 * n20 + 10 * n10 + 5 * n5 + n1;

        // -------- Weighted tender generation (you control likelihoods) --------

        /// <summary>
        /// Generate how much the customer "decides" to hand you based on weighted outcomes.
        /// Outcomes are: NextDollar, NextFive, NextTen, NextTwenty, NextHundred.
        /// You can bias behavior by setting weights (likelihoods). Zero or missing = not chosen.
        /// Returns (amount, bills, outcomeChosen).
        /// </summary>
        /// <param name="total">Cart total (e.g., 46.52m)</param>
        /// <param name="weights">
        /// Map of Outcome -> weight (>= 0). Example:
        /// { NextTwenty: 0.6f, NextTen: 0.2f, NextFive: 0.1f, NextHundred: 0.1f }.
        /// If null or all weights <= 0, sensible defaults will be used.
        /// </param>
        /// <param name="singleDenominationWhenPossible">
        /// If true and the chosen amount is divisible by one denomination, use only that bill
        /// (e.g., $60 -> 3×$20, $200 -> 2×$100). Otherwise uses fewest-bills mix.
        /// </param>
        public static (int amount, Bills breakdown, Outcome outcomeChosen) GenerateTendered(
            float total,
            IDictionary<Outcome, float> weights = null,
            bool singleDenominationWhenPossible = false)
        {
            int baseWhole = (int)Math.Ceiling(total);

            // Use defaults if nothing valid provided.
            var w = EnsureWeights(weights);

            // Pick outcome by weighted random.
            Outcome chosen = WeightedPick(w);

            // Compute the corresponding whole-dollar amount.
            int tendered = chosen switch
            {
                Outcome.NextDollar => RoundUpTo(baseWhole, 1),
                Outcome.NextFive => RoundUpTo(baseWhole, 5),
                Outcome.NextTen => RoundUpTo(baseWhole, 10),
                Outcome.NextTwenty => RoundUpTo(baseWhole, 20),
                Outcome.NextHundred => RoundUpTo(baseWhole, 100),
                _ => baseWhole
            };

            // Build bill breakdown
            Bills b = singleDenominationWhenPossible
                ? SingleDenominationOrFewest(tendered)
                : Breakdown(tendered);

            return (tendered, b, chosen);
        }

        // -------- Internals --------

        private static int RoundUpTo(int value, int multiple)
        {
            int m = value % multiple;
            return m == 0 ? value : value + (multiple - m);
        }

        private static Bills SingleDenominationOrFewest(int amount)
        {
            // Prefer the largest single denom that divides amount
            if (amount % 100 == 0) return new Bills { n100 = amount / 100 };
            if (amount % 20 == 0) return new Bills { n20 = amount / 20 };
            if (amount % 10 == 0) return new Bills { n10 = amount / 10 };
            if (amount % 5 == 0) return new Bills { n5 = amount / 5 };
            // Not exactly divisible by a single denom -> fallback to fewest bills
            return Breakdown(amount);
        }

        private static Dictionary<Outcome, float> EnsureWeights(IDictionary<Outcome, float> weights)
        {
            // Defaults (tweak to taste): heavy on $20s; some $10/$5; small chance of $1 or $100.
            var defaults = new Dictionary<Outcome, float>
            {
                { Outcome.NextTwenty, 0.55f },
                { Outcome.NextTen,    0.20f },
                { Outcome.NextFive,   0.15f },
                { Outcome.NextHundred,0.07f },
                { Outcome.NextDollar, 0.03f },
            };

            if (weights == null) return defaults;

            // Copy, sanitize (no negatives/NaN), and check if all <= 0
            var copy = new Dictionary<Outcome, float>();
            float sum = 0f;
            foreach (Outcome o in Enum.GetValues(typeof(Outcome)))
            {
                weights.TryGetValue(o, out float v);
                if (float.IsNaN(v) || v < 0f) v = 0f;
                copy[o] = v;
                sum += v;
            }

            // If everything is zero, fallback to defaults
            if (sum <= 0f) return defaults;
            return copy;
        }

        private static Outcome WeightedPick(IReadOnlyDictionary<Outcome, float> weights)
        {
            // Sum weights
            float total = 0f;
            foreach (var kv in weights) total += kv.Value;
            // Safety: if degenerate, choose a deterministic fallback
            if (total <= 0f) return Outcome.NextTwenty;

            float r = UnityEngine.Random.value * total;
            foreach (var kv in weights)
            {
                r -= kv.Value;
                if (r <= 0f) return kv.Key;
            }
            // Fallback (floating point edge)
            foreach (var kv in weights) return kv.Key;
            return Outcome.NextTwenty;
        }
    }
}

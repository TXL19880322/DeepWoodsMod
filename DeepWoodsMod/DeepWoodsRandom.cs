﻿using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using static DeepWoodsMod.DeepWoodsEnterExit;

namespace DeepWoodsMod
{
    class DeepWoodsRandom
    {
        private const int MAGIC_SALT = 854574563;

        private readonly int seed;
        private readonly Random random;
        private int masterModeCounter;

        public class Probability
        {
            public const int PROCENT = 100;
            public const int PROMILLE = 1000;

            public readonly static Probability FIFTY_FIFTY = new Probability(50);

            private int probability;
            private int range;

            public Probability(int probability, int range = PROCENT)
            {
                this.probability = probability;
                this.range = range;
            }

            public int GetValue()
            {
                return this.probability;
            }

            public int GetRange()
            {
                return this.range;
            }
        }

        public DeepWoodsRandom(int level, EnterDirection enterDir, int? salt)
        {
            this.seed = CalculateSeed(level, enterDir, salt);
            this.random = new Random(this.seed);
            this.masterModeCounter = 0;
        }

        public bool IsInMasterMode()
        {
            return this.masterModeCounter > 0;
        }

        public int GetSeed()
        {
            return this.seed;
        }

        private int CalculateSeed(int level, EnterDirection enterDir, int? salt)
        {
            if (level == 1)
            {
                // This is the "root" DeepWoods level, always use UniqueMultiplayerID as seed.
                // This makes sure the first level stays the same for the entire game, but still be different for each unique game experience.
                return GetHashFromUniqueMultiplayerID() ^ MAGIC_SALT;
            }
            else
            {
                // Calculate seed from multiplayer ID, DeepWoods level, enter direction and time since start.
                // This makes sure the seed is the same for all players entering the same DeepWoods level during the same game hour,
                // but still makes it unique for each game and pseudorandom enough for players to not be able to reasonably predict the woods.
                return GetHashFromUniqueMultiplayerID() ^ UniformAnyInt(level) ^ UniformAnyInt((int)enterDir) ^ UniformAnyInt(HoursSinceStart()) ^ salt.Value;
            }
        }

        private int UniformAnyInt(int x)
        {
            // From https://stackoverflow.com/a/12996028/9199167
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = (x >> 16) ^ x;
            return x;
        }

        private int GetHashFromUniqueMultiplayerID()
        {
            long uniqueMultiplayerID = Game1.MasterPlayer.UniqueMultiplayerID;
            return UniformAnyInt((int)((uniqueMultiplayerID >> 32) ^ uniqueMultiplayerID));
        }

        private int HoursSinceStart()
        {
            int hourOfDay = 1 + (Game1.timeOfDay - 600) / 100;
            return hourOfDay + SDate.Now().DaysSinceStart * 20;
        }

        public bool GetChance(Probability probability)
        {
            return GetRandomValue(0, probability.GetRange()) < probability.GetValue();
        }

        public Random GetRandom()
        {
            if (this.IsInMasterMode())
            {
                return Game1.random;
            }
            else
            {
                return this.random;
            }
        }

        public int GetRandomValue(int min, int max)
        {
            if (this.IsInMasterMode())
            {
                return GetRandom().Next(min, max);
            }
            else
            {
                return GetRandom().Next(min, max);
            }
        }

        public int GetRandomValue(int[] values, Probability firstValueProbability = null)
        {
            if (firstValueProbability != null)
            {
                if (GetChance(firstValueProbability))
                {
                    return values[0];
                }
                else
                {
                    return values[GetRandomValue(1, values.Length)];
                }
            }
            else
            {
                return values[GetRandomValue(0, values.Length)];
            }
        }

        public void EnterMasterMode()
        {
            // Master Mode is used when generating interactive content (monsters, terrain features, loot etc.)
            // These things are only generated by the server (while the map itself is generated on every client, hence the shared seed),
            // so when in master mode, we use Game1.random instead of our own random.
            // This ensures server-side only generation doesn't mess with shared generation (as the shared random stays in sync).
            this.masterModeCounter++;
        }

        public void LeaveMasterMode()
        {
            this.masterModeCounter--;
        }
    }
}

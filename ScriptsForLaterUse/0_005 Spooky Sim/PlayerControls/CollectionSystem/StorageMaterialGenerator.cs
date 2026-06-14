using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cherry.DayAndTime;
using static Cherry.Inventory.StorageContainer;

namespace Cherry.Inventory
{
    [AddComponentMenu("Cherry/Inventory/Storage Material Generator")]
    public class StorageMaterialGenerator : MonoBehaviour, IStorageAbsorbRule
    {
        [Serializable]
        public class GeneratorRecipe
        {
            [Header("Recipe")]
            public string label;

            [Tooltip("The item the player inserts into this container.")]
            public ItemDefinition generatorObject;

            [Tooltip("The material this object slowly creates.")]
            public ItemDefinition generatedMaterial;

            [Header("Physical Representation")]
            [Tooltip("Prefab spawned inside the container when this generator object is inserted.")]
            public GameObject physicalPrefab;

            [Tooltip("Local position offset from the Physical Object Anchor.")]
            public Vector3 localPositionOffset = Vector3.zero;

            [Tooltip("Local rotation offset from the Physical Object Anchor.")]
            public Vector3 localEulerOffset = Vector3.zero;

            [Tooltip("Local scale for the spawned physical object.")]
            public Vector3 localScale = Vector3.one;

            [Header("Generation")]
            [Tooltip("How many units count as full. If 0, uses StorageContainer SlotCapacity * StackLimit.")]
            [Min(0)] public int unitsWhenFull = 0;

            [Tooltip("How many in-game minutes this recipe takes to fill from empty to full. 1440 = 24 in-game hours.")]
            [Min(1)] public int fillDurationGameMinutesOverride = 0;
        }

        [Header("References")]
        [SerializeField] private StorageContainer outputContainer;

        [Tooltip("Where the physical generator object prefab appears.")]
        [SerializeField] private Transform physicalObjectAnchor;

        [Header("Recipes")]
        [SerializeField] private List<GeneratorRecipe> recipes = new List<GeneratorRecipe>();

        [Tooltip("If true, placing a valid generator object directly into the StorageContainer will absorb it and start generating.")]
        [SerializeField] private bool autoAbsorbGeneratorObjectsPlacedInStorage = true;

        [Tooltip("Usually false. If true, a new valid generator object can replace the active one.")]
        [SerializeField] private bool allowReplacingActiveGenerator = false;

        [Header("Game Time")]
        [Tooltip("How many in-game minutes it takes to fill the container from empty to full. 1440 = 24 in-game hours.")]
        [SerializeField, Min(1)] private int fillDurationGameMinutes = 1440;

        [Tooltip("If true, the sleep jump to next day also advances generation.")]
        [SerializeField] private bool generateDuringSleepTimeJump = true;

        [Tooltip("If true, generation only happens while the store phase is Open.")]
        [SerializeField] private bool onlyGenerateWhileStoreOpen = false;

        [Header("Output Rules")]
        [Tooltip("If true, generation pauses if the output container has anything except the generated material.")]
        [SerializeField] private bool pauseIfContainerHasOtherMaterials = true;

        [Header("Debug")]
        [SerializeField] private int activeRecipeIndex = -1;
        [SerializeField] private float generatedUnitBuffer = 0f;
        [SerializeField] private bool hasTimeSample;
        [SerializeField] private int lastSampleDayNumber;
        [SerializeField] private int lastSampleMinuteOfDay;

        [Header("Events")]
        public UnityEvent OnGeneratorInserted;
        public UnityEvent OnGeneratorRemoved;
        public UnityEvent OnMaterialGenerated;

        private GameObject spawnedPhysicalObject;
        private bool handlingStorageChange;
        private DayTimeSystem dayTimeSystem;

        public bool HasActiveGenerator => ActiveRecipe != null;

        public GeneratorRecipe ActiveRecipe
        {
            get
            {
                if (activeRecipeIndex < 0 || activeRecipeIndex >= recipes.Count)
                    return null;

                return recipes[activeRecipeIndex];
            }
        }

        private void Awake()
        {
            if (outputContainer == null)
                outputContainer = GetComponent<StorageContainer>();
        }

        private void Start()
        {
            TryBindDayTimeSystem();
            SampleCurrentGameTime();
        }

        private void OnEnable()
        {
            if (outputContainer != null)
                outputContainer.OnStorageChanged += HandleStorageChanged;

            TryBindDayTimeSystem();

            if (autoAbsorbGeneratorObjectsPlacedInStorage)
                TryAbsorbGeneratorObjectFromStorage();
        }

        private void OnDisable()
        {
            if (outputContainer != null)
                outputContainer.OnStorageChanged -= HandleStorageChanged;

            UnbindDayTimeSystem();
        }

        // ------------------------------------------------------------
        // Storage absorb gatekeeper
        // ------------------------------------------------------------

        public bool CanAbsorbItem(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0)
                return false;

            // If a generator is already installed, this container should not physically accept anything else.
            if (HasActiveGenerator && !allowReplacingActiveGenerator)
                return false;

            // Before the generator is installed, only accept valid generator objects.
            return FindRecipeIndexForObject(item) >= 0;
        }

        // ------------------------------------------------------------
        // Game time binding
        // ------------------------------------------------------------

        private void TryBindDayTimeSystem()
        {
            if (dayTimeSystem != null)
                return;

            dayTimeSystem = DayTimeSystem.Instance;

            if (dayTimeSystem == null)
                return;

            dayTimeSystem.OnTimeChanged += HandleGameTimeChanged;
            dayTimeSystem.OnNewDayStarted += HandleNewDayStarted;
        }

        private void UnbindDayTimeSystem()
        {
            if (dayTimeSystem == null)
                return;

            dayTimeSystem.OnTimeChanged -= HandleGameTimeChanged;
            dayTimeSystem.OnNewDayStarted -= HandleNewDayStarted;
            dayTimeSystem = null;
        }

        private void SampleCurrentGameTime()
        {
            if (dayTimeSystem == null)
                TryBindDayTimeSystem();

            if (dayTimeSystem == null)
                return;

            lastSampleDayNumber = dayTimeSystem.DayNumber;
            lastSampleMinuteOfDay = dayTimeSystem.MinuteOfDay;
            hasTimeSample = true;
        }

        private void HandleNewDayStarted(int newDayNumber)
        {
            // DayTimeSystem already invokes OnTimeChanged during SleepToNextDay.
            // This is just a safety sample in case event order changes later.
            SampleCurrentGameTime();
        }

        private void HandleGameTimeChanged(int minuteOfDay)
        {
            if (dayTimeSystem == null)
                return;

            if (!hasTimeSample)
            {
                SampleCurrentGameTime();
                return;
            }

            int currentDay = dayTimeSystem.DayNumber;
            int currentMinute = dayTimeSystem.MinuteOfDay;

            int previousAbsoluteMinute = (lastSampleDayNumber * 1440) + lastSampleMinuteOfDay;
            int currentAbsoluteMinute = (currentDay * 1440) + currentMinute;

            int deltaGameMinutes = currentAbsoluteMinute - previousAbsoluteMinute;

            lastSampleDayNumber = currentDay;
            lastSampleMinuteOfDay = currentMinute;

            if (deltaGameMinutes <= 0)
                return;

            if (!generateDuringSleepTimeJump && deltaGameMinutes > 120)
                return;

            if (onlyGenerateWhileStoreOpen && dayTimeSystem.Phase != DayPhase.Open)
                return;

            AdvanceGenerationByGameMinutes(deltaGameMinutes);
        }

        // ------------------------------------------------------------
        // Storage interaction
        // ------------------------------------------------------------

        private void HandleStorageChanged()
        {
            if (!autoAbsorbGeneratorObjectsPlacedInStorage)
                return;

            TryAbsorbGeneratorObjectFromStorage();
        }

        public bool TryInsertObject(ItemDefinition generatorObject)
        {
            if (generatorObject == null)
                return false;

            if (!CanAbsorbItem(generatorObject, 1))
                return false;

            int recipeIndex = FindRecipeIndexForObject(generatorObject);

            if (recipeIndex < 0)
                return false;

            return TryStartRecipe(recipeIndex);
        }

        public bool TryAbsorbGeneratorObjectFromStorage()
        {
            if (handlingStorageChange)
                return false;

            if (outputContainer == null)
                return false;

            if (!CanAcceptNewGenerator())
                return false;

            IReadOnlyList<ItemStack> slots = outputContainer.Slots;

            handlingStorageChange = true;

            try
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    ItemStack stack = slots[i];

                    if (stack.IsEmpty || stack.item == null || stack.amount <= 0)
                        continue;

                    int recipeIndex = FindRecipeIndexForObject(stack.item);

                    if (recipeIndex < 0)
                    {
                        // This should not happen anymore because CanAbsorbItem blocks invalid drops.
                        continue;
                    }

                    ItemDefinition removedItem = stack.item;

                    int removed = outputContainer.TryRemoveAtIndex(i, 1);

                    if (removed <= 0)
                        continue;

                    bool started = TryStartRecipe(recipeIndex);

                    if (started)
                        return true;

                    // Rollback if something went wrong.
                    int addedBack = outputContainer.TryAdd(removedItem, 1);

                    if (addedBack > 0)
                        outputContainer.NotifyStorageChanged();

                    return false;
                }
            }
            finally
            {
                handlingStorageChange = false;
            }

            return false;
        }

        // ------------------------------------------------------------
        // Generation
        // ------------------------------------------------------------

        public void AdvanceGenerationByGameMinutes(int gameMinutes)
        {
            if (gameMinutes <= 0)
                return;

            GeneratorRecipe recipe = ActiveRecipe;

            if (recipe == null)
                return;

            if (recipe.generatedMaterial == null || outputContainer == null)
                return;

            if (pauseIfContainerHasOtherMaterials && ContainerHasAnyItemOtherThan(recipe.generatedMaterial))
                return;

            int unitsWhenFull = GetUnitsWhenFull(recipe);
            int currentUnits = CountUnits(recipe.generatedMaterial);

            if (currentUnits >= unitsWhenFull)
            {
                generatedUnitBuffer = 0f;
                return;
            }

            int availableByTarget = unitsWhenFull - currentUnits;
            int availableByStorage = GetAddableSpaceFor(recipe.generatedMaterial);
            int available = Mathf.Min(availableByTarget, availableByStorage);

            if (available <= 0)
            {
                generatedUnitBuffer = Mathf.Min(generatedUnitBuffer, 0.999f);
                return;
            }

            int durationMinutes = GetFillDurationGameMinutes(recipe);
            float unitsPerGameMinute = unitsWhenFull / (float)durationMinutes;

            generatedUnitBuffer += gameMinutes * unitsPerGameMinute;

            int unitsToCreate = Mathf.FloorToInt(generatedUnitBuffer);

            if (unitsToCreate <= 0)
                return;

            unitsToCreate = Mathf.Min(unitsToCreate, available);

            int added = outputContainer.TryAdd(recipe.generatedMaterial, unitsToCreate);

            if (added > 0)
            {
                generatedUnitBuffer -= added;
                outputContainer.NotifyStorageChanged();
                OnMaterialGenerated?.Invoke();
            }
            else
            {
                generatedUnitBuffer = Mathf.Min(generatedUnitBuffer, 0.999f);
            }
        }

        public bool RemoveGeneratorObject()
        {
            if (ActiveRecipe == null)
                return false;

            ClearActiveGenerator();
            return true;
        }

        public bool RemoveGeneratorObject(out ItemDefinition removedObject)
        {
            removedObject = null;

            GeneratorRecipe recipe = ActiveRecipe;

            if (recipe == null)
                return false;

            removedObject = recipe.generatorObject;

            ClearActiveGenerator();

            return true;
        }

        // ------------------------------------------------------------
        // Internal helpers
        // ------------------------------------------------------------

        private bool TryStartRecipe(int recipeIndex)
        {
            if (recipeIndex < 0 || recipeIndex >= recipes.Count)
                return false;

            if (ActiveRecipe != null)
            {
                if (!allowReplacingActiveGenerator)
                    return false;

                ClearActiveGenerator();
            }

            GeneratorRecipe recipe = recipes[recipeIndex];

            if (recipe == null)
                return false;

            if (recipe.generatorObject == null || recipe.generatedMaterial == null)
                return false;

            activeRecipeIndex = recipeIndex;
            generatedUnitBuffer = 0f;

            SpawnPhysicalObject(recipe);
            SampleCurrentGameTime();

            OnGeneratorInserted?.Invoke();

            return true;
        }

        private bool CanAcceptNewGenerator()
        {
            return ActiveRecipe == null || allowReplacingActiveGenerator;
        }

        private void ClearActiveGenerator()
        {
            activeRecipeIndex = -1;
            generatedUnitBuffer = 0f;

            if (spawnedPhysicalObject != null)
                Destroy(spawnedPhysicalObject);

            spawnedPhysicalObject = null;

            OnGeneratorRemoved?.Invoke();
        }

        private void SpawnPhysicalObject(GeneratorRecipe recipe)
        {
            if (spawnedPhysicalObject != null)
                Destroy(spawnedPhysicalObject);

            if (recipe.physicalPrefab == null)
                return;

            if (physicalObjectAnchor != null)
            {
                spawnedPhysicalObject = Instantiate(recipe.physicalPrefab, physicalObjectAnchor);

                spawnedPhysicalObject.transform.localPosition = recipe.localPositionOffset;
                spawnedPhysicalObject.transform.localRotation = Quaternion.Euler(recipe.localEulerOffset);
                spawnedPhysicalObject.transform.localScale = recipe.localScale;
            }
            else
            {
                spawnedPhysicalObject = Instantiate(recipe.physicalPrefab, transform);

                spawnedPhysicalObject.transform.localPosition = recipe.localPositionOffset;
                spawnedPhysicalObject.transform.localRotation = Quaternion.Euler(recipe.localEulerOffset);
                spawnedPhysicalObject.transform.localScale = recipe.localScale;
            }
        }

        private int FindRecipeIndexForObject(ItemDefinition generatorObject)
        {
            if (generatorObject == null)
                return -1;

            for (int i = 0; i < recipes.Count; i++)
            {
                GeneratorRecipe recipe = recipes[i];

                if (recipe == null)
                    continue;

                if (recipe.generatorObject == generatorObject)
                    return i;
            }

            return -1;
        }

        private int GetUnitsWhenFull(GeneratorRecipe recipe)
        {
            if (recipe.unitsWhenFull > 0)
                return recipe.unitsWhenFull;

            if (outputContainer == null)
                return 1;

            int slots = Mathf.Max(1, outputContainer.SlotCapacity);
            int stackLimit = Mathf.Max(1, outputContainer.StackLimit);

            return slots * stackLimit;
        }

        private int GetFillDurationGameMinutes(GeneratorRecipe recipe)
        {
            if (recipe.fillDurationGameMinutesOverride > 0)
                return Mathf.Max(1, recipe.fillDurationGameMinutesOverride);

            return Mathf.Max(1, fillDurationGameMinutes);
        }

        private int CountUnits(ItemDefinition item)
        {
            if (outputContainer == null || item == null)
                return 0;

            int total = 0;
            IReadOnlyList<ItemStack> slots = outputContainer.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                ItemStack stack = slots[i];

                if (!stack.IsEmpty && stack.item == item)
                    total += Mathf.Max(0, stack.amount);
            }

            return total;
        }

        private bool ContainerHasAnyItemOtherThan(ItemDefinition allowedItem)
        {
            if (outputContainer == null)
                return false;

            IReadOnlyList<ItemStack> slots = outputContainer.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                ItemStack stack = slots[i];

                if (stack.IsEmpty)
                    continue;

                if (stack.item != allowedItem)
                    return true;
            }

            return false;
        }

        private int GetAddableSpaceFor(ItemDefinition item)
        {
            if (outputContainer == null || item == null)
                return 0;

            int space = 0;
            int stackLimit = Mathf.Max(1, outputContainer.StackLimit);
            int itemMaxStack = Mathf.Max(1, item.ItemMaxStack);
            int cap = Mathf.Min(stackLimit, itemMaxStack);

            IReadOnlyList<ItemStack> slots = outputContainer.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                ItemStack stack = slots[i];

                if (stack.IsEmpty)
                {
                    space += cap;
                }
                else if (stack.item == item)
                {
                    space += Mathf.Max(0, cap - stack.amount);
                }
            }

            return space;
        }
    }
}
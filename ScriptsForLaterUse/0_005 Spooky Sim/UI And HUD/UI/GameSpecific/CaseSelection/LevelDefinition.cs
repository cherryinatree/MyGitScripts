using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "Cherry/Levels/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Identity & Routing")]
        [SerializeField, Tooltip("Stable ID; auto-fills. You can keep the GUID or set a readable ID.")]
        private string levelId;
        [Tooltip("Scene name or Addressables key used to load the level.")]
        public string sceneName;
        [Tooltip("Optional category/tag for catalog filtering (e.g., 'Story', 'Contracts').")]
        public string category;

        [Header("Presentation")]
        public string displayName;
        [TextArea] public string description;
        public Sprite thumbnail;

        [Header("Requirements")]
        public int requirements = 0;
        [Tooltip("Recommended player level (for UI only).")]
        public int recommendedLevel = 1;

        [Header("Randomization Rules (rolled on select)")]
        [Tooltip("Inclusive min/max. Integers for simplicity; change to float if needed.")]
        public int rewardMin = 50;
        public int rewardMax = 150;

        [Tooltip("How many modifiers to roll (0 = none).")]
        [Min(0)] public int modifiersToRoll = 0;

        public string LevelId => string.IsNullOrWhiteSpace(levelId) ? name : levelId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                levelId = GUID.Generate().ToString();
                EditorUtility.SetDirty(this);
            }
            if (rewardMax < rewardMin) rewardMax = rewardMin;
        }
#endif
        
    public LevelRunSaveData ConvertToLevelRunSaveData()
    {
        LevelRunSaveData saveData = new LevelRunSaveData();
        saveData.levelId = this.LevelId;
        saveData.sceneName = this.sceneName;
        saveData.displayName = this.displayName;
        saveData.category = this.category;
        saveData.rolledReward = UnityEngine.Random.Range(this.rewardMin, this.rewardMax + 1);

        return saveData;

    }

    }


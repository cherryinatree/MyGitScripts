using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardSetUp : MonoBehaviour
{

    public bool isGenerated = false;

    List<Cube> startingCubes;
    public GameObject CharacterPrefab;

    List<Character> monsters;
    List<Character> enemies;
    List<Character> villans;

    EnemyManager enemyManager;

    // Start is called before the first frame update
    void Start()
    {
        //SaveManipulator.TestNewSaveAddAllCharacters();
        SaveManipulator.LoadSceneChange();

        enemyManager = new EnemyManager();
        CombatSingleton.Instance.Combatants = new List<GameObject>();
        GamingTools.SoundController.SetMusic("Sounds/Music/BattleSong1");

        LoadStartingCubes();

        DetermineLoadCharacterStyle();

        ResetSpawnCubes();
    }

    private void DetermineLoadCharacterStyle()
    {
        if (isGenerated)
        {
            if (SaveData.Current.mainData.loadSceneData.isLoad)
            {
                LoadCharactersInCorrectPlace();
                LoadEnemiesInCorrectPlace();
            }
            else
            {
                SpawnCharacters();
                LoadTheOpposition();
            }
        }
        else
        {
            if (SaveData.Current.mainData.loadSceneData.isLoad)
            {
                ResetSpawnCubes();
                LoadCharactersInCorrectPlace();
                GatherNonGeneratedEnemies();
                LoadNonGeneratedEnemiesInCorrectPlace();
               // SetUpSaveObjects();
            }
            else
            {
                //CombatSingleton.Instance.SaveObjects = new List<GameObject>();
                SpawnCharacters();
                GatherNonGeneratedEnemies();
                ResetCharacters();
            }
        }
    }

    private void SetUpSaveObjects()
    {
        foreach(GameObject gObject in CombatSingleton.Instance.SaveObjects)
        {
            Debug.Log(gObject.name);
            foreach(MyChange myChange in SaveData.Current.mainData.loadSceneData.boardChanges.changes)
            {
                if(gObject.name == myChange.nameID)
                {
                    gObject.GetComponent<MyDataUploader>().LoadPreviousChanges(myChange);
                }
            }
        }
    }

    private void ResetCharacters()
    {
        foreach(GameObject character in CombatSingleton.Instance.Combatants)
        {
            character.GetComponent<CombatCharacter>().ResetCharacter();
        }
    }

    private void ResetSpawnCubes()
    {
        foreach(Cube cube in CombatSingleton.Instance.Cubes)
        {
            cube.ResetSpawnCubes();
        }
    }

    private void InstantiateCharacters(List<Character> characters, GROUNDTYPE type, int team)
    {
        string route = "Prefabs/";
        foreach (Character characterStats in characters)
        {
            foreach (Cube cube in startingCubes)
            {
                if (cube.MyType == type)
                {
                    string path = route + characterStats.modelPath;

                    GameObject body = GamingTools.ResourseLoader.GetGameObject(path);
                    GameObject character = GameObject.Instantiate(body);

                    character.GetComponent<CombatCharacter>().NewCube(cube.gameObject);
                    character.GetComponent<CombatCharacter>().SetStats(characterStats, team);
                    //character.transform.position = new Vector3(cube.transform.position.x, cube.transform.position.y + 0.5f, cube.transform.position.z);

                    CombatSingleton.Instance.Combatants.Add(character);
                    break;
                }
            }
        }
    }



    private void SpawnCharacters()
    {
        InstantiateCharacters(SaveData.Current.mainData.characters, GROUNDTYPE.PlayerSpawn, 0);
        
    }

    private void LoadTheOpposition()
    {
        if (SaveData.Current.mainData.loadSceneData.isBattle == false)
        {
            if (SaveData.Current.mainData.battleLoadData == null)
            {
                SetUpBoardsave();
            }
            if (SaveData.Current.mainData.battleLoadData.villian == true)
            {
                JsonRetriever jsonRetriever = new JsonRetriever();
                List<Character> villianList = jsonRetriever.LoadAllCharacters(RetrieverConstants.JsonVillian);
                villans = new List<Character>();
                for (int i = 0; i < SaveData.Current.mainData.battleLoadData.whichVillian.Length; i++)
                {
                    foreach (Character character in villianList)
                    {
                        if (character.id == SaveData.Current.mainData.battleLoadData.whichVillian[i])
                        {
                            villans.Add(character);
                        }
                    }
                }

                InstantiateCharacters(villans, GROUNDTYPE.EnemySpawn, 1);
            }
            if (SaveData.Current.mainData.battleLoadData.enemy == true)
            {


                enemies = new List<Character>();
                for (int i = 0; i < SaveData.Current.mainData.battleLoadData.enemyGroupCount; i++)
                {
                    Character enemy = enemyManager.GetRandomEnemy(SaveData.Current.mainData.battleLoadData.minLevel,
                        SaveData.Current.mainData.battleLoadData.maxLevel);

                    for (int x = 0; x < SaveData.Current.mainData.battleLoadData.enemyPerGroup; x++)
                    {
                        enemies.Add(enemyManager.GetEnemyById(enemy.id));
                    }
                }

                InstantiateCharacters(enemies, GROUNDTYPE.EnemySpawn, 1);
            }
            if (SaveData.Current.mainData.battleLoadData.monster == true)
            {
                monsters = new List<Character>();
                for (int i = 0; i < SaveData.Current.mainData.battleLoadData.monsterGroupCount; i++)
                {
                    Character monster = enemyManager.GetRandomMonster(SaveData.Current.mainData.battleLoadData.minLevel,
                        SaveData.Current.mainData.battleLoadData.maxLevel);

                    for (int x = 0; x < SaveData.Current.mainData.battleLoadData.monsterPerGroup; x++)
                    {
                        monsters.Add(enemyManager.GetMonsterById(monster.id));
                    }
                }

                InstantiateCharacters(monsters, GROUNDTYPE.EnemySpawn, 1);
            }
        }
        else
        {

        }
    }

    private void GatherNonGeneratedEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        JsonRetriever jsonRetriever = new JsonRetriever();
        

        foreach (GameObject enemy in enemies)
        {

            enemy.GetComponent<CombatCharacter>().SetStats(enemyManager.GetMonsterById(
                enemy.GetComponent<EnemyIdentify>().enemyId), 1);
            CombatSingleton.Instance.Combatants.Add(enemy);
        }
    }

    private void LoadEnemiesInCorrectPlace()
    {

    }

    private void LoadCharactersInCorrectPlace()
    {
        string route = "Prefabs/";
        foreach (Character characterStats in SaveData.Current.mainData.loadSceneData.playerList)
        {
            if (characterStats.currentHealth > 0)
            {
                GameObject myCube = GameObject.Find(characterStats.myCubeName);


                string path = route + characterStats.modelPath;

                GameObject body = GamingTools.ResourseLoader.GetGameObject(path);
                GameObject character = GameObject.Instantiate(body);

                character.GetComponent<CombatCharacter>().NewCube(myCube.gameObject);
                character.GetComponent<CombatCharacter>().SetStats(characterStats, 0);
                character.GetComponent<CombatCharacter>().FaceDirection(character.GetComponent<CombatCharacter>().myStats.facing);
                character.transform.position = new Vector3(myCube.transform.position.x, 
                    myCube.transform.position.y + ContantCharacterPosition.Y_addedToCubePosition, myCube.transform.position.z);

                CombatSingleton.Instance.Combatants.Add(character);



            }
            
        }
    }

    private void LoadNonGeneratedEnemiesInCorrectPlace()
    {
        List<int> dead = new List<int>(); 
        List<GameObject> enemies = new List<GameObject>();
        foreach(GameObject character in CombatSingleton.Instance.Combatants)
        {
            if(character.GetComponent<CombatCharacter>().team != 0)
            {
                enemies.Add(character);
            }
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            bool isDead = true;
            for (int x = 0; x < SaveData.Current.mainData.loadSceneData.enemyList.Count; x++)
            {
                if(enemies[i].name == SaveData.Current.mainData.loadSceneData.enemyList[x].characterName)
                {
                    isDead = false;
                    break;
                }
            }
            if (isDead)
            {
                dead.Add(i);
            }
        }

        for (int i = 0; i < dead.Count; i++)
        {
            CombatSingleton.Instance.Combatants.Remove(enemies[dead[i]]);
            Destroy(enemies[dead[i]]);
        }

        foreach (GameObject character in enemies)
        {
            for (int i = 0; i < SaveData.Current.mainData.loadSceneData.enemyList.Count; i++)
            {
                if(character.name == SaveData.Current.mainData.loadSceneData.enemyList[i].characterName)
                {

                    GameObject myCube = GameObject.Find(SaveData.Current.mainData.loadSceneData.enemyList[i].myCubeName);

                    character.GetComponent<CombatCharacter>().NewCube(myCube);
                    character.GetComponent<CombatCharacter>().SetStats(SaveData.Current.mainData.loadSceneData.enemyList[i], 1);
                    character.GetComponent<CombatCharacter>().FaceDirection(character.GetComponent<CombatCharacter>().myStats.facing);

                    character.transform.position = new Vector3(myCube.transform.position.x, 
                        myCube.transform.position.y + 0.5f, myCube.transform.position.z);
                }
            }
        }
    }

    private void SetUpBoardsave()
    {
        SaveData.Current.mainData.battleLoadData = new BattleLoadData();
        SaveData.Current.mainData.battleLoadData.villian = false;
        SaveData.Current.mainData.battleLoadData.enemy = false;
        SaveData.Current.mainData.battleLoadData.monster = true;


        SaveData.Current.mainData.battleLoadData.minLevel = 1;
        SaveData.Current.mainData.battleLoadData.maxLevel = 1;
        SaveData.Current.mainData.battleLoadData.enemyGroupCount = 1;
        SaveData.Current.mainData.battleLoadData.enemyPerGroup = 3;
        SaveData.Current.mainData.battleLoadData.monsterGroupCount = 1;
        SaveData.Current.mainData.battleLoadData.monsterPerGroup = 4;

    }

    private void LoadStartingCubes()
    {
        startingCubes = new List<Cube>();
        foreach (Cube cube in CombatSingleton.Instance.Cubes)
        {
            if(cube.MyType == GROUNDTYPE.PlayerSpawn || cube.MyType == GROUNDTYPE.EnemySpawn)
            {
                startingCubes.Add(cube);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

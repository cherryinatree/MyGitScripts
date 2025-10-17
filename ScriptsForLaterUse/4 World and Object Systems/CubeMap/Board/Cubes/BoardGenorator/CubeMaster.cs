using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CubeMaster : MonoBehaviour
{
    public Texture2D map;
    public GameObject cubeParent;
    public GameObject enemiesParent;
    public bool hasWalls = false;
    public bool hasProps = false;
    public CubeCorridantor[] corridants;

    EnemyManager enemyManager;
    // Start is called before the first frame update
   public void MakeBoard()
    {
        enemyManager = new EnemyManager();


        for (int i = cubeParent.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(cubeParent.transform.GetChild(i).gameObject);
        }
        for (int i = enemiesParent.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(enemiesParent.transform.GetChild(i).gameObject);
        }

        for (int x = 0; x < map.width; x++)
        {
            for(int y = 0; y < map.height; y++)
            {
                GenerateCube(x, y);
            }
        }
    }

    public void ClearBoard()
    {

        for (int i = cubeParent.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(cubeParent.transform.GetChild(i).gameObject);
        }
        for (int i = enemiesParent.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(enemiesParent.transform.GetChild(i).gameObject);
        }
    }

    private void GenerateCube(int x, int y)
    {
        Color pixel = map.GetPixel(x, y);

        if(pixel.a == 0)
        {
            return;
        }

        Color32 pixelColor = pixel;
        foreach (var corridant in corridants)
        {
            Debug.Log(corridant.color.a);
            if (corridant.color.a == pixelColor.a)
            {
                GameObject cubes = Instantiate(corridant.Prefab);
                cubes.transform.position = new Vector3(x,(pixelColor.b/2f), y);
                cubes.transform.parent = cubeParent.transform;
                CubeType(cubes, pixelColor);
                if (hasWalls)
                {
                    GenerateWall(x, y, cubes, pixelColor);
                }
                if (hasProps)
                {
                    GenerateProps(cubes, pixelColor);
                }
            }
        }

    }

    private void GenerateProps(GameObject cubes, Color32 pixelColor)
    {
        if((int)pixelColor.r == 61)
        {
            GameObject[] props = GamingTools.ResourseLoader.GetMultipleGameObjects("GeneratedProps/Props/DungeonProps");


            GameObject wall = Instantiate(props[(int)pixelColor.g]);
            wall.transform.position = new Vector3(cubes.transform.position.x, cubes.transform.position.y + 1, cubes.transform.position.z);
            wall.transform.parent = cubeParent.transform;
            cubes.GetComponent<CubePhase>().type = GROUNDTYPE.Obstacle;
        }


    }

    private void GenerateWall(int x, int y, GameObject cubes, Color32 pixelColor)
    {
        if (map.GetPixel(x, y - 1).a == 0)
        {
            PlaceWall(cubes, 0);
        }

        if (map.GetPixel(x, y + 1).a == 0)
        {
            PlaceWall(cubes, 1);
        }

        if (map.GetPixel(x-1, y).a == 0)
        {
            PlaceWall(cubes, 2);
        }

        if (map.GetPixel(x+1, y).a == 0)
        {
            PlaceWall(cubes, 3);
        }


    }


    private void PlaceWall(GameObject cubes, int direnction)
    {
        string whichWall;

        if (direnction == 0)
        {
            whichWall = "GeneratedProps/Walls/Up/WallUp";
        }
        else if (direnction == 1)
        {
            whichWall = "GeneratedProps/Walls/Down/WallDown";
        }
        else if(direnction == 2)
        {
            whichWall = "GeneratedProps/Walls/Right/WallRight";
        }
        else
        {
            whichWall = "GeneratedProps/Walls/Left/WallLeft";
        }
        Debug.Log(whichWall);
        GameObject wall = Instantiate(GamingTools.ResourseLoader.GetGameObject(whichWall));
        wall.transform.position = cubes.transform.position;
        wall.transform.parent = cubeParent.transform;
    }


    private void CubeType(GameObject cube, Color32 pixelColor)
    {
        switch ((int)pixelColor.r)
        {
            case 0:
                MakeNormalCube(cube);
                return;
            case int i when i > 0 && i <= 20:
                MonsterSpawn(cube, pixelColor);
                return;
            case 21:
                MakeCursorCube(cube);
                return;
            case 22:
                MakePlayerSpawn(cube);
                return;
            case 23:
                MakeHazardCube(cube);
                return;
            case 24:
                MakeHealCube(cube);
                return;
            case 25:
                MakeBuffCube(cube);
                return;
            case 26:
                return;
            case int i when i > 40 && i <= 100:
                MakeObstacle(cube);
                return;
            case 81:
                return;


        }
    }


    private void MakeNormalCube(GameObject cube)
    {
        CubePhase cubePhase = cube.GetComponent<CubePhase>();
        CubePhaseTypeChange(cubePhase, "ground");
        CubePhasePhaseChange(cubePhase, "normal");
        CubePhaseModifyChange(cubePhase, "normal");
    }

    private void MakeCursorCube(GameObject cube)
    {
        CubePhase cubePhase = cube.GetComponent<CubePhase>();
        CubePhaseTypeChange(cubePhase, "ground");
        CubePhasePhaseChange(cubePhase, "cursorCube");
        CubePhaseModifyChange(cubePhase, "normal");
    }

    private void MakePlayerSpawn(GameObject cube)
    {
        CubePhase cubePhase = cube.GetComponent<CubePhase>();
        CubePhaseTypeChange(cubePhase, "playerSpawn");
        CubePhasePhaseChange(cubePhase, "normal");
        CubePhaseModifyChange(cubePhase, "normal");
    }

    private void MonsterSpawn(GameObject cube, Color32 pixelColor)
    {
        MakeNormalCube(cube);

        Character monster = enemyManager.GetMonsterById((int)pixelColor.g);
        GameObject monsterForm = Instantiate(GamingTools.ResourseLoader.GetGameObject("Prefabs/" +monster.modelPath));


        CombatCharacter monsterCharacter = monsterForm.GetComponent<CombatCharacter>();
        monsterCharacter.myCube = cube;
        Vector3 cubePosition = cube.transform.position;
        monsterForm.transform.position = new Vector3(cubePosition.x, cubePosition.y + 1f, cubePosition.z);
        monsterCharacter.myStats.level = (int)pixelColor.g;
        AdjustMonsterStats(monsterCharacter);

        monsterForm.tag = "Enemy";
        monsterForm.layer = 3;

        foreach(Transform t in monsterForm.transform)
        {
            t.gameObject.tag = "Enemy";
            t.gameObject.layer = 3;
        }

        monsterForm.transform.parent = enemiesParent.transform;
    }

    private void AdjustMonsterStats(CombatCharacter monster)
    {
        int[] stats = new int[5];
        stats[0] = monster.myStats.attack;
        stats[1] = monster.myStats.defense;
        stats[2] = monster.myStats.intelligence;
        stats[3] = monster.myStats.maxHealth;
        stats[4] = monster.myStats.maxMana;

        int highestStat = 0;
        int highestScore = 0;

        for (int i = 0; i < 3; i++)
        {
            if (stats[i] > highestScore) 
            {
                highestScore = stats[i];
                highestStat = i;
            }
        }



        if (monster.myStats.level > 4)
        {
            AddToStat(monster, highestStat);
        }
        if (monster.myStats.level > 5)
        {
            AddToStat(monster, highestStat);
        }
        if (monster.myStats.level > 6)
        {
            AddToStat(monster, 3);
        }
        if (monster.myStats.level > 7)
        {
            AddToStat(monster, 4);
        }
        if (monster.myStats.level > 8)
        {
            AddToStat(monster, highestStat);
        }
        if (monster.myStats.level > 9)
        {
            AddToStat(monster, highestStat);
        }
        if (monster.myStats.level > 10)
        {
            AddToStat(monster, 3);
        }
        if (monster.myStats.level > 11)
        {
            AddToStat(monster, 3);
        }
        if (monster.myStats.level > 12)
        {
            AddToStat(monster, 4);
        }
        if (monster.myStats.level > 13)
        {
            AddToStat(monster, highestStat);
        }
        if (monster.myStats.level > 14)
        {
            AddToStat(monster, highestStat);
        }
        if (monster.myStats.level > 15)
        {
            AddToStat(monster, highestStat);
        }
        if (monster.myStats.level > 16)
        {
            AddToStat(monster, 3);
        }
        if (monster.myStats.level > 17)
        {
            AddToStat(monster, 3);
        }
        if (monster.myStats.level > 18)
        {
            AddToStat(monster, 4);
        }
        if (monster.myStats.level > 19)
        {
            AddToStat(monster, highestStat);
        }
    }

    private void AddToStat(CombatCharacter character, int highestStat)
    {
        if (highestStat == 0)
        {

            character.myStats.AbilityPointsSpentAttack += 1;
        }
        if (highestStat == 1)
        {

            character.myStats.AbilityPointsSpentDefense += 1;
        }
        if (highestStat == 2)
        {
            character.myStats.AbilityPointsSpentIntelligence += 1;
        }
        if (highestStat == 3)
        {
            character.myStats.AbilityPointsSpentHealth += 1;
        }
        if (highestStat == 4)
        {
            character.myStats.AbilityPointsSpentMana += 1;
        }

        character.myStats.AbilityPointsSpent += 1;
    }

    private void MakeObstacle(GameObject cube)
    {

    }

    private void MakeHazardCube(GameObject cube)
    {

    }

    private void MakeHealCube(GameObject cube)
    {

    }

    private void MakeBuffCube(GameObject cube)
    {

    }




    private void CubePhaseTypeChange(CubePhase phase, string type)
    {
        switch (type)
        {
            case "ground":
                phase.type = GROUNDTYPE.Ground; break;
            case "playerSpawn":
                phase.type = GROUNDTYPE.PlayerSpawn; break;
            case "obstacle":
                phase.type = GROUNDTYPE.Obstacle; break;
            case "occupied":
                phase.type = GROUNDTYPE.Occupied; break;
            default:
                break;
        }
    }
    private void CubePhasePhaseChange(CubePhase phase, string type)
    {
        switch (type)
        {
            case "normal":
                phase.myPhase = CUBEPHASE.NORMAL; break;
            case "cursorCube":
                phase.myPhase = CUBEPHASE.CURSORCUBE;
                phase.previousPhase = CUBEPHASE.NORMAL; break;
        }
    }
    private void CubePhaseModifyChange(CubePhase phase, string type)
    {

        switch (type)
        {
            case "normal":
                phase.groundModify = GROUNDMODIFY.Normal; break;
            case "hazard":
                phase.groundModify = GROUNDMODIFY.Hazard; break;
            case "buff":
                phase.groundModify = GROUNDMODIFY.Buff; break;
            case "heal":
                phase.groundModify = GROUNDMODIFY.Heal; break;
        }
    }
}

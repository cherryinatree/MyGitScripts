using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

public class Construction : CharacterAbility
{

    public GameObject prefab;
    public GameObject ground;
    public Material placementMaterial;
    public Material cantPlaceMaterial;
    private Material[] origionalMaterial;
    private bool isPlacing = false;
    public float rotationSpeed = 100f;

    private GameObject building;

    private BuildObject buildingObject;

    protected override void Initialization()
    {
        base.Initialization();
    }

    public void Update()
    {

        if(isPlacing)
        {
            PlaceBuilding();
        }


    }

    public void StartPlacing(BuildObject buildMe)
    {

        isPlacing = true;
        building = Instantiate(buildMe.buildPrefab, gameObject.transform.position+(gameObject.transform.forward*2), Quaternion.identity);
        buildingObject = buildMe;
    }


    private void PlaceBuilding()
    {
        MeshRenderer meshRenderer = building.GetComponent<MeshRenderer>();
        if(meshRenderer == null)
        {
            meshRenderer = building.GetComponentInChildren<MeshRenderer>();
        }

        float heightAboveGround = (meshRenderer.bounds.size.y / 2.75f);
        if(buildingObject.buildID == 1)
        {
            heightAboveGround = (meshRenderer.bounds.size.y / 1.25f);
        }
        Debug.Log("moving");
        if (_inputManager.SecondaryMovement.x > 0.2f)
        {
            building.transform.Rotate(new Vector3(0, rotationSpeed * Time.deltaTime, 0));
            Debug.Log("rotating 1");
        }
        if (_inputManager.SecondaryMovement.x < -0.2f)
        {
            building.transform.Rotate(new Vector3(0, -rotationSpeed * Time.deltaTime, 0));
            Debug.Log("rotating -1");
        }

        Vector3 position = new Vector3(gameObject.transform.position.x + (gameObject.transform.forward.x * 2), 
            ground.transform.position.y + heightAboveGround, gameObject.transform.position.z + (gameObject.transform.forward.z * 2));

        building.transform.position = position;

        if (_inputManager.JumpButton.State.CurrentState == MMInput.ButtonStates.ButtonUp)
        {
            building.AddComponent<MonoMovableObject>();
            building.GetComponent<MonoMovableObject>().NewMovableObject(buildingObject);
            GameObject.Find("GameMaster").GetComponent<SetUpMovableObjects>().AddMovableObject(building);
            isPlacing = false;
            building = null;
        }

        if (_inputManager.TimeControlButton.State.CurrentState == MMInput.ButtonStates.ButtonUp)
        {
            Destroy(building);
            isPlacing = false;
            building = null;
        }
    }
}

using Cherry.Anomalies;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnomalyRoomController : MonoBehaviour
{

    public AnomalyType[] anomalyTypesInRoom;

    [Header("Object")]
    [SerializeField] private List<ObjectAnomalyVariant> variants = new();
    public int amountOfVariations = 1;
    public bool randomObjects = true;


    [Header("VFX")]
    public bool callByName = false;
    public string[] vfxNames;
    public int vfxAmount = 1;

    [Header("Sounds")]
    public string[] soundNames;
    [Header("Decal")]
    public string[] decalNames;
    [Header("Creature")]
    public string[] creatureNames;

    private void Start()
    {

        foreach (AnomalyType anomaly in anomalyTypesInRoom)
        {
            if (anomaly == AnomalyType.Object)
            {
                AnomalyObjectSetUp();
            }
            if (anomaly == AnomalyType.Lights)
            {
                AnomalyLightsSetup();
            }
            if (anomaly == AnomalyType.VFX)
            {
                AnomalyVFXsetUp();
            }
            if (anomaly == AnomalyType.Sound)
            {
                AnomalySoundSetUp();
            }
        }
    }

    private void AnomalySoundSetUp()
    {

        AnomalyBase[] soundAnomalies = AnomalyManager.Instance.All.Where(a => a.Type == AnomalyType.Sound).ToArray();
        for (int i = 0; i < soundAnomalies.Length; i++)
        {
            for (int j = 0; j < soundNames.Length; j++)
            {
                if (soundAnomalies[i].DisplayName.ToLower() == soundNames[j].ToLower())
                {
                    soundAnomalies[i].Activate();
                }
            }
        }
    }

    private void AnomalyVFXsetUp()
    {
        if (callByName)
        {
            AnomalyBase[] vfxAnomalies = AnomalyManager.Instance.All.Where(a => a.Type == AnomalyType.VFX).ToArray();

            for (int i = 0; i < vfxAnomalies.Length; i++)
            {
                for (int j = 0; j < vfxNames.Length; j++)
                {
                    if(vfxAnomalies[i].DisplayName.ToLower() == vfxNames[j].ToLower())
                    {
                        vfxAnomalies[i].Activate();
                    }
                }
            }
        }
        else
        {
            AnomalyBase[] vfxAnomalies = AnomalyManager.Instance.All.Where(a => a.Type == AnomalyType.VFX).ToArray();

            for (int i = 0; i < Mathf.Min(vfxAmount, vfxAnomalies.Length); i++)
            {
                vfxAnomalies[i].Activate();
            }
        }
    }

    private void AnomalyLightsSetup()
    {
        AnomalyBase[] objectAnomalies = AnomalyManager.Instance.All.Where(a => a.Type == AnomalyType.Lights).ToArray();

        foreach (AnomalyBase lightAnomaly in objectAnomalies)
        {
            lightAnomaly.Activate();
        }

        // Implementation for setting up light anomalies
    }

    private void AnomalyObjectSetUp()
    {
        AnomalyBase[] objectAnomalies = AnomalyManager.Instance.All.Where(a => a.Type == AnomalyType.Object).ToArray();

        int anomalyVariantTypes = 0;
        for (int i = 0; i < Mathf.Min(amountOfVariations, objectAnomalies.Length); i++)
        {
            if (randomObjects)
            {
                objectAnomalies[Random.Range(0, objectAnomalies.Length-1)].GetComponent<ObjectAnomaly>().ApplyVariant(variants[anomalyVariantTypes]);
            }
            else
            {
                objectAnomalies[i].GetComponent<ObjectAnomaly>().ApplyVariant(variants[anomalyVariantTypes]);
            }
            //objectAnomalies[i].Activate();
            anomalyVariantTypes++;
            if(anomalyVariantTypes >= variants.Count - 1)
            {
                anomalyVariantTypes = 0;
            }
        }
       // Implementation for setting up object anomalies based on variants and amountOfVariations
    }

}

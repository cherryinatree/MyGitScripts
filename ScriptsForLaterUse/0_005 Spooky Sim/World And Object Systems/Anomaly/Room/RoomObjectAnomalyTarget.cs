using UnityEngine;
using System.Collections.Generic;

namespace Cherry.Anomalies
{
    public enum RoomObjectVariantType
    {
        Missing,
        WrongSize,
        WrongPlace,
        WrongAngle,
        ExtraObject
    }

    [System.Serializable]
    public class RoomObjectVariant
    {
        public RoomObjectVariantType type;
        public Vector3 positionOffset;
        public Vector3 rotationOffsetEuler;
        public Vector3 scaleMultiplier = Vector3.one;
        public GameObject extraPrefab; // for "not usually in room"
    }

    public class RoomObjectAnomalyTarget : MonoBehaviour
    {
        [SerializeField] private List<RoomObjectVariant> variants = new();

        private Vector3 normalPos;
        private Quaternion normalRot;
        private Vector3 normalScale;
        private bool normalActive;

        private GameObject spawnedExtra;

        private void Awake()
        {
            CacheNormal();
        }

        private void CacheNormal()
        {
            normalPos = transform.localPosition;
            normalRot = transform.localRotation;
            normalScale = transform.localScale;
            normalActive = gameObject.activeSelf;
        }

        public void ApplyRandomVariant(System.Random rng)
        {
            if (variants == null || variants.Count == 0) return;

            var v = variants[rng.Next(variants.Count)];
            ApplyVariant(v);
        }

        public void ApplyVariant(RoomObjectVariant v)
        {
            RestoreNormal();

            switch (v.type)
            {
                case RoomObjectVariantType.Missing:
                    gameObject.SetActive(false);
                    break;

                case RoomObjectVariantType.WrongSize:
                    transform.localScale = Vector3.Scale(normalScale, v.scaleMultiplier);
                    break;

                case RoomObjectVariantType.WrongPlace:
                    transform.localPosition = normalPos + v.positionOffset;
                    break;

                case RoomObjectVariantType.WrongAngle:
                    transform.localRotation = normalRot * Quaternion.Euler(v.rotationOffsetEuler);
                    break;

                case RoomObjectVariantType.ExtraObject:
                    if (v.extraPrefab)
                    {
                        spawnedExtra = Instantiate(v.extraPrefab, transform.parent);
                        spawnedExtra.transform.localPosition = normalPos + v.positionOffset;
                        spawnedExtra.transform.localRotation = normalRot * Quaternion.Euler(v.rotationOffsetEuler);
                        spawnedExtra.transform.localScale = Vector3.Scale(normalScale, v.scaleMultiplier);
                    }
                    break;
            }
        }

        public void RestoreNormal()
        {
            if (spawnedExtra) Destroy(spawnedExtra);

            transform.localPosition = normalPos;
            transform.localRotation = normalRot;
            transform.localScale = normalScale;
            gameObject.SetActive(normalActive);
        }
    }
}

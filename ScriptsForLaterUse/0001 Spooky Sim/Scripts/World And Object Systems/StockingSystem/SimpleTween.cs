using System.Collections;
using UnityEngine;

public static class SimpleTween
{
    /// <summary>
    /// Ease in-out cubic between two poses over 'duration' seconds.
    /// Caller should StartCoroutine on this.
    /// </summary>
    public static IEnumerator MoveRotate(Transform t, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        if (!t || duration <= 0f)
        {
            if (t)
            {
                t.position = targetPos;
                t.rotation = targetRot;
            }
            yield break;
        }

        Vector3 startPos = t.position;
        Quaternion startRot = t.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            float s = EaseInOutCubic(u);

            t.position = Vector3.LerpUnclamped(startPos, targetPos, s);
            t.rotation = Quaternion.SlerpUnclamped(startRot, targetRot, s);
            yield return null;
        }

        t.position = targetPos;
        t.rotation = targetRot;
    }

    private static float EaseInOutCubic(float x)
    {
        return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
    }
}

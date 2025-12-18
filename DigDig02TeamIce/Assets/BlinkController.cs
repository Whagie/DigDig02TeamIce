using UnityEngine;
using System.Collections;

public class BlinkController : MonoBehaviour
{
    private Player player;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private int blinkLayerIndex = 1;
    [SerializeField] private string blinkCloseTrigger = "BlinkClose";
    [SerializeField] private string blinkOpenTrigger = "BlinkOpen";

    [Header("Blink Settings")]
    [Tooltip("Average blinks per minute at multiplier = 1")]
    public float blinksPerMinute = 17f;

    [Tooltip("How often we roll the blink chance (seconds)")]
    public float tickInterval = 0.5f;

    [Tooltip("Global blink speed multiplier")]
    public float blinkRateMultiplier = 1f;

    [Header("Blend Shape Driver")]
    [SerializeField] private SkinnedMeshRenderer skinnedMesh;
    [SerializeField] private int blinkBlendShapeIndex;

    [Header("Blink Duration (seconds)")]
    public Vector2 blinkDurationRange = new Vector2(0.3f, 0.4f);

    void Start()
    {
        player = GetComponent<Player>();
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float blinkChance = (blinksPerMinute / 60f) * tickInterval * blinkRateMultiplier;

            if (Random.value < blinkChance)
            {
                float normalizedBlend = 1f - (skinnedMesh.GetBlendShapeWeight(blinkBlendShapeIndex) / 100f);

                animator.SetLayerWeight(blinkLayerIndex, normalizedBlend);

                animator.SetTrigger(blinkCloseTrigger);

                float blinkDuration = Random.Range(
                    blinkDurationRange.x,
                    blinkDurationRange.y
                );

                yield return new WaitForSeconds(blinkDuration);

                animator.SetTrigger(blinkOpenTrigger);
            }

            yield return new WaitForSeconds(tickInterval);
        }
    }
}

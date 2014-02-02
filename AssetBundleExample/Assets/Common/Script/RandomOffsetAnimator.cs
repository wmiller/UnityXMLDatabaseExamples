using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class RandomOffsetAnimator : MonoBehaviour
{
    public string StateName;
    public int Layer;
    public float MinNormalizedTime;
    public float MaxNormalizedTime;

    private void Start()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play(StateName, Layer, Random.Range(MinNormalizedTime, MaxNormalizedTime));
        }
    }
}

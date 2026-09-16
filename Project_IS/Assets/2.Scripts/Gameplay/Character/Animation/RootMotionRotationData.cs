using UnityEngine;

[CreateAssetMenu(menuName = "Animation/Root Motion Rotation Data")]
public class RootMotionRotationData : ScriptableObject
{
    public AnimationClip sourceClip;

    // 원본 애니메이션의 총 회전량
    public float totalYaw;

    // 원본 회전각
    // x = normalizedTime
    // y = 누적 회전각
    public AnimationCurve rotationDegrees;

    // 0 ~ 1로 정규화된 회전 진행도
    public AnimationCurve normalizedRotation;
}

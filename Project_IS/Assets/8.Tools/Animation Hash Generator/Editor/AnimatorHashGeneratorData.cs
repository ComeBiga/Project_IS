using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

[CreateAssetMenu(menuName = "Animation/Animator Hash Generator Data", fileName = "Animator Hash Generator Data")]
public class AnimatorHashGeneratorData : ScriptableObject
{
    public AnimatorController animatorController;
}

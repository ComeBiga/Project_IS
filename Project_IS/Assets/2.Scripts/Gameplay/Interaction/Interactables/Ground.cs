using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    public enum EGroundType { Concrete, Wood, Metal, Other }

    public EGroundType Type => _groundType;

    [SerializeField]
    private EGroundType _groundType;
    [SerializeField]
    private string _otherGroundTypeName;

    public void PlayFootStepSound()
    {
        //if (mGround == null)
        //{
        //    AudioManager.instance.PlayOneShot("FootStepConcrete");
        //    return;
        //}

        switch (_groundType)
        {
            case Ground.EGroundType.Concrete:
                AudioManager.instance.PlayOneShot("FootStepConcrete");
                break;
            case Ground.EGroundType.Wood:
                AudioManager.instance.PlayOneShot("FootStepWood");
                break;
            case Ground.EGroundType.Other:
                AudioManager.instance.PlayOneShot($"FootStep{_otherGroundTypeName}");
                break;
            default:
                AudioManager.instance.PlayOneShot("FootStepConcrete");
                break;
        }
    }

    public void PlayFootStepSound(float volume)
    {
        //if (mGround == null)
        //{
        //    AudioManager.instance.PlayOneShot("FootStepConcrete");
        //    return;
        //}

        switch (_groundType)
        {
            case Ground.EGroundType.Concrete:
                AudioManager.instance.PlayOneShot("FootStepConcrete", volume);
                break;
            case Ground.EGroundType.Wood:
                AudioManager.instance.PlayOneShot("FootStepWood", volume);
                break;
            case Ground.EGroundType.Other:
                AudioManager.instance.PlayOneShot($"FootStep{_otherGroundTypeName}", volume);
                break;
            default:
                AudioManager.instance.PlayOneShot("FootStepConcrete", volume);
                break;
        }
    }

    public void PlayFootStepBigSound()
    {
        switch (_groundType)
        {
            case Ground.EGroundType.Concrete:
                AudioManager.instance.PlayOneShot("FootStepConcreteBig");
                break;
            case Ground.EGroundType.Wood:
                AudioManager.instance.PlayOneShot("FootStepWoodBig");
                break;
            case Ground.EGroundType.Other:
                AudioManager.instance.PlayOneShot($"FootStep{_otherGroundTypeName}");
                break;
            default:
                AudioManager.instance.PlayOneShot("FootStepConcrete");
                break;
        }
    }

    public void PlayFootStepBigSound(float volume)
    {
        switch (_groundType)
        {
            case Ground.EGroundType.Concrete:
                AudioManager.instance.PlayOneShot("FootStepConcreteBig", volume);
                break;
            case Ground.EGroundType.Wood:
                AudioManager.instance.PlayOneShot("FootStepWoodBig", volume);
                break;
            case Ground.EGroundType.Other:
                AudioManager.instance.PlayOneShot($"FootStep{_otherGroundTypeName}", volume);
                break;
            default:
                AudioManager.instance.PlayOneShot("FootStepConcrete", volume);
                break;
        }
    }

    public void PlayHandTouchSound()
    {
        switch (_groundType)
        {
            case Ground.EGroundType.Concrete:
                AudioManager.instance.PlayOneShot("HandTouchConcrete");
                break;
            case Ground.EGroundType.Wood:
                AudioManager.instance.PlayOneShot("HandTouchWood");
                break;
            case Ground.EGroundType.Other:
                AudioManager.instance.PlayOneShot($"HandTouch{_otherGroundTypeName}");
                break;
            default:
                AudioManager.instance.PlayOneShot("HandTouchConcrete");
                break;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PrototypeGround : MonoBehaviour
{
    private MeshRenderer mMeshRenderer;

    private const float DEFALUT_PLANE_SIZE = 10f;

    private void Awake()
    {
        mMeshRenderer = GetComponent<MeshRenderer>();
        setTextureScale();
    }

    private void setTextureScale()
    {
        float halfSize = DEFALUT_PLANE_SIZE * .5f;
        float textureScaleX = DEFALUT_PLANE_SIZE * transform.localScale.x;
        float textureScaleY = DEFALUT_PLANE_SIZE * transform.localScale.z;

        Vector2 newScale = mMeshRenderer.material.mainTextureScale;
        newScale.x = textureScaleX;
        newScale.y = textureScaleY;
        mMeshRenderer.material.mainTextureScale = newScale;

        Debug.Log(mMeshRenderer.material.mainTextureScale);
    }
}

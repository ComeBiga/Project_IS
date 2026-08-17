using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameGizmosDrawer : MonoBehaviour
{
    public interface IDrawGizmos
    {
        public void SetGizmos(GameGizmosDrawer gizmosDrawer);
        public void DrawGizmos(GameGizmosDrawer gizmosDrawer);
    }

    [Serializable]
    public struct DrawInfo
    {
        public Type type;
        public bool drawOnSelected;
    }

    [SerializeField]
    private Transform _trPlayerCharacter;

    // Start is called before the first frame update
    void Start()
    {
        
    }
}

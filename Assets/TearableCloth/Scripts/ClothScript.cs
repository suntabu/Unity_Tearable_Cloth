using System;
using UnityEngine;
using Cloth = TearableCloth.Cloth;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ClothScript : MonoBehaviour
{
    public int accuracy = 5;
    public int gravity = -10;
    public int clothY = 28;
    public int clothX = 54;
    public float spacing = 8;
    public int tearDist = 60;
    public float friction = 0.99f;
    public float bounce = 0.5f;

    public float force = 0.01f;
    public float particleSize = 0.1f;

    [HideInInspector] public Mesh mesh;
    public MouseInfo mouse;

    private Cloth cloth;
    public float width = 800;
    public float height = 1260;

    private void Awake()
    {
        mouse = new MouseInfo
        {
            influence = 26,
            cut = 8
        };

        mesh = GetComponent<MeshFilter>().mesh;
        cloth = new Cloth(this);
    }

    private void Update()
    {
        mouse.Update();

        cloth.Update(force, particleSize);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (cloth != null)
            cloth.DrawGizmos(particleSize);
    }
#endif

    [Serializable]
    public class MouseInfo
    {
        public float influence;
        public float cut;
        public bool isEnable;

        [HideInInspector] public float x;
        [HideInInspector] public float y;
        [HideInInspector] public float px;
        [HideInInspector] public float py;
        [HideInInspector] public bool down;


        public void Update()
        {
            if (!isEnable)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                down = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                down = false;
            }

            var p = Input.mousePosition;
            px = x;
            py = y;

            x = p.x;
            y = p.y;
        }
    }
}
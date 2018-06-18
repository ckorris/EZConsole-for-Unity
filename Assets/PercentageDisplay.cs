using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PercentageDisplay : MonoBehaviour //TODO: Refactor because percentage implies a max of 100. 
{
    public float Height = 0.5f; //How tall the bar is, which is the axis the value is shown.
    public float Width = 0.1f;
    public float Length = 0.1f;

    public int MaxValue = 25; 

    [SerializeField]
    private int _currentValue = 15;
    public int CurrentValue
    {
        get
        {
            return _currentValue;
        }
        set
        {
            _currentValue = value;
            _material.SetVector("_CullPlanePos", new Vector4(0, SegmentHeight * value, 0, 1));
        }
    }

    public float Padding = 0.01f; //Space between each cube. Must be >0 or clipping shader will cull a face that should be visible. 

    private Material _material; //The material to be attached to the procedural mesh

    private float SegmentHeight //Distance between each "notch". 
    {
        get
        {
            return Height / (MaxValue);
        }
    }

    // Use this for initialization
    void Start()
    {
        //Set the material to a copy from the Resources folder
        //TODO: Check if we've already applied that material
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material currentmat = renderer.material;

        if(currentmat.name == "CullAbovePlaneMat")
        {
            _material = new Material(currentmat); //Instancing so we can safely adjust properties.
        }
        else
        {
            _material = new Material(Resources.Load<Material>("CullAbovePlaneMat")); //Instancing so we can safely adjust properties.

        }

        renderer.material = _material;
        _material.SetVector("_CullPlaneNormal", new Vector4(0, -1, 0, 1));
        _material.SetVector("_CullPlanePos", new Vector4(0, SegmentHeight * _currentValue, 0, 1));

        GetComponent<MeshFilter>().mesh = BuildNewCubeMesh();
    }

    // Update is called once per frame
    void Update()
    {
        //Test
        CurrentValue = _currentValue;
    }

    private Mesh BuildNewCubeMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] verts = new Vector3[24 * MaxValue];
        int[] tris = new int[36 * MaxValue];

        for(int i = 0; i < MaxValue; i++)
        {
            Vector3[] cubeverts = GetCubeVertices(0 - (Width / 2), Width / 2, SegmentHeight * i + Padding / 2, SegmentHeight * (i + 1) - Padding / 2, 0 - (Length / 2), Length / 2);
            Array.Copy(cubeverts, 0, verts, i * 24, 24);

            int[] cubetris = GetCubeTriangles(i * 24);
            Array.Copy(cubetris, 0, tris, i * 36, 36);

        }

        //mesh.vertices = GetCubeVertices(-0.1f, 0.1f, 00f, 0.1f, -0.1f, 0.1f);
        //mesh.triangles = GetCubeTriangles();

        mesh.vertices = verts;
        mesh.triangles = tris;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Vector3[] GetCubeVertices(float left, float right, float bottom, float top, float back, float front)
    {
        Vector3 leftbottomback = new Vector3(left, bottom, back);
        Vector3 rightbottomback = new Vector3(right, bottom, back);
        Vector3 lefttopback = new Vector3(left, top, back);
        Vector3 righttopback = new Vector3(right, top, back);
        Vector3 leftbottomfront = new Vector3(left, bottom, front);
        Vector3 rightbottomfront = new Vector3(right, bottom, front);
        Vector3 lefttopfront = new Vector3(left, top, front);
        Vector3 righttopfront = new Vector3(right, top, front);

        //If anything about this order seems weird, read this: https://docs.unity3d.com/Manual/Example-CreatingaBillboardPlane.html
        //The order for each face is bottomleft, bottomright, topleft, topright when looking straight at the face from the most intuitive direction. 
        //Note: We double up vertices on corners instead of reusing them so that they can have separate normal values. 
        Vector3[] verts = new Vector3[24]
        {
            leftbottomfront, leftbottomback, lefttopfront, lefttopback, //Left face
            rightbottomback, rightbottomfront, righttopback, righttopfront, //Right face
            rightbottomback, leftbottomback, rightbottomfront, leftbottomfront, //Bottom face
            righttopfront, lefttopfront, righttopback, lefttopback, //Top face
            rightbottomfront, leftbottomfront, righttopfront, lefttopfront, //Front face
            leftbottomback, rightbottomback, lefttopback, righttopback //Back face
        };

        return verts;
    }

    private int[] GetCubeTriangles(int vertoffset = 0)
    {
        int[] tris = new int[36];

        //This is hard-coded because there's no more optimal way to do it, assuming the layout from GetCubeVertices
        //For each face, arrange tri points counter-clockwise from perspective where the polygon will be visible. 
        //This link (same as from verts) explains why we do this order: https://docs.unity3d.com/Manual/Example-CreatingaBillboardPlane.html
        for (int i = 0; i < 6; i++)
        {
            int vertstep = i * 4;
            int tristep = i * 6;

            tris[tristep] = vertoffset + vertstep;  //Bottom left
            tris[tristep + 1] = vertoffset + vertstep + 2; //Top left
            tris[tristep + 2] = vertoffset + vertstep + 1; //Bottom right
            tris[tristep + 3] = vertoffset + vertstep + 2; //Top left
            tris[tristep + 4] = vertoffset + vertstep + 3; //Top right
            tris[tristep + 5] = vertoffset + vertstep + 1; //Bottom right
        }

        return tris;
    }

    public void OnDrawGizmos()
    {
        //Draw an outline of the mesh's bounds, so it's easy to see before we have the procedural mesh. 
        Vector3 bottombackleft = transform.rotation * new Vector3(0 - Width / 2, 0, 0 - Length / 2) + transform.position;
        Vector3 bottombackright = transform.rotation * new Vector3(Width / 2, 0, 0 - Length / 2) + transform.position;
        Vector3 bottomfrontleft = transform.rotation * new Vector3(0 - Width / 2, 0, Length / 2) + transform.position;
        Vector3 bottomfrontright = transform.rotation * new Vector3(Width / 2, 0, Length / 2) + transform.position;

        Vector3 heightvector = transform.rotation * (Vector3.up * Height * transform.localScale.y);
        
        Gizmos.DrawLine(bottombackleft, bottombackleft + heightvector);
        Gizmos.DrawLine(bottombackright, bottombackright + heightvector);
        Gizmos.DrawLine(bottomfrontleft, bottomfrontleft + heightvector);
        Gizmos.DrawLine(bottomfrontright, bottomfrontright + heightvector);

        //Draw lines for the segments, but only if the app isn't running (because then we'd see the mesh outline)
        if (!Application.isPlaying) 
        {
            //Prevent drawing too many segments, because it'll get ridiculous. 
            float cappedmaxvalue = MaxValue;
            while (cappedmaxvalue > 50)
            {
                cappedmaxvalue /= 2;
            }

            for (int i = 0; i <= cappedmaxvalue; i++)
            {
                Vector3 segmentheightvector = transform.rotation * (Vector3.up * Height / cappedmaxvalue * i * transform.localScale.y);

                Gizmos.DrawLine(bottombackleft + segmentheightvector, bottombackright + segmentheightvector);
                Gizmos.DrawLine(bottombackright + segmentheightvector, bottomfrontright + segmentheightvector);
                Gizmos.DrawLine(bottomfrontright + segmentheightvector, bottomfrontleft + segmentheightvector);
                Gizmos.DrawLine(bottomfrontleft + segmentheightvector, bottombackleft + segmentheightvector);
            }
        }
        else
        {
            //Just draw the top and bottoms. 
            //Bottoms first
            Gizmos.DrawLine(bottombackleft, bottombackright);
            Gizmos.DrawLine(bottombackright, bottomfrontright);
            Gizmos.DrawLine(bottomfrontright, bottomfrontleft);
            Gizmos.DrawLine(bottomfrontleft, bottombackleft);

            Gizmos.DrawLine(bottombackleft + heightvector, bottombackright + heightvector);
            Gizmos.DrawLine(bottombackright + heightvector, bottomfrontright + heightvector);
            Gizmos.DrawLine(bottomfrontright + heightvector, bottomfrontleft + heightvector);
            Gizmos.DrawLine(bottomfrontleft + heightvector, bottombackleft + heightvector);
        }
    }

}

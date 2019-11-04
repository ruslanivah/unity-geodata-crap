using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;


[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GlobeTest : MonoBehaviour
{
  int xSize = 36;
  int ySize = 18;

  public Vector3[,] points;
  public Vector3[,] pointsSphere;
  public Vector3[,] pointsReal;
  [Range(0f, 1f)] public float Lerp;
  [Range(0, 1000)] public float Radius = 100;
  public bool Cartesian;
  public bool DrawCoords;
  public bool DrawIndices;
  public bool DrawGrids;

  MeshFilter mf;


  void Start()
  {
    Configure();
  }

  void OnEnable()
  {
    Configure();
  }


  void Configure()
  {
    mf = GetComponent<MeshFilter>();
    FillCoords();
    CalculatePoints();
  }


  void FillCoords()
  {
    points = new Vector3[37, 19];
    pointsSphere = new Vector3[37, 19];
    pointsReal = new Vector3[37, 19];


    for (int x = 0; x < 361; x += 10)
    {
      for (int y = 0; y < 181; y += 10)
      {
        var pos = new Vector2(x - 180, y - 90);
        points[x / 10, y / 10] = pos;
        pointsSphere[x / 10, y / 10] = SphericalCoordinates.SphericalToCartesian(Radius, pos.x,
          pos.y);
      }
    }
  }


  public Mesh MakeMesh()
  {
    var m = new Mesh();
    var verts = new List<Vector3>();
    var triangles = new List<int>();
    for (int y = 0; y < pointsReal.GetLength(1) - 1; y++)
    {
      for (int x = 0; x < pointsReal.GetLength(0) - 1; x++)
      {
        verts.Add(pointsReal[x, y]);
        verts.Add(pointsReal[x, y + 1]);
        verts.Add(pointsReal[x + 1, y + 1]);
        verts.Add(pointsReal[x + 1, y]);

        triangles.Add(x * 4);
        triangles.Add(x * 4 + 1);
        triangles.Add(x * 4 + 2);

        triangles.Add(x * 4 + 0);
        triangles.Add(x * 4 + 2);
        triangles.Add(x * 4 + 3);
      }
    }


    m.SetVertices(verts);
    m.SetTriangles(triangles.ToArray(), 0);

    m.RecalculateTangents();
    m.RecalculateNormals();


    return m;
  }

  void CalculatePoints()
  {
    for (int i = 0; i < points.GetLength(0); i++)
    {
      for (int j = 0; j < points.GetLength(1); j++)
      {
        pointsReal[i, j] = Vector3.Lerp(points[i, j], pointsSphere[i, j], Lerp);
      }
    }
  }


  void Update()
  {
    CalculatePoints();
    var m = SphereMeshGenerator.GenerateTerrainMesh(pointsReal);
    mf.sharedMesh = m.CreateMesh();
    //for (int i = 0; i < 18; i++)
    {
      //filters[i].mesh = MakeMesh(i);
      //combine[i].mesh = MakeMesh(i);
    }

    //var m = new Mesh();
    //m.CombineMeshes(combine, true);
    //mf.sharedMesh= m;
  }

  void OnDrawGizmos()
  {
    Gizmos.color = Color.white;

    for (int i = 0; i < pointsReal.GetLength(0); i++)
    {
      for (int j = 0; j < pointsReal.GetLength(1); j++)
      {
        if (DrawGrids)
        {
          Gizmos.DrawWireSphere(pointsReal[i, j], 1f);
          if (i > 0)
          {
            Gizmos.DrawLine(pointsReal[i, j], pointsReal[i - 1, j]);
          }

          if (j > 0)
          {
            Gizmos.DrawLine(pointsReal[i, j], pointsReal[i, j - 1]);
          }
        }
      }
    }


    Gizmos.color = Color.white;
    if (Cartesian)
    {
      Gizmos.DrawLine(new Vector3(-180, 90, 0), new Vector3(180, 90, 0));
      Gizmos.DrawLine(new Vector3(-180, -90, 0), new Vector3(180, -90, 0));

      Gizmos.DrawLine(new Vector3(-180, 90, 0), new Vector3(-180, -90, 0));
      Gizmos.DrawLine(new Vector3(180, 90, 0), new Vector3(180, -90, 0));
    }
  }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.Networking;
using static WebTileHelper;

[System.Serializable]
public class Tilemap
{
  public int X;
  public int Y;
  public int Z;
}


[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class TilemapRenderer : MonoBehaviour
{
  public Tilemap Tilemap;
  public int IDx = 267580;
  public int IDy = 175363;
  public int Zoom = 19;
  public string Name;
  public Vector3 Offset;
  Vector3 LeftBottom { get; set; }
  Vector3 LeftTop { get; set; }
  Vector3 RightTop { get; set; }
  Vector3 RightBottom { get; set; }

  BoundingBox BoundingBox;

  public string ImageURL;

  void Awake()
  {
    _meshRenderer = GetComponent<MeshRenderer>();
    _meshFilter = GetComponent<MeshFilter>();
    Configure();
    TextureTile();
  }

  void Configure()
  {
    BoundingBox = Tile2BoundingBox(IDx, IDy, Zoom);
    CalculateVertices(BoundingBox);
    Name = "/Tile/" + IDx + "/" + IDy + "/" + Zoom;
    ImageURL = TileServers.RetrieveWebTile(IDx, IDy, Zoom, WebMapProvider.OpenStreetMapBase);
  }

  public void InitializeTile(int x, int y, int zoom, Vector3 offset)
  {
    IDx = x;
    IDy = y;
    Zoom = zoom;
    Offset = offset;
    Configure();
  }

  void TextureTile()
  {
    ConstructMesh();
    StartCoroutine(DownloadWebmapTile());
  }


  public BoundingBox Tile2BoundingBox(int x, int y, int zoom)
  {
    var bb = new BoundingBox();
    bb.North = Tile2Lat(y, zoom);
    bb.South = Tile2Lat(y + 1, zoom);
    bb.West = Tile2Lon(x, zoom);
    bb.East = Tile2Lon(x + 1, zoom);
    return bb;
  }

  public void CalculateVertices(BoundingBox box)
  {
    LeftBottom = new Vector3((float) (MercatorProjection.lonToX(box.West)), 0,
      (float) (MercatorProjection.latToY(box.South)));
    LeftTop = new Vector3((float) (MercatorProjection.lonToX(box.West)), 0,
      (float) (MercatorProjection.latToY(box.North)));
    RightBottom = new Vector3((float) (MercatorProjection.lonToX(box.East)), 0,
      (float) (MercatorProjection.latToY(box.South)));
    RightTop = new Vector3((float) (MercatorProjection.lonToX(box.East)), 0,
      (float) (MercatorProjection.latToY(box.North)));
  }

  public void ConstructMesh()
  {
    var mesh = new Mesh();
    var vertices = new List<Vector3>();
    vertices.Add(Vector3.zero);
    vertices.Add(RightBottom - LeftBottom);
    vertices.Add(LeftTop - LeftBottom);
    vertices.Add(RightTop - LeftBottom);

    var tri = new int[6];
    //  Lower left triangle.
    tri[0] = 0;
    tri[1] = 2;
    tri[2] = 1;
    //  Upper right triangle.
    tri[3] = 2;
    tri[4] = 3;
    tri[5] = 1;
    var uv = new List<Vector2>();
    uv.Add(new Vector2(0, 0));
    uv.Add(new Vector2(1, 0));
    uv.Add(new Vector2(0, 1));
    uv.Add(new Vector2(1, 1));

    mesh.SetVertices(vertices);
    mesh.SetTriangles(tri, 0);
    mesh.SetUVs(0, uv);
    mesh.RecalculateNormals();
    _meshFilter.sharedMesh = mesh;
  }


  public IEnumerator DownloadWebmapTile()
  {
    using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(ImageURL))
    {
      yield return uwr.SendWebRequest();

      if (uwr.isNetworkError || uwr.isHttpError)
      {
        Debug.Log(uwr.error);
      }
      else
      {
        // Get downloaded asset bundle
        print("done");

        _meshRenderer.material.SetTexture("_BaseColorMap", DownloadHandlerTexture.GetContent(uwr));
      }
    }
  }


  MeshFilter _meshFilter;
  MeshRenderer _meshRenderer;
}
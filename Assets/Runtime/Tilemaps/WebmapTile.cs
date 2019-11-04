using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using static WebTileHelper;


[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class WebmapTile : MonoBehaviour
{
  public int IDx = 267580;
  public int IDy = 175363;
  public int Zoom = 19;
  public string Name;
  public Vector3 Offset;
  Vector3 LeftBottom { get; set; }
  Vector3 LeftTop { get; set; }
  Vector3 RightTop { get; set; }
  Vector3 RightBottom { get; set; }

  public BoundingBox BoundingBox;
  public double North;
  public double South;
  public double East;
  public double West;
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
    Tile2BoundingBox(IDx, IDy, Zoom);
    CalculateVertices();
    Name = "/Tile/" + IDx + "/" + IDy + "/" + Zoom;
    ConstructImageURL();
  }

  public void InitializeTile(int input_x, int input_y, int zoom, Vector3 offset)
  {
    IDx = input_x;
    IDy = input_y;
    Zoom = zoom;
    Offset = offset;
    Configure();
  }

  public void TextureTile()
  {
    ConstructMesh();
    StartCoroutine(DownloadWebmapTile());
  }


  public void Tile2BoundingBox(int x, int y, int zoom)
  {
    North = Tile2Lat(y, zoom);
    South = Tile2Lat(y + 1, zoom);
    West = Tile2Lon(x, zoom);
    East = Tile2Lon(x + 1, zoom);
  }

  public void CalculateVertices()
  {
    LeftBottom = new Vector3((float) (MercatorProjection.lonToX(West)), 0, (float) (MercatorProjection.latToY(South)));
    LeftTop = new Vector3((float) (MercatorProjection.lonToX(West)), 0, (float) (MercatorProjection.latToY(North)));
    RightBottom = new Vector3((float) (MercatorProjection.lonToX(East)), 0, (float) (MercatorProjection.latToY(South)));
    RightTop = new Vector3((float) (MercatorProjection.lonToX(East)), 0, (float) (MercatorProjection.latToY(North)));
  }

  public void ConstructMesh()
  {
    var mesh = new Mesh();
    var vertices = new Vector3[4];

    vertices[0] = Vector3.zero;
    vertices[1] = RightBottom - LeftBottom;
    vertices[2] = LeftTop - LeftBottom;
    vertices[3] = RightTop - LeftBottom;

    mesh.vertices = vertices;

    var tri = new int[6];

    //  Lower left triangle.
    tri[0] = 0;
    tri[1] = 2;
    tri[2] = 1;

    //  Upper right triangle.
    tri[3] = 2;
    tri[4] = 3;
    tri[5] = 1;

    mesh.triangles = tri;

    var normals = new Vector3[4];
    normals[0] = Vector3.up;
    normals[1] = Vector3.up;
    normals[2] = Vector3.up;
    normals[3] = Vector3.up;
    mesh.normals = normals;

    var uv = new Vector2[4];
    uv[0] = new Vector2(0, 0);
    uv[1] = new Vector2(1, 0);
    uv[2] = new Vector2(0, 1);
    uv[3] = new Vector2(1, 1);
    mesh.uv = uv;

    _meshFilter.sharedMesh = mesh;
  }

  public void ConstructImageURL()
  {
    ImageURL = TileServers.RetrieveWebTile(IDx, IDy, Zoom, WebMapProvider.OpenStreetMapBase);
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
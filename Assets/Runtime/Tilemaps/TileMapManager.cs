using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class TileMapManager : MonoBehaviour
{
  public bool UseGPSLocation = true;
  public GPS _sessionLocation;
  public bool UseVectorData;
  public bool UseTileMaps = true;
  int _zoomLevel = 19;
  float _rotationY;
  public int MapSizeMultiplier;
  public GameObject MapPrefab;

  Vector2 _previousMousePosition;
  GameObject _mapPrefab;
  GameObject _userPrefab;
  string XMLString;
  public string DataURL;
  Vector3 Origin;
  WebmapTile _tilemapdata;
  Vector2 _originID = new Vector2();
  List<WebmapTile> _tileList = new List<WebmapTile>();
  List<WebmapTile> _textureDownloadQueue = new List<WebmapTile>();
  public int TextureDownloadQueueSize = 5;
  int _textureCounter = 0;

  void Start()
  {
    // Include a check for using GPS or Editor/Runtime input
    GPS.OnStart += SpawnSession;
  }

  void OnDestroy()
  {
    GPS.OnStart -= SpawnSession;
  }

  void SpawnSession()
  {
    Origin = new Vector3((float) GPS.Instance.Longitude, 0, (float) GPS.Instance.Latitude);
    _originID.x = WebTileHelper.Longitude2TileX(GPS.Instance.Longitude, _zoomLevel);
    _originID.y = WebTileHelper.Latitude2TileY(GPS.Instance.Latitude, _zoomLevel);


    if (MapSizeMultiplier == 0)
      SpawnTile((int) _originID.x, (int) _originID.y, _zoomLevel, UseVectorData, UseTileMaps);
    else
    {
      for (int x = -MapSizeMultiplier; x < MapSizeMultiplier + 1; x++)
      {
        for (int y = -MapSizeMultiplier; y < MapSizeMultiplier + 1; y++)
        {
          var idx = _originID.x - x;
          var idy = _originID.y - y;

          SpawnTile((int) idx, (int) idy, _zoomLevel, UseVectorData, UseTileMaps);
        }
      }
    }

    // This code calculates our map size. It should be able to run before/without spawning the map.
    var left_bottom = new Vector2(_originID[0] - MapSizeMultiplier, _originID[1] + MapSizeMultiplier);
    var right_top = new Vector2(_originID[0] + MapSizeMultiplier, _originID[1] - MapSizeMultiplier);

    var LowerLeft = _tileList.Find(tile => tile.IDx == left_bottom.x && tile.IDy == left_bottom.y);
    var UpperRight = _tileList.Find(tile => tile.IDx == right_top.x && tile.IDy == right_top.y);

    /*
    Debug.Log("lower left is " + LowerLeft.Name);
    Debug.Log("upper right is" + UpperRight.Name);

    Debug.Log("north is" + UpperRight.North);
    Debug.Log("south is" + LowerLeft.South);
    Debug.Log("east is " + UpperRight.East);
    Debug.Log("west is" + LowerLeft.West);
    */

    DataURL = TileServers.ConstructDataURL(LowerLeft.South, LowerLeft.West, UpperRight.North, UpperRight.East);

    if (UseVectorData == true)
    {
      DataDownload();
    }
  }


  void SpawnTile(int x, int y, int zoom, bool useVector, bool useTileMap)
  {
    var tile = Instantiate(MapPrefab, transform) as GameObject;
    var data = tile.GetComponent<WebmapTile>();
    _tileList.Add(data);
    data.InitializeTile(x, y, _zoomLevel, Origin);
    tile.name = data.Name;
  }


  void CheckTilesComplete()
  {
    Debug.Log("Tile Complete");
  }


  public void DataDownload()
  {
    StartCoroutine("DownloadData");
  }

  public IEnumerator DownloadData()
  {
    using (UnityWebRequest www = UnityWebRequest.Get(DataURL))
    {
      Debug.Log("attempting download");
      yield return www.SendWebRequest();
      if (www.isNetworkError || www.isHttpError)
      {
        Debug.LogError(www.error);
        Debug.Log(www.downloadHandler.text);
      }
      else
      {
        XMLString = www.downloadHandler.text;

        //ParseXML(XMLString);
        Debug.Log("data downloaded");
      }
    }
  }
}
using UnityEngine;
using System;


public static class WebTileHelper
{
  public class BoundingBox
  {
    public double North;
    public double West;
    public double South;
    public double East;
  }

  public static int Longitude2TileX(double lon, int z)
  {
    return (int) (Math.Floor((lon + 180.0) / 360.0 * Math.Pow(2.0, z)));
  }

  public static int Latitude2TileY(double lat, int z)
  {
    return (int) (Math.Floor(
      (1.0 - Math.Log(Math.Tan(lat * Mathf.PI / 180.0) + 1.0 / Math.Cos(lat * Mathf.PI / 180.0)) / Mathf.PI) / 2.0 *
      Math.Pow(2.0, z)));
  }

  public static double Tile2Lon(int x, int z)
  {
    return x / Math.Pow(2.0, z) * 360.0 - 180;
  }

  public static double Tile2Lat(int y, int z)
  {
    double n = Math.PI - (2.0 * Math.PI * y) / Math.Pow(2.0, z);
    return (Math.Atan(Math.Sinh(n))) * Mathf.Rad2Deg;
  }

  public static BoundingBox Tile2BoundingBox(int x, int y, int zoom)
  {
    var bb = new BoundingBox();
    bb.North = Tile2Lat(y, zoom);
    bb.South = Tile2Lat(y + 1, zoom);
    bb.West = Tile2Lon(x, zoom);
    bb.East = Tile2Lon(x + 1, zoom);
    return bb;
  }
}
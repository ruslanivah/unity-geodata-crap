using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WebMapProvider
{
  OpenStreetMapBase,
  OpenStreetMapNoLabels,
  Wikimaps,
  GeografischRuimtelijkBestand,
  AGIV,
}

public static class TileServers
{
  const string basemap = "https://b.tile.openstreetmap.org/";
  const string nolabels = "https://tiles.wmflabs.org/osm-no-labels/";
  const string wikimaps = "https://maps.wikimedia.org/osm-intl/";
  public const string overpass = "https://overpass-api.de/api/interpreter?data=";
  const string overpass_map = "https://overpass-api.de/api/map";
  public const string overpass_3dparty = "https://lz4.overpass-api.de/api/map?bbox=";

  public static string RetrieveWebTile(int x, int y, int z, WebMapProvider provider)
  {
    switch (provider)
    {
      case WebMapProvider.OpenStreetMapBase:
        return ConstructOSMTile(x, y, z);
      case WebMapProvider.OpenStreetMapNoLabels:
        return $"{nolabels}/{z}/{x}/{y}.png";
      case WebMapProvider.Wikimaps:
        return $"{wikimaps}/{z}/{x}/{y}.png";
      case WebMapProvider.GeografischRuimtelijkBestand:
        return ConstructGRB(x, y, z);
      case WebMapProvider.AGIV:
        return ConstructAerial(x, y, z);
      default:
        throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
    }
  }


  static string ConstructGRB(int x, int y, int z)
  {
    var req =
      "https://tile.informatievlaanderen.be/ws/raadpleegdiensten/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=grb_bsk&STYLE=&FORMAT=image/png&tileMatrixSet=GoogleMapsVL&tileMatrix=" +
      z + "&tileRow=" + y + "&tileCol=" + x;
    return req;
  }

  static string ConstructAerial(int x, int y, int z)
  {
    var req =
      "https://tile.informatievlaanderen.be/ws/raadpleegdiensten/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=omwrgbmrvl&STYLE=&FORMAT=image/png&tileMatrixSet=GoogleMapsVL&tileMatrix=" +
      z + "&tileRow=" + y + "&tileCol=" + x;
    return req;
  }

  public static string ConstructOSMTile(int x, int y, int z)
  {
    var req = basemap + z + "/" + x + "/" + y + ".png";
    return req;
  }

  public static string ConstructDataURL(double south, double west, double north, double east)
  {
    // Make sure doubles are correctly formatted.
    var bbox = string.Join(",",
      west.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
      south.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
      east.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
      north.ToString("G", System.Globalization.CultureInfo.InvariantCulture)
    );

    return $"{overpass_map}?bbox={bbox}";
  }
}
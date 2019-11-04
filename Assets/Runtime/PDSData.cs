using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class MoonConstants
{
  /// <summary>
  /// The bottom of a small unnamed crater within Antoniadi crater was measured by the laser altimeter (LALT) instrument
  /// on board the Japanese Selenological and Engineering Explorer (SELENE) satellite to be the lowest point on the moon.
  /// longitude 172.413 °W
  /// latitude = 70.368 °S
  /// altitude = - 9,178 meter
  /// </summary>
  public const float LowestPointOnTheMoon = -9.178f;

  /// <summary>
  /// The "Selenean summit" refers to the "highest" point on the Moon, notionally similar to Mount Everest on the Earth.
  /// longitude = 158.6335 °W
  /// latitude =5.4125 °N
  /// altitude = 10,786 meter
  /// </summary>
  public const float HighestPointOnTheMoon = 10.786f;
}

[System.Serializable]
public class PDSData
{
  public string DatasetID;

  /// <summary>
  /// Number of pixels per degree of longitude or latitude.
  /// </summary>
  public int MapResolution;

  /// <summary>
  /// X dimension
  /// </summary>
  public int ColumnCount;

  /// <summary>
  /// Y dimension
  /// </summary>
  public int RowCount;

  public float MinLat;
  public float MaxLat;
  public float MinLon;
  public float MaxLon;
  public float DataScale;

  /// <summary>
  /// Radius of the moon in kilometer.
  /// </summary>
  public float MoonRadius;

  /// <summary>
  /// IEEE754 float bit depth
  /// </summary>
  public int SampleBits;

  public string SampleType;

  // Number of bytes to represent a number in the .IMG file.
  public int DataSize;


  public float MinHeight, MaxHeight, TrueMin, TrueMax;

  string ReturnValueForToken(string token, List<string> lines)
  {
    string result = string.Empty;
    foreach (var line in lines)
    {
      if (line.Contains(token))
      {
        if (line.Contains("="))
        {
          var split = line.Split('=');
          if (split[1].Contains('<'))
          {
            var final = split[1].Split('<');
            return final[0];
          }

          return split[1];
        }
      }
    }

    return result;
  }

  public PDSData(TextAsset labels)
  {
    var result = Regex.Split(labels.text, "\r\n|\r|\n").ToList();


    DatasetID = ReturnValueForToken("DATA_SET_ID", result);
    MapResolution = int.Parse(ReturnValueForToken("MAP_RESOLUTION     ", result));
    RowCount = int.Parse(ReturnValueForToken("LINE_LAST_PIXEL     ", result));
    ColumnCount = int.Parse(ReturnValueForToken("SAMPLE_LAST_PIXEL     ", result));

    MinLat = float.Parse(ReturnValueForToken("MINIMUM_LATITUDE     ", result));
    MaxLat = float.Parse(ReturnValueForToken("MAXIMUM_LATITUDE     ", result));
    MinLon = float.Parse(ReturnValueForToken("WESTERNMOST_LONGITUDE     ", result));
    MaxLon = float.Parse(ReturnValueForToken("EASTERNMOST_LONGITUDE     ", result));
    DataScale = float.Parse(ReturnValueForToken("SCALING_FACTOR     ", result));
    MoonRadius = float.Parse(ReturnValueForToken("  OFFSET     ", result));
    SampleBits = int.Parse(ReturnValueForToken("SAMPLE_BITS     ", result));
    SampleType = ReturnValueForToken("SAMPLE_TYPE     ", result);

    if (SampleBits == 16)
    {
      DataSize = 2;
      MinHeight = -8.746f;
      MaxHeight = 10.380f;
      TrueMin = -100f;
      TrueMax = 100f;
    }
    else
    {
      DataSize = 4;
      MinHeight = float.Parse(ReturnValueForToken("  MINIMUM     ", result));
      MaxHeight = float.Parse(ReturnValueForToken("  MAXIMUM     ", result));
      TrueMin = MoonConstants.LowestPointOnTheMoon;
      TrueMax = MoonConstants.HighestPointOnTheMoon;
    }
  }
}
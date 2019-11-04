using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Newtonsoft.Json.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SocialPlatforms;


[Serializable]
public class Crater
{
  public string Name;
  public float Diameter;
  public float Longitude;
  public float Latitude;
  public Vector3 Cartesian;
  public Vector3 Position;


  public Vector3 AsTopDown(Vector3 Offset, float multiplier)
  {
    return new Vector3(Cartesian.x * multiplier, Cartesian.z, Cartesian.y * multiplier) + Offset;
  }

  public Crater(string name, float diameter, float longitude, float latitude, float sphereRadius)
  {
    Name = name;
    Diameter = diameter;
    Longitude = longitude;
    Latitude = latitude;
    Cartesian = new Vector3(Longitude, Latitude, 0f);
  }

  public Vector3 AsSpherical(float radius, Vector3 offset)
  {
    return SphericalCoordinates.SphericalToCartesian(radius, Longitude, Latitude) + offset;
  }


  public void UpdatePosition(Vector3 pos)
  {
    Position = pos;
  }
}

public enum PlaneOrientation
{
  XY,
  XZ
}


public class CraterReader : MonoBehaviour
{
  public TextAsset CraterData;
  public float DiameterThreshold = 50f;
  List<Crater> CraterListFull;

  public List<Crater> CraterList;

  [Range(0f, 1f)] public float Lerp;
  [Range(0, 1000)] public int selection;
  public PlaneOrientation PlaneOrientation;
  public float Radius = 90f;
  public Vector3 Offset = new Vector3(3600f / 2f, 0f, 1800f / 2f);
  public float Multiplier = 10f;
  GUIStyle _style;


  List<Crater> GetCratersByDiameter(float threshold)
  {
    var list = CraterListFull.Where(x => x.Diameter > threshold).ToList();
    return list;
  }


  void OnValidate()
  {
    if (CraterListFull == null)
    {
      Configure();
    }

    if (CraterListFull.Count == 0)
    {
      Configure();
    }

    CraterList = GetCratersByDiameter(DiameterThreshold);
  }


  // Start is called before the first frame update
  public void Configure()
  {
    CraterListFull = new List<Crater>();
    var arr = JArray.Parse(CraterData.text);
    for (int i = 0; i < arr.Count; i++)
    {
      var craterName = arr[i]["1. Crater name "].ToString();
      var craterDiameter = float.Parse(arr[i]["2. Diameter [km]"].ToString());
      var lon = float.Parse(arr[i]["4. Longitude [°]"].ToString());
      var lat = float.Parse(arr[i]["3. Latitude [°]"].ToString());
      CraterListFull.Add(new Crater(craterName, craterDiameter, lon, lat, Radius));
    }

    _style = new GUIStyle();
    _style.fontSize = 24;
    _style.normal.textColor = Color.red;
  }

  void Update()
  {
    if (CraterList != null)
    {
      for (int i = 0; i < CraterList.Count; i++)
      {
        CraterList[i]
          .UpdatePosition(Vector3.Lerp(CraterList[i].Cartesian, CraterList[i].AsSpherical(Radius, Offset), Lerp));
      }
    }
  }


  void OnDrawGizmosSelected()
  {
    if (CraterList != null)
    {
      for (int i = 0; i < CraterList.Count; i++)
      {
        if (i == selection)
        {
          Gizmos.color = Color.red;
        }

        if (CraterList[i].Name == "Tycho")
        {
          Gizmos.color = Color.green;
        }
        else
        {
          Gizmos.color = Color.white;
        }

        switch (PlaneOrientation)
        {
          case PlaneOrientation.XY:
            Gizmos.DrawWireSphere(
              Vector3.Lerp(CraterList[i].Cartesian, CraterList[i].AsSpherical(Radius, Offset), Lerp),
              .009f * CraterList[i].Diameter * 2);
            break;
          case PlaneOrientation.XZ:
            Gizmos.DrawWireSphere(
              Vector3.Lerp(CraterList[i].AsTopDown(Offset, Multiplier), CraterList[i].AsSpherical(Radius, Offset),
                Lerp),
              .009f * CraterList[i].Diameter * 2);
            break;
        }
      }
    }
  }
}
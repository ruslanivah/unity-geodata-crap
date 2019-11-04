using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

[ExecuteInEditMode]
public class CraterUI : MonoBehaviour
{
  public Texture BoxTexture;
  public Camera camera;
  public CraterReader CraterReader;
  public bool AutoPlay;
  [Range(0f, 1f)] public float Scanline;
  [Range(0.1f, 6f)] public float ScanlineSpeed;

  public bool CullWithPlane;

  void Start()
  {
    _style = new GUIStyle();
    _style.fontSize = 28;
    _style.normal.textColor = new Color(1f, 0f, 0f, alpha);
    _boxStyle = new GUIStyle();
    _tex = MakeTex(2, 2, new Color(1f, 0f, 0f, alpha));
    _boxStyle.normal.background = MakeTex(5, 5, new Color(1f, 0f, 0f, alpha));
    //_boxStyle.normal.background = MakeTex(2, 2, new Color(1f, 0f, 0f, alpha));
  }


  Texture2D MakeTex(int width, int height, Color col)
  {
    Color[] pix = new Color[width * height];
    for (int i = 0; i < pix.Length; ++i)
    {
      pix[i] = col;
    }

    Texture2D result = new Texture2D(width, height);
    result.SetPixels(pix);
    result.Apply();
    return result;
  }


  // Update is called once per frame
  void Update()
  {
    if (AutoPlay)
    {
      Scanline = Mathf.PingPong(Time.time * ScanlineSpeed, 1f);
    }
  }


  private void OnDrawGizmos()
  {
    _moonPosition = CraterReader.transform.position;
    _cameraPosition = camera.transform.position;
    _cameraNormal = (_moonPosition - _cameraPosition).normalized;


    _plane = new Plane(_cameraNormal, 0f);
    Gizmos.color = Color.red;
    foreach (var c in scannedCraters)
    {
      Gizmos.DrawWireSphere(c.Position, 1f);
    }

    if (_currentCrater != null)
    {
      Gizmos.DrawLine(_cameraPosition, _currentCrater.AsTopDown(CraterReader.Offset, CraterReader.Multiplier));
    }
  }


  void OnGUI()
  {
    if (CullWithPlane)
    {
      var craterpos = CraterReader.transform.position;
      var campos = camera.transform.position;
      var normal = (craterpos - campos).normalized;
      _plane = new Plane(normal, 0f);
    }

    if (scannedCraters.Count > 0)
    {
      var rnd = UnityEngine.Random.Range(0, scannedCraters.Count);
      _currentCrater = scannedCraters[rnd];
      CraterReader.selection = rnd;
    }

    scannedCraters.Clear();
    foreach (var c in CraterReader.CraterList)
    {
      if (CullWithPlane)
      {
        if (_plane.GetSide(c.Position))
        {
          return;
        }
        else
        {
          continue;
        }
      }

      var p = camera.WorldToScreenPoint(c.AsTopDown(CraterReader.Offset, CraterReader.Multiplier));
      var scan = Mathf.Lerp(0, Screen.height, Scanline);
      GUI.Box(new Rect(0, Screen.height - scan, Screen.width, 8), _tex, _boxStyle);
      if (p.y > scan)
      {
        Vector2 vec = p;
        GUI.Box(new Rect(vec.x, Screen.height - vec.y, BoxSize, BoxSize), _tex, _boxStyle);
        GUI.Label(new Rect(vec.x + 20, Screen.height - vec.y, 100, 20), $"{c.Name}", _style);
        scannedCraters.Add(c);
      }
    }
  }

  GUIStyle _style;
  GUIStyle _boxStyle;
  Texture _tex;
  int BoxSize = 10;
  float alpha = .666f;

  List<Crater> scannedCraters = new List<Crater>();
  Plane _plane;
  Vector3 _cameraPosition;
  Vector3 _cameraNormal;
  Vector3 _moonPosition;
  Crater _currentCrater;
}
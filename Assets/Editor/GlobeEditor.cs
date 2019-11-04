using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlobeTest))]
public class GlobeEditor : Editor
{

  GlobeTest sphere;
  GUIStyle _style;
  void OnEnable()
  {
    _style = new GUIStyle();
    _style.fontSize = 8;
    _style.normal.textColor = Color.white;
    sphere = (GlobeTest) target;
  }

  public void OnSceneGUI()
  {

    if (sphere.pointsReal != null)
    {
      for (int x = 0; x < sphere.pointsReal.GetLength(0); x++)
      {
        for (int y = 0; y < sphere.pointsReal.GetLength(1); y++)
        {
          if (sphere.DrawCoords)
          {
            Handles.Label(sphere.pointsReal[x, y], sphere.points[x, y].ToString(), _style);
          }

          if (sphere.DrawIndices)
          {
            Handles.Label(sphere.pointsReal[x, y], $"{x}_{y}", _style);
          }
        }
      }
    }
  }
}
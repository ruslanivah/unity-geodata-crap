using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraterReader))]
public class CraterEditor : Editor
{
  GUIStyle _style;
  CraterReader reader;

  void OnEnable()
  {
    _style = new GUIStyle();
    _style.fontSize = 18;
    _style.normal.textColor = Color.red;
    reader = (CraterReader) target;
  }


  public override void OnInspectorGUI()
  {
    if (GUILayout.Button("Configure"))
    {
      reader.Configure();
    }

    base.OnInspectorGUI();
  }


  public void OnSceneGUI()
  {
    if (reader.CraterList != null)
    {
      if (reader.selection > reader.CraterList.Count)
      {
        return;
      }

      if (reader.CraterList[reader.selection] != null)
      {
        var pos = Vector3.zero;
        switch (reader.PlaneOrientation)
        {
          case PlaneOrientation.XY:
            pos = Vector3.Lerp(reader.CraterList[reader.selection].Cartesian,
              reader.CraterList[reader.selection].AsSpherical(reader.Radius, reader.Offset), reader.Lerp);
            break;

          case PlaneOrientation.XZ:

            pos = Vector3.Lerp(reader.CraterList[reader.selection].Position,
              reader.CraterList[reader.selection].AsSpherical(reader.Radius, reader.Offset), reader.Lerp);

            break;
        }

        Handles.Label(pos, reader.CraterList[reader.selection].Name, _style);
      }
    }
  }
}
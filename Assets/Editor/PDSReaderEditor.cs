using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PDSReader))]
public class PDSReaderEditor : Editor
{
  PDSReader reader;


  // Start is called before the first frame update
  void OnEnable()
  {
    reader = (PDSReader) target;
  }

  public override void OnInspectorGUI()
  {
    if (GUILayout.Button("Read"))
    {
      reader.Read();
    }

    if (GUILayout.Button("Build"))
    {
      reader.Build();
    }

    if (GUILayout.Button("Clear"))
    {
      reader.GetComponent<MeshFilter>().sharedMesh = new Mesh();
    }

    base.OnInspectorGUI();
  }
}
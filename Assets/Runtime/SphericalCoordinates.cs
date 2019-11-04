using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SphericalCoordinates
{

  static float ConvertToRadians(float angle)
  {
    return (float) (Math.PI / 180) * angle;
  }
  public static Vector3 SphericalToCartesian(float radius, float polar, float elevation)
  {
    polar = ConvertToRadians(polar);
    elevation = ConvertToRadians(elevation);
    var a = radius * Mathf.Cos(elevation);
    var outCart = Vector3.zero;
    outCart.x = a * Mathf.Cos(polar);
    outCart.y = radius * Mathf.Sin(elevation);
    outCart.z = a * Mathf.Sin(polar);
    return outCart;
  }
}
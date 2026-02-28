using System;
using UnityEngine;

public class NotePad : MonoBehaviour
{
   public Study_Lamda studyLamda;
   public void Update()
   {
      if (Input.GetKeyDown(KeyCode.Space))
      {
         studyLamda.StartMethod.Invoke();
      }
   }
}

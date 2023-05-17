using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Mod
{
    public class ModUtils : MonoBehaviour
    {
        public static void LogFields<T>(T Object){
            string output="";
            foreach (FieldInfo fieldinfo in Object.GetType().GetFields())
            {
                output=output+fieldinfo.Name+":"+fieldinfo.GetValue(Object)+"\n";
            }
            Debug.Log(output);
        }

        public static void CopyFields<T>(ref T ObjectA,ref T ObjectB){
            foreach (FieldInfo fieldinfo in ObjectA.GetType().GetFields())
            {
                fieldinfo.SetValue(ObjectB,fieldinfo.GetValue(ObjectA));
            }
        }

        public static object GetPrivate<T>(T Object, string FieldName){
            return typeof(T).GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Object);
        }
    }
}
// Originally uploaded by 'NeitherFishNorCat'. Do not reupload without their explicit permission.

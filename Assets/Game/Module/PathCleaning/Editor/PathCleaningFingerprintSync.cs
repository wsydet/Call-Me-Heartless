using System;
using System.IO;
using System.Security.Cryptography;
using Ember.Basic;
using UnityEditor;
namespace Game.PathCleaning.Editor
{
    public sealed class PathCleaningFingerprintSync : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] old)
        {if(Array.Exists(imported,p=>p.StartsWith("Assets/GameResource/Resources/Config/Tables/path_cleaning")&&p.EndsWith(".bytes")))EditorApplication.delayCall+=Sync;}
        public static void Sync()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            try
            {
                string path="Assets/GameResource/Resources/Config/Tables/path_cleaning.bytes";if(!File.Exists(path))return;
                string hash=PathCleaningModule.BakedFingerprint();
                foreach(var guid in AssetDatabase.FindAssets("t:PathCleaningStepSO"))
                {var asset=AssetDatabase.LoadAssetAtPath<PathCleaningStepSO>(AssetDatabase.GUIDToAssetPath(guid));var so=new SerializedObject(asset);var field=so.FindProperty("_configFingerprint");if(field.stringValue==hash)continue;field.stringValue=hash;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
            }catch(Exception e){EmberDebug.LogError("Game.PathCleaning","小游戏指纹同步失败："+e.Message);}
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
const string path = "Assets/Art/UI/Events/button_normal.png";
byte[] original = File.ReadAllBytes(path);
var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);
var originalHash = AssetDatabase.GetAssetDependencyHash(path);
var texture = new Texture2D(2,2);
try
{
    texture.LoadImage(original);
    texture.SetPixel(texture.width/2,texture.height/2,Color.magenta); texture.Apply();
    File.WriteAllBytes(path,texture.EncodeToPNG());
    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
    var updated = AssetDatabase.LoadAssetAtPath<Sprite>(path);
    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(updated,out string newGuid,out long newId);
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameDevEndingPopupSetup.PrefabPath);
    if (guid != newGuid || id != newId || prefab.GetComponentInChildren<Button>(true).image.sprite != updated || AssetDatabase.GetAssetDependencyHash(path) == originalHash)
        throw new Exception("Sprite replacement contract failed.");
}
finally
{
    File.WriteAllBytes(path,original); AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
    UnityEngine.Object.DestroyImmediate(texture);
}
if (!File.ReadAllBytes(path).SequenceEqual(original)) throw new Exception("Art restore failed.");
var report = "PASS: PNG replacement/reimport changed asset dependency hash, kept popup reference and GUID/local ID, restored original bytes: " + guid + "/" + id;
File.WriteAllText("outputs/ending-popup/art-check.txt",report);
return report;

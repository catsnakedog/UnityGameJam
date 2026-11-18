using System.IO;
using UnityEditor;
using UnityEngine;
namespace ShelterRun.Editor
{
    public static class FarmingSideViewArt
    {
        public const string Root = "Assets/Art/Farming/SideView/";
        static readonly Color32 Ink=C("263B46"), Skin=C("F0BF93");
        public static Color32 C(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c); return c; }
        public static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root+name+".png");
        public static void Create(FarmingDatabase database)
        {
            Directory.CreateDirectory(Root);
            Wall("wall_hall", C("B4B8A9"), C("677D7A"));
            Wall("wall_room", C("C9B799"), C("8D735A"));
            var floor=new Pixels(128,64);floor.Rect(0,0,128,64,C("263D47"));floor.Rect(0,43,128,21,C("6C8384"));floor.Rect(0,60,128,4,C("C3CEC3"));floor.Rect(63,44,2,16,C("425C65"));floor.Rect(0,20,128,2,C("344D56"));Save("floor",floor,Vector2.zero);
            var door=new Pixels(96,192);door.Rect(5,0,86,188,Ink);door.Rect(9,0,78,184,C("C8B595"));door.Rect(15,0,66,178,C("4D696C"));door.Rect(19,8,58,164,C("668182"));door.Rect(24,94,48,62,Ink);door.Rect(28,98,40,54,C("8CB6BA"));door.Rect(29,142,38,8,C("AFD2CC"));door.Rect(62,66,10,5,C("E7BD6D"));door.Rect(19,8,58,4,C("344E56"));Save("door",door,new Vector2(.5f,0));
            var focus=new Pixels(96,192);focus.Rect(4,0,4,190,C("F7CA76"));focus.Rect(88,0,4,190,C("F7CA76"));focus.Rect(4,187,88,4,C("F7CA76"));focus.Rect(4,0,88,4,C("F7CA76"));Save("door_focus",focus,new Vector2(.5f,0));
            var table=new Pixels(192,96);table.Rect(15,0,9,73,Ink);table.Rect(166,0,9,73,Ink);table.Rect(24,32,143,6,C("647B7C"));table.Rect(5,71,182,18,Ink);table.Rect(8,78,176,11,C("D9B583"));table.Rect(8,72,176,6,C("956F4D"));Save("table",table,new Vector2(.5f,0));
            var chair=new Pixels(64,96);chair.Rect(13,0,5,68,Ink);chair.Rect(46,0,5,68,Ink);chair.Rect(10,48,44,38,C("527A71"));chair.Rect(14,53,36,29,C("8FB6A0"));chair.Rect(8,34,48,8,C("9BC5AE"));Save("chair",chair,new Vector2(.5f,0));
            var window=new Pixels(192,128);window.Rect(0,0,192,128,Ink);window.Rect(5,5,182,118,C("91B9BD"));window.Rect(9,9,174,110,C("416779"));window.Rect(14,17,163,89,C("64919C"));window.Rect(15,88,161,14,C("91B9BD"));window.Rect(91,6,7,116,Ink);window.Rect(6,57,180,6,Ink);Save("window",window,new Vector2(.5f,0));
            var bunker=new Pixels(192,192);bunker.Rect(5,0,182,188,Ink);bunker.Rect(10,0,172,182,C("9AC7B7"));bunker.Rect(19,0,154,172,C("385C63"));for(int y=12;y<164;y+=14){bunker.Rect(23,y,146,11,C("507B7A"));bunker.Rect(23,y+9,146,2,C("6B9790"));}bunker.Rect(88,66,16,8,C("D0E3C8"));bunker.Rect(2,0,188,7,C("B0E1C4"));Save("bunker",bunker,new Vector2(.5f,0));
            var ring=new Pixels(128,32);ring.Rect(14,5,100,4,C("F7CA76"));ring.Rect(14,5,4,10,C("F7CA76"));ring.Rect(110,5,4,10,C("F7CA76"));Save("focus",ring,new Vector2(.5f,.15625f));
            Person("player_idle",C("E5A64E"),false,false);Person("player_carry",C("E5A64E"),false,true);
            int index=0;
            foreach(var row in database.Records)
            {
                var color=Color.HSVToRGB(index*.14f%1,.35f,.8f);
                if(row.Type==FarmingObjectType.Person){Person(row.Id,color,false,false);Person(row.Id+"_lying",color,true,false);}
                else if (File.Exists("Assets/Art/UI/Inventory/"+row.Id+".png")) CreateItemSprite(row.Id);
                else
                {
                    var item=new Pixels(64,64);item.Rect(13,2,38,40,Ink);item.Rect(16,5,32,34,color);item.Rect(16,34,32,5,C("E5D8AC"));item.Rect(25,42,14,6,Ink);item.Rect(29,43,6,2,C("C9CDBA"));item.Rect(24,13,16,16,C("F2E4C4"));item.Rect(30,15,4,12,Ink);item.Rect(26,19,12,4,Ink);Save(row.Id,item,new Vector2(.5f,0));
                }
                index++;
            }
            AssetDatabase.Refresh();
        }
        public static void CreateItemSprite(string id)
        {
            // Derive the world sprite from the inventory illustration, aligned to the table surface.
            var icon = new Texture2D(2, 2);
            try
            {
                if (!icon.LoadImage(File.ReadAllBytes("Assets/Art/UI/Inventory/" + id + ".png")))
                    throw new IOException("Cannot load inventory illustration for " + id);
                var pixels = new Pixels(64, 64);
                var source = icon.GetPixels32();
                for (int y = 0; y < 48; y++)
                    for (int x = 0; x < 48; x++)
                        pixels.Data[y * 64 + x + 8] = source[(y * icon.height / 48) * icon.width + x * icon.width / 48];
                Save(id, pixels, new Vector2(.5f, 0));
            }
            finally { Object.DestroyImmediate(icon); }
        }
        static void Wall(string name,Color32 main,Color32 lower)
        {
            var p=new Pixels(192,256);p.Rect(0,0,192,256,main);p.Rect(0,0,192,68,lower);p.Rect(0,0,192,7,Ink);p.Rect(0,66,192,6,C("D8D3B8"));p.Rect(0,247,192,9,Ink);p.Rect(0,238,192,5,C("DED8BE"));p.Rect(0,73,3,165,C("9B9D8E"));p.Rect(188,73,4,165,C("DAD2B6"));p.Rect(9,12,174,42,C("FFFFFF"));
            // Subtle inset panel, with authored colors instead of tinted white primitives.
            p.Rect(11,14,170,38,lower);Save(name,p,Vector2.zero);
        }
        static void Person(string name,Color tint,bool lying,bool carrying)
        {
            var p=new Pixels(lying?128:64,lying?64:96);Color32 dark=tint*.65f;dark.a=255;
            if(lying)
            {
                p.Rect(10,25,38,10,Ink);p.Rect(10,39,38,10,Ink);p.Rect(43,22,39,29,dark);p.Rect(47,28,31,20,tint);p.Rect(49,21,30,6,Skin);p.Rect(83,25,27,27,C("574D42"));p.Rect(85,26,24,21,Skin);p.Rect(100,36,3,3,Ink);Save(name,p,new Vector2(.5f,.5f));return;
            }
            p.Rect(21,0,8,29,Ink);p.Rect(35,0,8,29,Ink);p.Rect(18,0,13,5,C("1C303A"));p.Rect(34,0,14,5,C("1C303A"));p.Rect(17,27,29,31,dark);p.Rect(20,31,24,26,tint);p.Rect(14,30,7,24,dark);p.Rect(43,30,7,24,dark);p.Rect(14,carrying?43:25,8,9,Skin);p.Rect(43,carrying?43:25,8,9,Skin);if(carrying){p.Rect(10,44,15,6,Skin);p.Rect(40,44,15,6,Skin);}p.Rect(25,55,15,8,Skin);p.Rect(19,62,28,25,C("574D42"));p.Rect(23,61,27,20,Skin);p.Rect(45,65,7,10,Skin);p.Rect(39,73,3,3,Ink);p.Rect(18,79,29,9,C("574D42"));p.Rect(18,69,7,13,C("574D42"));Save(name,p,new Vector2(.5f,0));
        }
        static void Save(string name,Pixels p,Vector2 pivot)
        {
            var texture=new Texture2D(p.Width,p.Height,TextureFormat.RGBA32,false);texture.SetPixels32(p.Data);texture.Apply();string path=Root+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=64;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        sealed class Pixels
        {
            public readonly int Width,Height;public readonly Color32[] Data;
            public Pixels(int w,int h){Width=w;Height=h;Data=new Color32[w*h];}
            public void Rect(int x,int y,int w,int h,Color32 color){for(int j=Mathf.Max(0,y);j<Mathf.Min(Height,y+h);j++)for(int i=Mathf.Max(0,x);i<Mathf.Min(Width,x+w);i++)Data[j*Width+i]=color;}
        }
    }
}

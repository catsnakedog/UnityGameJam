using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShelterRun.Editor
{
    // Pixel artwork follows the Basic reference tiles: integer pixels, 64 PPU,
    // 2:1 ground diamonds, no filtering/mipmaps/compression, one restrained palette.
    public static class FarmingIsoArt
    {
        public const string Root = "Assets/Art/Farming/Isometric/";
        static readonly Color32 Ink = C("293E47"), Skin = C("EAB58D"), SkinLight = C("FFD7AF");
        public static Color32 C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        public static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + name + ".png");
        public static void Create(FarmingDatabase db)
        {
            Directory.CreateDirectory(Root);
            Reference("floor_hall", "Flat/base01", C("C2CBC7"));
            Reference("floor_room", "Flat/base01", C("D1B994"));
            Reference("floor_border", "Flat/base01", C("627B80"));
            Reference("floor_door", "Flat/base01", C("7DCEBA"));
            Reference("roof", "Flat/base01", C("748C91"));
            Reference("wall", "Raised/cube01", C("BEA17C"));
            Reference("wall_hall", "Raised/cube01", C("7E999F"));
            Person("player_idle", C("E5A64E"), false, false);
            Person("player_carry", C("E5A64E"), false, true);
            int index = 0;
            foreach (var row in db.Records)
            {
                if (row.Type == FarmingObjectType.Person)
                {
                    Color tint = Color.HSVToRGB((index * .14f) % 1, .35f, .78f);
                    Person(row.Id, tint, false, false); Person(row.Id + "_lying", tint, true, false);
                }
                else Item(row.Id, index);
                index++;
            }
            var t = new Pixels(192, 128);
            t.Ellipse(96, 25, 77, 18, new Color32(25, 40, 47, 55));
            foreach (var p in new[] { new Vector2(27,32), new Vector2(124,16), new Vector2(162,38), new Vector2(63,59) })
                t.Rect((int)p.x, (int)p.y, 5, 31, C("445B60"));
            t.Poly(C("705843"), 18,68, 117,19, 173,47, 74,96);
            t.Poly(C("A77A50"), 18,73, 117,24, 173,52, 74,101);
            t.Poly(C("D8B587"), 21,74, 117,27, 169,53, 74,100);
            Save("table", t, new Vector2(.5f, .23f));
            var chair = new Pixels(64,96);
            chair.Rect(17,8,4,30,C("40565C")); chair.Rect(45,10,4,30,C("40565C"));
            chair.Poly(C("4F686C"), 10,31,32,20,55,32,33,43);
            chair.Poly(C("8CB3A5"), 10,36,32,25,55,37,33,48);
            chair.Poly(C("476D67"), 11,40,16,73,43,80,44,51);
            chair.Poly(C("9AC6B1"), 14,44,18,72,40,77,41,53);
            Save("chair",chair,new Vector2(.5f,.1f));
            var ring = new Pixels(128,64);
            ring.Ellipse(64,24,44,17,C("F5BF6C")); ring.Ellipse(64,24,39,13,new Color32(0,0,0,0));
            Save("focus",ring,new Vector2(.5f,.375f));
            var hatch = new Pixels(192,128);
            hatch.Poly(Ink, 9,48,96,5,183,48,96,92);
            hatch.Poly(C("70C6AE"), 14,49,96,9,178,49,96,89);
            hatch.Poly(C("314E55"), 25,49,96,14,167,49,96,84);
            hatch.Poly(C("4A7477"), 39,49,96,21,153,49,96,77);
            hatch.Poly(C("98D6BE"), 70,48,96,35,122,48,96,61);
            hatch.Rect(89,44,14,8,Ink);
            Save("bunker",hatch,new Vector2(.5f,.38f));
            AssetDatabase.Refresh();
        }
        static void Reference(string name, string source, Color tint)
        {
            string path = "Temp/ReferenceIso/2D_IsoTilemaps_Project/Assets/Tilemaps/Isometric/Sprites/Basic/" + source + ".png";
            // Keep an attributed original alongside derived art so future edits need no network.
            string original = Root + "Reference/" + Path.GetFileName(path);
            Directory.CreateDirectory(Path.GetDirectoryName(original));
            if (!File.Exists(original)) File.Copy(path, original);
            Import("Reference/" + Path.GetFileNameWithoutExtension(original),new Vector2(.5f,.25f));
            var tex = new Texture2D(2,2); tex.LoadImage(File.ReadAllBytes(original));
            var pixels = tex.GetPixels32();
            var originalPixels = (Color32[])pixels.Clone();
            for (int i=0;i<pixels.Length;i++)
            {
                if (pixels[i].a==0) continue;
                Color c = pixels[i]; float shade = Mathf.Clamp(c.grayscale / .55f, .72f, 1.06f);
                if (source.StartsWith("Flat/",StringComparison.Ordinal))
                {
                    int x=i%tex.width,y=i/tex.width;
                    bool edge=x==0||y==0||x==tex.width-1||y==tex.height-1||originalPixels[i-1].a==0||originalPixels[i+1].a==0||originalPixels[i-tex.width].a==0||originalPixels[i+tex.width].a==0;
                    shade=edge?.86f:1;
                }
                pixels[i] = new Color(tint.r*shade,tint.g*shade,tint.b*shade,c.a);
            }
            tex.SetPixels32(pixels); tex.Apply();
            File.WriteAllBytes(Root+name+".png",tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            Import(name,new Vector2(.5f,.25f));
        }
        static void Person(string name, Color color, bool lying, bool carry)
        {
            var p = new Pixels(lying?128:64,lying?64:96);
            var dark = (Color32)(color*.65f); dark.a=255;
            if (lying)
            {
                p.Rect(15,26,29,8,Ink); p.Rect(15,37,29,8,Ink);
                p.Rect(9,25,9,10,C("25343F")); p.Rect(9,36,9,10,C("25343F"));
                p.Rect(41,23,38,26,dark); p.Rect(43,29,34,18,color);
                p.Rect(47,20,31,6,Skin); p.Rect(47,47,31,6,Skin);
                p.Rect(80,27,25,25,C("5A493E")); p.Rect(80,27,23,21,Skin);
                p.Rect(82,30,20,16,SkinLight); p.Rect(94,33,3,3,Ink); p.Rect(94,41,3,3,Ink);
                Save(name,p,new Vector2(.5f,.5f)); return;
            }
            p.Ellipse(32,8,19,6,new Color32(21,40,45,60));
            p.Rect(20,7,9,27,Ink); p.Rect(35,7,9,27,Ink);
            p.Rect(17,6,12,6,C("1F303C")); p.Rect(35,6,12,6,C("1F303C"));
            p.Rect(18,30,28,30,dark); p.Rect(20,33,24,27,color);
            p.Rect(24,56,15,8,Skin); p.Rect(30,36,5,20,C("D2D5C5"));
            p.Rect(13,36,7,21,dark); p.Rect(44,36,7,21,dark);
            p.Rect(13,carry?44:30,8,carry?10:9,Skin); p.Rect(44,carry?44:30,8,carry?10:9,Skin);
            if (carry) { p.Rect(10,45,15,7,SkinLight); p.Rect(39,45,15,7,SkinLight); }
            p.Rect(18,63,29,24,C("5A493E")); p.Rect(19,61,27,22,Skin);
            p.Rect(22,64,23,18,SkinLight); p.Rect(18,80,28,9,C("54453B"));
            p.Rect(18,74,5,11,C("54453B")); p.Rect(29,72,3,3,Ink); p.Rect(40,72,3,3,Ink);
            p.Rect(33,65,6,2,C("BC7B68"));
            Save(name,p,new Vector2(.5f,.083333f));
        }
        static void Item(string name, int index)
        {
            var p=new Pixels(64,64); p.Ellipse(32,12,18,6,new Color32(21,40,45,55));
            var tint=Color.HSVToRGB((index*.17f)%1,.32f,.85f);
            p.Poly(Ink,13,19,32,10,51,19,51,39,32,49,13,39);
            p.Poly(tint*.75f,15,20,32,12,32,32,15,40);
            p.Poly(tint*.9f,32,12,49,20,49,40,32,32);
            p.Poly(tint,15,40,32,48,49,40,32,32);
            // Every existing record gets a clearly visible package and simple pictogram.
            p.Rect(34,19,10,12,C("F3E7C8"));
            p.Rect(38,21,2,8,Ink); p.Rect(35,24,8,2,Ink);
            Save(name,p,new Vector2(.5f,.1875f));
        }
        static void Save(string name,Pixels p,Vector2 pivot)
        {
            var tex=new Texture2D(p.Width,p.Height,TextureFormat.RGBA32,false);
            tex.SetPixels32(p.Data);tex.Apply();File.WriteAllBytes(Root+name+".png",tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);Import(name,pivot);
        }
        static void Import(string name,Vector2 pivot)
        {
            string path=Root+name+".png";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=64;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        sealed class Pixels
        {
            public readonly int Width,Height;public readonly Color32[] Data;
            public Pixels(int width,int height){Width=width;Height=height;Data=new Color32[width*height];}
            void Pixel(int x,int y,Color32 c){if(x>=0&&y>=0&&x<Width&&y<Height)Data[y*Width+x]=c;}
            public void Rect(int x,int y,int w,int h,Color32 c){for(int j=y;j<y+h;j++)for(int i=x;i<x+w;i++)Pixel(i,j,c);}
            public void Ellipse(int cx,int cy,int rx,int ry,Color32 c)
            {for(int y=cy-ry;y<=cy+ry;y++)for(int x=cx-rx;x<=cx+rx;x++)if((x-cx)*(x-cx)/(float)(rx*rx)+(y-cy)*(y-cy)/(float)(ry*ry)<=1)Pixel(x,y,c);}
            public void Poly(Color32 c,params int[] points)
            {
                for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
                {
                    bool inside=false;int count=points.Length/2;
                    for(int i=0,j=count-1;i<count;j=i++)
                    {
                        float ax=points[i*2],ay=points[i*2+1],bx=points[j*2],by=points[j*2+1];
                        if((ay>y)!=(by>y)&&x<(bx-ax)*(y-ay)/(by-ay)+ax)inside=!inside;
                    }
                    if(inside)Pixel(x,y,c);
                }
            }
        }
    }
}

using UnityEditor;
using UnityEngine;

public static class MetaReaColorPresets
{
    public static readonly string[] Names = new[] { "ミッドナイト（黒・濃色の壁）","ウォルナット（茶色・木の壁）","アイボリー（白・ベージュの壁）","ローズ（ピンクの壁）","スカイ（薄青色の壁）","セージ（薄緑色の壁）","ラベンダー（薄紫色の壁）","ストーン（グレーの壁）","カスタム" };
    public static readonly string[] Fields = new[] { "headingColor","bodyColor","secondaryColor","accentColor","buttonTextColor","backgroundColor","cardColor","selectedTabColor","portalColor","borderColor" };
    static readonly string[] Palettes = new[] { "E0F0FF B8D1E3 A3C4DB 2EBAF2 E0F0FF 040911 06101C 084C78 006EA8 1C4766","FFF4E5 EEDBC5 DCC3A5 FFCE8A FFF4E5 292019 382B21 654A31 705333 8B7056","302B25 494136 615649 73501F 302B25 F5F0E7 FFFCF7 DED0B7 D8C3A0 A49883","422632 583C49 705361 843353 422632 F8EDF1 FFF8FA E9C8D5 E4B7C9 B997A5","203C50 355569 49697C 175E82 203C50 EAF3F9 F7FBFF C1DDED AED2E6 829FAF","293F32 405A49 536E5C 306348 293F32 EEF4EC F9FCF6 CCDDC8 BED4BA 8CA38D","38304B 504664 665B7B 634185 38304B F1EDF8 FBF9FF D8CBE9 CBB9E1 A295B7","2C3740 45535F 596A77 315D79 2C3740 EFF1F3 FAFCFD CCD4DB BCCAD6 8D9CA8" };
    public static Color[] Colors(int index) { var hex = Palettes[index].Split(' '); var colors = new Color[hex.Length]; for(int i=0;i<hex.Length;i++) ColorUtility.TryParseHtmlString("#"+hex[i],out colors[i]); return colors; }
    public static int Match(SerializedObject obj) { for(int p=0;p<8;p++){var colors=Colors(p);bool match=true;for(int i=0;i<Fields.Length;i++){var c=obj.FindProperty(Fields[i]).colorValue; if(Mathf.Abs(c.r-colors[i].r)>.004f||Mathf.Abs(c.g-colors[i].g)>.004f||Mathf.Abs(c.b-colors[i].b)>.004f||Mathf.Abs(c.a-1)>.004f)match=false;}if(match)return p;}return 8; }
    public static void Apply(SerializedObject obj,int index) {var colors=Colors(index);for(int i=0;i<Fields.Length;i++)obj.FindProperty(Fields[i]).colorValue=colors[i];}
}


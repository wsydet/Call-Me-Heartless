using System.Collections.Generic;
using UnityEngine;
namespace Game.TalismanPractice
{
    public static class TalismanPatterns
    {
        public static string Name(int index) => index==0?"平安符":index==1?"顺遂符":"安神符";
        public static string LayerName(int layer) => layer==0?"底纹":layer==1?"中层":"细节";
        public static int PartFor(int pattern,int layer) => layer==1 ? (pattern==1?2:pattern==2?1:0) : pattern;
        public static string PartName(int layer,int variant)
        {
            string[][] names={new[]{"横冠","折冠","双冠"},new[]{"十字","回环","折线"},new[]{"一印","双印","三印"}};
            return names[layer][variant];
        }
        private static Vector2 P(float x,float y)=>new Vector2(x,y);
        public static Vector2[][] Layer(int layer,int variant)
        {
            if(variant<0 || variant>2)return new Vector2[0][];
            if(layer==0)
            {
                var crown=variant==0?new[]{P(.23f,.8f),P(.77f,.8f)}
                    :variant==1?new[]{P(.23f,.8f),P(.36f,.9f),P(.5f,.8f),P(.64f,.9f),P(.77f,.8f)}
                    :new[]{P(.23f,.88f),P(.77f,.88f),P(.77f,.78f),P(.23f,.78f)};
                return new[]{crown,new[]{P(.16f,.72f),P(.16f,.12f),P(.3f,.12f)},new[]{P(.84f,.72f),P(.84f,.12f),P(.7f,.12f)}};
            }
            if(layer==1)
            {
                if(variant==0)return new[]{new[]{P(.5f,.72f),P(.5f,.32f)},new[]{P(.27f,.52f),P(.73f,.52f)}};
                if(variant==1)return new[]{new[]{P(.5f,.72f),P(.27f,.52f),P(.5f,.32f),P(.73f,.52f),P(.5f,.72f)}};
                return new[]{new[]{P(.27f,.7f),P(.73f,.57f),P(.27f,.44f),P(.73f,.32f)}};
            }
            var strokes=new List<Vector2[]>();
            for(int i=0;i<=variant;i++){float x=.5f+(i-variant*.5f)*.15f;strokes.Add(new[]{P(x,.26f),P(x,.16f)});}
            return strokes.ToArray();
        }
        public static Vector2[][] Compose(int[] parts)
        {
            var strokes=new List<Vector2[]>();
            for(int layer=0;layer<3;layer++)strokes.AddRange(Layer(layer,parts[layer]));
            return strokes.ToArray();
        }
        public static Vector2[][] Strokes(int pattern)=>Compose(new[]{PartFor(pattern,0),PartFor(pattern,1),PartFor(pattern,2)});
        public static Vector2 DifferenceCenter(int pattern,int difference)=>difference==0?P(.5f,.84f):P(.5f,.21f);
        public static Vector2[][] IncorrectStrokes(int pattern)
        {
            var strokes=new List<Vector2[]>(Strokes(pattern));
            strokes[0]=new[]{P(.23f,.84f),P(.77f,.84f),P(.77f,.94f)};
            strokes.Add(new[]{P(.29f,.21f),P(.71f,.21f)});
            return strokes.ToArray();
        }
    }
}

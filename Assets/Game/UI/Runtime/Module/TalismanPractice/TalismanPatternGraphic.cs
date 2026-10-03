using Game.TalismanPractice;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI
{
    /// <summary>Replaceable geometric placeholder for a whole talisman or one selected layer.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TalismanPatternGraphic : MaskableGraphic
    {
        public int pattern;
        public bool incorrect;
        public bool assembled;
        public int layerOnly=-1;
        public int variant;
        public float thickness=7;
        private int[] _parts={-1,-1,-1};
        public void Show(int id,bool corrupt,int[] parts=null)
        {pattern=id;incorrect=corrupt;if(parts!=null)_parts=(int[])parts.Clone();SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var strokes=layerOnly>=0?TalismanPatterns.Layer(layerOnly,variant):assembled?TalismanPatterns.Compose(_parts)
                :incorrect?TalismanPatterns.IncorrectStrokes(pattern):TalismanPatterns.Strokes(pattern);
            var r=rectTransform.rect;
            for(int s=0;s<strokes.Length;s++)for(int i=1;i<strokes[s].Length;i++)
            {
                var a=new Vector2(r.xMin+strokes[s][i-1].x*r.width,r.yMin+strokes[s][i-1].y*r.height);
                var b=new Vector2(r.xMin+strokes[s][i].x*r.width,r.yMin+strokes[s][i].y*r.height);
                if((b-a).sqrMagnitude<.001f)continue;
                var n=new Vector2(-(b-a).y,(b-a).x).normalized*thickness*.5f;int v=vh.currentVertCount;
                vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);
                vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
                vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);
            }
        }
    }
}

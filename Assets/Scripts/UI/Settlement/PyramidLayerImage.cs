using UnityEngine;
using UnityEngine.UI;

namespace ZombieCardSurvive.UI.Settlement
{
    /// <summary>Readable shape and damage marks without requiring a new art asset.</summary>
    public sealed class PyramidLayerImage : Image
    {
        [SerializeField, Range(0,2)] private int layerIndex;
        [SerializeField] private bool damaged;
        public void Configure(int index) { layerIndex=index; SetVerticesDirty(); }
        public void SetDamaged(bool value) { damaged=value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=GetPixelAdjustedRect();float inset=r.width/(2*(layerIndex+1));
            var a=new Vector2(r.xMin,r.yMin);var b=new Vector2(r.xMax,r.yMin);
            var c=new Vector2(r.xMax-inset,r.yMax);var d=new Vector2(r.xMin+inset,r.yMax);
            Quad(vh,a,b,c,d,color);
            var edge=new Color32(238,229,206,255);
            Line(vh,a,b,2,edge);Line(vh,b,c,2,edge);Line(vh,c,d,2,edge);Line(vh,d,a,2,edge);
            if(!damaged)return;
            var p=new Vector2(r.center.x-r.width*.08f,r.yMax);
            var q=new Vector2(r.center.x+r.width*.06f,r.center.y);
            var z=new Vector2(r.center.x-r.width*.08f,r.yMin);
            Line(vh,p,q,4,edge);Line(vh,q,z,4,edge);
            Line(vh,q,new Vector2(r.center.x+r.width*.22f,r.center.y-r.height*.12f),3,edge);
        }
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color colour)
        {
            if((b-a).sqrMagnitude<.01f)return;
            var direction=(b-a).normalized;var offset=new Vector2(-direction.y,direction.x)*width*.5f;
            Quad(vh,a-offset,b-offset,b+offset,a+offset,colour);
        }
        private static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color colour)
        {
            int index=vh.currentVertCount;
            vh.AddVert(a,colour,Vector2.zero);vh.AddVert(b,colour,Vector2.zero);
            vh.AddVert(c,colour,Vector2.zero);vh.AddVert(d,colour,Vector2.zero);
            vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
        }
    }
}

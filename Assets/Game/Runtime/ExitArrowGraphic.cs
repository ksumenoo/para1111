using UnityEngine;

namespace SecondChance
{
    public sealed class ExitArrowGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper helper)
        {
            helper.Clear();
            foreach(var p in new[]{new Vector2(-21,2),new Vector2(0,29),new Vector2(21,2),new Vector2(-7,4),new Vector2(7,4),new Vector2(7,-24),new Vector2(-7,-24)})
                helper.AddVert(p,color,Vector2.zero);
            helper.AddTriangle(0,1,2);helper.AddTriangle(3,4,5);helper.AddTriangle(3,5,6);
        }
    }
}

using UnityEngine;

namespace Hikaria.AdminSystem.Utilities
{
    public static class FigExt
    {
        public static void HighlightPoint(Camera camera, Vector3 pos, string text, Vector2 textSize, Color textColor, Color crossColor, Color lineColor, Material material, float verticalLineSize = 1f, float crossSize = 0.25f, float textHeightMulti = 0.3f)
        {
            var pos2 = pos + Vector3.up * verticalLineSize;

            if (verticalLineSize > 0)
            {
                Fig.DrawLine(pos, pos2, lineColor, material, 1);
            }

            if (crossSize > 0)
            {
                var fwleft = (Vector3.forward + Vector3.left) * crossSize;
                var fwright = (Vector3.forward + Vector3.right) * crossSize;
                Fig.DrawLine(pos + fwleft, pos - fwleft, crossColor, material, 1);
                Fig.DrawLine(pos + fwright, pos - fwright, crossColor, material, 1);
            }

            if (string.IsNullOrWhiteSpace(text))
                return;

            var rot = Quaternion.LookRotation(pos2 - camera.transform.position, Vector3.up);
            Fig.DrawText(text, pos2 + Vector3.up * textHeightMulti, rot, textSize, textColor, material, TextAnchor.LowerCenter, TextAlignment.Center, 1f);
        }

        public static void DrawFacingText(Camera camera, Vector3 pos, string text, Vector2 textSize, Color textColor, Material material)
        {
            var rot = Quaternion.LookRotation(pos - camera.transform.position, Vector3.up);
            Fig.DrawText(text, pos, rot, textSize, textColor, material, TextAnchor.LowerCenter, TextAlignment.Center, 1f);
        }
    }
}
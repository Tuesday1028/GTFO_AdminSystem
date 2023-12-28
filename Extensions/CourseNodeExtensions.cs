using AIGraph;
using Enemies;
using System.Collections.Generic;
using UnityEngine;

namespace Hikaria.AdminSystem.Extensions
{
    public static class CourseNodeExtensions
    {
        public static List<Vector3> GetRandomPoints(this AIG_CourseNode node, int maxCount)
        {
            List<Placement> list = new();
            if (list.Count < maxCount * 2)
            {
                int num = maxCount * 2 - list.Count;
                for (int i = 0; i < num; i++)
                {
                    list.Add(new Placement(node.m_nodeCluster.GetRandomPosition(false), EnemyGroup.GetRandomRotation()));
                }
            }
            Vector3 vector = list[0].position;
            for (int j = 0; j < list.Count; j++)
            {
                vector += list[j].position;
            }
            vector *= 1f / (float)list.Count;
            Vector3 vector2 = vector;
            float num2 = float.MinValue;
            int num3 = 0;
            for (int k = 0; k < list.Count; k++)
            {
                Vector3 vector3 = list[k].position;
                float num4 = (vector3 - vector).sqrMagnitude;
                if (num4 > num2)
                {
                    vector2 = vector3;
                    num2 = num4;
                    num3 = k;
                }
            }
            list.RemoveAt(num3);
            Vector3 normalized = Random.insideUnitSphere.normalized;
            Vector3 vector4 = Vector3.Slerp(new Vector3(normalized.x, 0f, normalized.y), (vector - vector2).normalized, 0.5f);
            Vector3 vector5 = vector2;
            List<Vector3> list2 = new() { vector2 };
            Vector3 vector6 = vector4;
            float num5 = 9f;
            float num6 = 100f;
            float num7 = 1f / (225f - num6);
            List<int> list3 = new();
            while (list2.Count < maxCount && list.Count > 0)
            {
                float num8 = float.MinValue;
                num3 = -1;
                for (int l = 0; l < list.Count; l++)
                {
                    Vector3 vector3 = list[l].position;
                    Vector3 vector7 = vector3 - vector5;
                    float num4 = vector7.sqrMagnitude;
                    if (num4 < num5)
                    {
                        list3.Add(l);
                    }
                    else
                    {
                        vector7.Normalize();
                        float num9 = Vector3.Dot(vector7, vector4);
                        if (num9 > 0f)
                        {
                            num9 += Mathf.Clamp01((num4 - num6) * num7);
                        }
                        if (num9 > num8)
                        {
                            vector2 = vector3;
                            num8 = num9;
                            num3 = l;
                            vector6 = vector7;
                        }
                    }
                }
                if (num3 == -1)
                {
                    break;
                }
                vector5 = vector2;
                vector4 = vector6;
                list2.Add(vector2);
                for (int m = list3.Count - 1; m > -1; m--)
                {
                    list.RemoveAt(list3[m]);
                }
                list3.Clear();
            }
            return list2;
        }
    }
}

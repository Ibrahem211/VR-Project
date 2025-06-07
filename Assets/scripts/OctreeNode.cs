using System.Collections.Generic;
using UnityEngine;

public class OctreeNode
{
    public Bounds Bounds;
    public int Depth;
    public int MaxDepth;
    public List<int> PointIndices = new(); // 🟢 تخزين إندكسات بدلاً من مواقع
    public OctreeNode[] Children;

    public OctreeNode(Bounds bounds, int depth)
    {
        Bounds = bounds;
        Depth = depth;
    }

    public void Insert(Vector3 point, int index)
    {
        if (!Bounds.Contains(point))
            return;

        if (Depth >= MaxDepth || Bounds.size.magnitude < 0.001f)
        {
            PointIndices.Add(index);
            return;
        }

        if (Children == null)
            Subdivide();

        // تمرير النقطة فقط للطفل الذي يحتويها
        foreach (var child in Children)
        {
            if (child.Bounds.Contains(point))
            {
                child.Insert(point, index);
                return; // نقطة واحدة فقط، إذا وجدت الطفل المناسب نخرج من الدالة
            }
        }
    }


    private void Subdivide()
    {
        Children = new OctreeNode[8];
        Vector3 size = Bounds.size / 2f;
        Vector3 center = Bounds.center;

        for (int i = 0; i < 8; i++)
        {
            Vector3 offset = new Vector3(
                (i & 1) == 0 ? -0.25f : 0.25f,
                (i & 2) == 0 ? -0.25f : 0.25f,
                (i & 4) == 0 ? -0.25f : 0.25f
            );
            Vector3 childCenter = center + Vector3.Scale(size, offset);
            Bounds childBounds = new Bounds(childCenter, size);
            Children[i] = new OctreeNode(childBounds, Depth + 1)
            {
                MaxDepth = MaxDepth
            };
        }
    }

    public void FindNearbyPoints(Vector3 pos, float radius, List<int> results, List<Vector3> allPoints)
    {
        // أولاً، تحقق إذا كانت حدود العقدة قريبة بما فيه الكفاية (تتداخل مع كرة البحث)
        if (!Bounds.Intersects(new Bounds(pos, Vector3.one * radius * 2f)))
            return;

        // تحقق النقاط الموجودة في هذه العقدة (النقاط المخزنة في PointIndices)
        foreach (var idx in PointIndices)
        {
            Vector3 pointPos = allPoints[idx];
            if (Vector3.Distance(pos, pointPos) <= radius)
                results.Add(idx);
        }

        // إذا هناك أبناء، تابع البحث في الأبناء
        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.FindNearbyPoints(pos, radius, results, allPoints);
            }
        }
    }


}


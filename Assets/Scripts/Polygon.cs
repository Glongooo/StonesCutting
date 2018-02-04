using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Polygon
{
    private Vector3[] points; //вершины нашего многоугольника
    private Triangle[] triangles; //треугольники, на которые разбит наш многоугольник
    private bool[] taken; //была ли рассмотрена i-ая вершина многоугольника

    public Polygon(float[] points) //points - х и y координаты
    {
        if (points.Length % 2 == 1 || points.Length < 6)
            throw new System.Exception(); //ошибка, если не многоугольник

        this.points = new Vector3[points.Length / 2]; //преобразуем координаты в вершины
        for (int i = 0; i < points.Length; i += 2)
            this.points[i / 2] = new Vector3(points[i], points[i + 1]);

        triangles = new Triangle[this.points.Length - 2];

        taken = new bool[this.points.Length];

        triangulate(); //триангуляция
    }

    public Polygon(List<Vector3> vertices)
    {
        points = vertices.ToArray();
        triangles = new Triangle[this.points.Length - 2];
        taken = new bool[this.points.Length];
        triangulate();
    }

    private void triangulate()
    {
        int trainPos = 0;
        int leftPoints = points.Length;

        int ai = findNextNotTaken(0);
        int bi = findNextNotTaken(ai + 1);
        int ci = findNextNotTaken(bi + 1);

        int count = 0;

        while (leftPoints > 3)
        {
            if (isLeft(points[ai], points[bi], points[ci]) && canBuildTriangle(ai, bi, ci))
            {
                triangles[trainPos++] = new Triangle(points[ai], points[bi], points[ci]);
                taken[bi] = true;
                leftPoints--;
                bi = ci;
                ci = findNextNotTaken(ci + 1); //берем следующую вершину
            }
            else
            { //берем следующие три вершины
                ai = findNextNotTaken(ai + 1);
                bi = findNextNotTaken(ai + 1);
                ci = findNextNotTaken(bi + 1);
            }

            if (count > points.Length * points.Length)
            { //если по какой-либо причине (например, многоугольник задан по часовой стрелке) триангуляцию провести невозможно, выходим
                triangles = null;
                break;
            }

            count++;
        }

        if (triangles != null) //если триангуляция была проведена успешно
            triangles[trainPos] = new Triangle(points[ai], points[bi], points[ci]);
    }

    private int findNextNotTaken(int startPos) //найти следущую нерассмотренную вершину
    {
        startPos %= points.Length;
        if (!taken[startPos])
            return startPos;

        int i = (startPos + 1) % points.Length;
        while (i != startPos)
        {
            if (!taken[i])
                return i;
            i = (i + 1) % points.Length;
        }

        return -1;
    }

    private bool isLeft(Vector3 a, Vector3 b, Vector3 c) //левая ли тройка векторов
    {
        float abX = b.x - a.x;
        float abY = b.y - a.y;
        float acX = c.x - a.x;
        float acY = c.y - a.y;

        return abX * acY - acX * abY < 0;
    }

    private bool isPointInside(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
    {
        float ab = (a.x - p.x) * (b.y - a.y) - (b.x - a.x) * (a.y - p.y);
        float bc = (b.x - p.x) * (c.y - b.y) - (c.x - b.x) * (b.y - p.y);
        float ca = (c.x - p.x) * (a.y - c.y) - (a.x - c.x) * (c.y - p.y);

        return (ab >= 0 && bc >= 0 && ca >= 0) || (ab <= 0 && bc <= 0 && ca <= 0);
    }

    private bool canBuildTriangle(int ai, int bi, int ci)
    {
        for (int i = 0; i < points.Length; i++)
            if (i != ai && i != bi && i != ci)
                if (isPointInside(points[ai], points[bi], points[ci], points[i]))
                    return false;
        return true;
    }

    public Vector3[] getPoints()
    {
        return points;
    }

    public Triangle[] getTriangles()
    {
        return triangles;
    }

}

public class Triangle
{
    public Vector3 a, b, c;

    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        this.a = a;
        this.b = b;
        this.c = c;
    }
}


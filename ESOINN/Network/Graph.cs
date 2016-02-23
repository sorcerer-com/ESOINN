using System;
using System.Collections.Generic;
using System.Linq;

namespace Main.Network
{
    public class Vertex
    {
        public double[] Weight { get; set; }
        public int ClassId { get; set; }
        public double Density { get; set; }
        public int NumberOfSignals { get; set; }
        public double S { get; set; }

        public List<Edge> Edges { get; private set; }

        public Vertex()
        {
            this.Edges = new List<Edge>();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            Vertex vertex = obj as Vertex;
            if (vertex != null)
            {
                if (this.Weight.Length != vertex.Weight.Length)
                    return false;
                for (int i = 0; i < this.Weight.Length; i++)
                {
                    if (this.Weight[i] != vertex.Weight[i])
                        return false;
                }
                return true;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }

    public class Edge
    {
        public Vertex Vertex1 { get; set; }
        public Vertex Vertex2 { get; set; }
        public int Age { get; set; }

        public Edge(Vertex vertex1, Vertex vertex2)
        {
            this.Vertex1 = vertex1;
            this.Vertex2 = vertex2;
            this.Age = 0;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            Edge edge = obj as Edge;
            if (edge != null)
                return (this.Vertex1 == edge.Vertex1 && this.Vertex2 == edge.Vertex2) ||
                    (this.Vertex1 == edge.Vertex2 && this.Vertex2 == edge.Vertex1);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class Graph
    {
        public List<Vertex> Vertices { get; private set; }
        public List<Edge> Edges { get; private set; }

        public Graph()
        {
            this.Vertices = new List<Vertex>();
            this.Edges = new List<Edge>();
        }


        public Edge GetEdge(Vertex vertex1, Vertex vertex2)
        {
            foreach (var edge in vertex1.Edges) // before this.Edges
            {
                if ((edge.Vertex1 == vertex1 && edge.Vertex2 == vertex2) ||
                    (edge.Vertex1 == vertex2 && edge.Vertex2 == vertex1))
                    return edge;
            }
            return null;
        }

        public List<Edge> GetOutEdges(Vertex vertex)
        {
            /*List<Edge> result = new List<Edge>();
            foreach (var edge in this.Edges)
            {
                if (edge.Vertex1 == vertex || edge.Vertex2 == vertex)
                    result.Add(edge);
            }
            return result;*/
            return vertex.Edges;
        }

        public List<Vertex> GetAdjacentVertices(Vertex vertex)
        {
            List<Vertex> result = new List<Vertex>();
            foreach (var edge in vertex.Edges) // before this.Edges
            {
                if (edge.Vertex1 == vertex)
                    result.Add(edge.Vertex2);
                else if (edge.Vertex2 == vertex)
                    result.Add(edge.Vertex1);
            }
            return result;
        }

        public void ClearVertex(Vertex vertex)
        {
            /*for (int i = 0; i < this.Edges.Count; i++)
            {
                if (this.Edges[i].Vertex1 == vertex || this.Edges[i].Vertex2 == vertex)
                {
                    this.Edges.RemoveAt(i);
                    i--;
                }
            }*/
            foreach(var edge in vertex.Edges)
            {
                if (edge.Vertex1 != vertex)
                    edge.Vertex1.Edges.Remove(edge);
                if (edge.Vertex2 != vertex)
                    edge.Vertex2.Edges.Remove(edge);
                this.Edges.Remove(edge);
            }
            vertex.Edges.Clear();
        }

        public int GetConnectedComponents(out Dictionary<Vertex, int> map)
        {
            map = new Dictionary<Vertex, int>();

            int index = 0;
            foreach (var vertex in this.Vertices)
            {
                if (!map.ContainsKey(vertex))
                {
                    map.Add(vertex, index);

                    List<Vertex> connected = this.GetAdjacentVertices(vertex);
                    for (int i = 0; i < connected.Count; i++)
                    {
                        var temp = this.GetAdjacentVertices(connected[i]);
                        foreach(var temp1 in temp)
                        {
                            if (!connected.Contains(temp1))
                                connected.Add(temp1);
                        }
                        if (!map.ContainsKey(connected[i]))
                            map.Add(connected[i], index);
                        else if (map[connected[i]] != index)
                            Console.WriteLine("class id mismatch");
                    }

                    index++;
                }
            }
            return index;
        }
    }
}

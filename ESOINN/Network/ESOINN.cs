using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Main.Network
{
    public class ESOINN
    {
        private int dim;
        public int Dim
        {
            get { return dim; }
            set { this.dim = value; this.Clear(); }
        }
        public int AgeMax { get; set; }
        public int IterationCount { get; private set; }
        public int IterationThreshold { get; set; }
        public double C1 { get; set; }
        public double C2 { get; set; }

        public Graph Graph { get; private set; }
        public int NumberOfClasses { get; private set; }
        public int NumberOfVertices { get { return this.Graph.Vertices.Count; } }
        public int NumberOfEdges { get { return this.Graph.Edges.Count; } }

        
        public ESOINN(int dim = 2, int ageMax = 30, int iterationThreshold = 50, double c1 = 0.001, double c2 = 1.0)
        {
            this.dim = dim;
            this.AgeMax = ageMax;
            this.IterationThreshold = iterationThreshold;
            this.C1 = c1;
            this.C2 = c2;

            this.Graph = new Graph();
        }

        public Vertex Process(double[] inputSignal)
        {
            if(inputSignal.Length != dim)
                throw new Exception("Incorrect dimension of input signal in ESOINN::addSignal().");
            else
                return addSignal(inputSignal);
            
        }

        public void Classify()
        {
            deleteNoiseVertex();
            /*int index = 0;
            foreach(var v in this.Graph.Vertices)
            {
                v.ClassId = index;
                index++;
            }*/

            Dictionary<Vertex, int> componentMap;
            this.NumberOfClasses = this.Graph.GetConnectedComponents(out componentMap);
            foreach(var v in this.Graph.Vertices)
            {
                v.ClassId = componentMap[v];
            }
        }
        
        public List<double> GetCenterOfCluster(int classId)
        {
            double density = -1;
            Vertex center = null;
            foreach(var v in this.Graph.Vertices)
            {
                if(v.ClassId == classId && v.Density > density)
                {
                    center = v;
                    density = center.Density;
                }
            }
            if (center != null)
                return new List<double>(center.Weight);
            else
                return new List<double>();
        }

        public Vertex GetBestMatch(double[] inputSignal)
        {
            Vertex firstWinner = null;
            double firstWinnerDistance = double.MaxValue;
            foreach(var current in this.Graph.Vertices)
            {
                double dist = distance(inputSignal, current.Weight);
                if(dist < firstWinnerDistance)
                {
                    firstWinner = current;
                    firstWinnerDistance = dist;
                }
            }
            return firstWinner;
        }

        public void Save(string filename)
        {
            using(TextWriter writer = File.CreateText(filename))
            {
                writer.WriteLine(this.dim);
                writer.WriteLine(this.AgeMax);
                writer.WriteLine(this.IterationCount);
                writer.WriteLine(this.IterationThreshold);
                writer.WriteLine(this.C1);
                writer.WriteLine(this.C2);

                writer.WriteLine(this.Graph.Vertices.Count);
                foreach(var vertex in this.Graph.Vertices)
                {
                    writer.WriteLine(string.Join(";", vertex.Weight));
                    writer.WriteLine(vertex.ClassId);
                    writer.WriteLine(vertex.Density);
                    writer.WriteLine(vertex.NumberOfSignals);
                    writer.WriteLine(vertex.S);
                }

                writer.WriteLine(this.Graph.Edges.Count);
                foreach(var edge in this.Graph.Edges)
                {
                    writer.WriteLine(this.Graph.Vertices.IndexOf(edge.Vertex1));
                    writer.WriteLine(this.Graph.Vertices.IndexOf(edge.Vertex2));
                    writer.WriteLine(edge.Age);
                }
            }
        }

        public void Load(string filename)
        {
            this.Clear();
            using (TextReader reader = File.OpenText(filename))
            {
                this.dim = int.Parse(reader.ReadLine());
                this.AgeMax = int.Parse(reader.ReadLine());
                this.IterationCount = int.Parse(reader.ReadLine());
                this.IterationThreshold = int.Parse(reader.ReadLine());
                this.C1 = double.Parse(reader.ReadLine());
                this.C2 = double.Parse(reader.ReadLine());

                int count = int.Parse(reader.ReadLine());
                for (int i = 0; i < count; i++)
                {
                    Vertex vertex = new Vertex();
                    string[] temp = reader.ReadLine().Split(';');
                    vertex.Weight = new double[temp.Length];
                    for (int j = 0; j < temp.Length; j++)
                        vertex.Weight[j] = double.Parse(temp[j]);
                    vertex.ClassId = int.Parse(reader.ReadLine());
                    vertex.Density = double.Parse(reader.ReadLine());
                    vertex.NumberOfSignals = int.Parse(reader.ReadLine());
                    vertex.S = double.Parse(reader.ReadLine());
                    this.Graph.Vertices.Add(vertex);
                }

                count = int.Parse(reader.ReadLine());
                for (int i = 0; i < count; i++)
                {
                    var vert1 = this.Graph.Vertices[int.Parse(reader.ReadLine())];
                    var vert2 = this.Graph.Vertices[int.Parse(reader.ReadLine())];
                    Edge edge = new Edge(vert1, vert2);
                    edge.Age = int.Parse(reader.ReadLine());
                    vert1.Edges.Add(edge);
                    vert2.Edges.Add(edge);
                    this.Graph.Edges.Add(edge);
                }
            }
        }

        public void Clear()
        {
            this.Graph.Vertices.Clear();
            this.Graph.Edges.Clear();
            IterationCount = 0;
            NumberOfClasses = 0;
        }


        private Vertex addSignal(double[] inputSignal)
        {
            if(this.Graph.Vertices.Count < 2)
            {
                Vertex vertex = new Vertex();
                vertex.Weight = new double[inputSignal.Length];
                inputSignal.CopyTo(vertex.Weight, 0);
                vertex.ClassId = -1;
                vertex.Density = 0.0;
                vertex.NumberOfSignals = 0;
                vertex.S = 0;
                this.Graph.Vertices.Add(vertex);
                return vertex;
            }

            Tuple<Vertex, Vertex> winners = findWinners(inputSignal);
            if(!isWithinThreshold(inputSignal, winners.Item1, winners.Item2))
            {
                Vertex vertex = new Vertex();
                vertex.Weight = new double[inputSignal.Length];
                inputSignal.CopyTo(vertex.Weight, 0);
                vertex.ClassId = -1;
                vertex.Density = 0.0;
                vertex.NumberOfSignals = 0;
                vertex.S = 0;
                this.Graph.Vertices.Add(vertex);
                return vertex;
            }

            incrementEdgesAge(winners.Item1);
            if (needAddEdge(winners.Item1, winners.Item2))
            {
                Edge edge = new Edge(winners.Item1, winners.Item2);
                edge.Age = 0;
                winners.Item1.Edges.Remove(edge);
                winners.Item1.Edges.Add(edge);
                winners.Item2.Edges.Remove(edge);
                winners.Item2.Edges.Add(edge);
                this.Graph.Edges.Remove(edge);
                this.Graph.Edges.Add(edge);
            }
            else
            {
                Edge edge = this.Graph.GetEdge(winners.Item1, winners.Item2);
                winners.Item1.Edges.Remove(edge);
                winners.Item2.Edges.Remove(edge);
                this.Graph.Edges.Remove(edge);
            }

            updateDensity(winners.Item1);
            updateWeights(winners.Item1, inputSignal);
            deleteOldEdges();
            if(IterationCount % IterationThreshold == 0)
                updateClassLabels();

            IterationCount++;
            return winners.Item1;
        }

        private Tuple<Vertex, Vertex> findWinners(double[] inputSignal)
        {
            Vertex firstWinner = null;
            Vertex secondWinner = null;
            double firstWinnerDistance = double.MaxValue;
            double secondWinnerDistance = double.MaxValue;
            foreach(var current in this.Graph.Vertices)
            {
                double dist = distance(inputSignal, current.Weight);
                if(dist < firstWinnerDistance)
                {
                    secondWinner = firstWinner;
                    secondWinnerDistance = firstWinnerDistance;
                    firstWinner = current;
                    firstWinnerDistance = dist;
                } 
                else if(dist < secondWinnerDistance) 
                {
                    secondWinner = current;
                    secondWinnerDistance = dist;
                }
            }
            return new Tuple<Vertex,Vertex>(firstWinner, secondWinner);
        }

        private bool isWithinThreshold(double[] inputSignal, Vertex firstWinner, Vertex secondWinner)
        {
            if (distance(inputSignal, firstWinner.Weight) > getSimilarityThreshold(firstWinner))
                return false;

            if (distance(inputSignal, secondWinner.Weight) > getSimilarityThreshold(secondWinner))
                return false;

            return true;
        }

        private double getSimilarityThreshold(Vertex vertex)
        {
            double dist = 0.0;
            if(this.Graph.GetOutEdges(vertex).Count == 0)
            {
                dist = double.MaxValue;
                foreach(var current in this.Graph.Vertices)
                {
                    if(current != vertex)
                    {
                        double distCurrent = distance(vertex.Weight, current.Weight);
                        if(distCurrent < dist)
                            dist = distCurrent;
                    }
                }
            }
            else
            {
                dist = double.MinValue;
                var temp = this.Graph.GetAdjacentVertices(vertex);
                foreach(var current in temp)
                {
                    double distCurrent = distance(vertex.Weight, current.Weight);
                    if(distCurrent > dist) 
                        dist = distCurrent;
                }
            }
            return dist;
        }

        private void incrementEdgesAge(Vertex vertex)
        {
            var temp = this.Graph.GetOutEdges(vertex);
            foreach (var current in temp)
            {
                current.Age++;
            }
        }

        private bool needAddEdge(Vertex firstWinner, Vertex secondWinner)
        {
            if (firstWinner.ClassId == -1 || secondWinner.ClassId == -1)
                return true;
            else if (firstWinner.ClassId == secondWinner.ClassId)
                return true;
            else if (firstWinner.ClassId != secondWinner.ClassId && needMergeClasses(firstWinner, secondWinner))
                return true;

            return false;
        }

        private bool needMergeClasses(Vertex a, Vertex b)
        {
            int A = a.ClassId;
            double meanA = meanDensity(A);
            double maxA = maxDensity(A);
            double thresholdA = densityThershold(meanA, maxA);
            int B = b.ClassId;
            double meanB = meanDensity(B);
            double maxB = maxDensity(B);
            double thresholdB = densityThershold(meanB, maxB);
            double minAB = Math.Min(a.Density, b.Density);

            if(minAB > thresholdA * maxA && minAB > thresholdB * maxB)
                return true;

            return false;
        }

        private void mergeClasses(int A, int B)
        {
            int classId = Math.Min(A, B);
            foreach (var current in this.Graph.Vertices)
            {
                if (current.ClassId == A || current.ClassId == B)
                    current.ClassId = classId;
            }
        }

        private double meanDensity(int classId)
        {
            if(classId == -1) 
                return 0.0;

            int n = 0;
            double density = 0.0;
            foreach (var current in this.Graph.Vertices)
            {
                if(current.ClassId == classId)
                {
                    n++;
                    density += current.Density;
                }
            }
            density *= 1.0 / (double)n;
            return density;
        }

        private double maxDensity(int classId)
        {
            double density = double.MinValue;
            foreach (var current in this.Graph.Vertices)
            {
                if(current.Density > density && current.ClassId == classId)
                    density = current.Density;
            }
            return density;
        }

        private double densityThershold(double mean, double max)
        {
            double threshold;
            if (2.0 * mean >= max)
                threshold = 0.0;
            else if (3.0 * mean >= max && max > 2.0 * mean)
                threshold = 0.5;
            else
                threshold = 1.0;
            return threshold;
        }

        private double meanDistance(Vertex vertex)
        {
            double mDistance = 0.0;
            int m = 0;
            foreach (var current in this.Graph.Vertices)
            {
                mDistance += distance(vertex.Weight, current.Weight);
                m++;
            }
            mDistance *= 1.0 / (double)m;
            return mDistance;
        }

        private void updateDensity(Vertex vertex)
        {
            double mDistance = meanDistance(vertex);
            vertex.NumberOfSignals++;
            vertex.S += 1.0 / ((1 + mDistance)*(1 + mDistance));
            vertex.Density = vertex.S / (double)vertex.NumberOfSignals;
        }

        private void updateWeights(Vertex firstWinner, double[] inputSignal)
        {
            double div = (1.0 / firstWinner.NumberOfSignals);
            for (int i = 0; i < firstWinner.Weight.Length; i++)
                firstWinner.Weight[i] += div * (inputSignal[i] - firstWinner.Weight[i]);

            div = (1.0 / (100 * firstWinner.NumberOfSignals));
            var temp = this.Graph.GetAdjacentVertices(firstWinner);
            foreach(var current in temp)
            {
                for (int i = 0; i < firstWinner.Weight.Length; i++)
                    current.Weight[i] += div * (inputSignal[i] - current.Weight[i]);
            }
        }

        private void deleteOldEdges()
        {
            // TODO: may be different
            for (int i = 0; i < this.Graph.Edges.Count; i++)
            {
                Edge edge = this.Graph.Edges[i];
                if (edge.Age > AgeMax)
                {
                    edge.Vertex1.Edges.Remove(edge);
                    edge.Vertex2.Edges.Remove(edge);
                    this.Graph.Edges.Remove(edge);
                    i--;
                }
            }
        }

        private void updateClassLabels()
        {
            markClasses();
            partitionClasses();
            deleteNoiseVertex();
        }

        private void markClasses()
        {
            List<Vertex> vertexList = new List<Vertex>();
            foreach (var current in this.Graph.Vertices)
            {
                current.ClassId = -1;
                vertexList.Add(current);
            }

            vertexList.Sort((a, b) => b.Density.CompareTo(a.Density)); // TODO: is it ok, a should be before b if a.Density > b.Density

            int classCount = 0;
            foreach (var current in vertexList)
            {
                if (current.ClassId == -1)
                {
                    current.ClassId = classCount;
                    markAdjacentVertices(current, classCount);
                    classCount++;
                }
            }
        }

        private void partitionClasses()
        {
            // TODO: may be different
            for (int i = 0; i < this.Graph.Edges.Count; i++)
            {
                Vertex vertexS = this.Graph.Edges[i].Vertex1;
                Vertex vertexT = this.Graph.Edges[i].Vertex2;

                if(vertexS.ClassId != vertexT.ClassId)
                {
                    if(needMergeClasses(vertexS, vertexT)) 
                        mergeClasses(vertexS.ClassId, vertexT.ClassId);
                    else
                    {
                        vertexS.Edges.Remove(this.Graph.Edges[i]);
                        vertexT.Edges.Remove(this.Graph.Edges[i]);
                        this.Graph.Edges.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        private void markAdjacentVertices(Vertex vertex, int cID)
        {
            var temp = this.Graph.GetAdjacentVertices(vertex);
            foreach(var current in temp)
            {
                if (current.ClassId == -1 && current.Density < vertex.Density)
                {
                    current.ClassId = cID;
                    markAdjacentVertices(current, cID);
                }
            }
        }

        private void deleteNoiseVertex()
        {
            for (int i = 0; i < this.Graph.Vertices.Count; i++)
            {
                Vertex current = this.Graph.Vertices[i];

                double mean = 0.0;
                int outCount = this.Graph.GetOutEdges(current).Count;
                if (outCount != 0) 
                    mean = meanDensity(current.ClassId);
                if ((outCount == 2 && current.Density < C1 * mean) ||
                    (outCount == 1 && current.Density < C2 * mean) ||
                    (outCount == 0))
                {
                    this.Graph.ClearVertex(current);
                    this.Graph.Vertices.Remove(current);
                    i--;
                }
            }
        }

        private double distance(double[] x, double[] y)
        {
            double dist = 0.0, temp = 0.0;
            for (int i = 0; i < x.Length; i++)
            {
                temp = x[i] - y[i];
                dist += temp * temp;
            }
            dist = Math.Sqrt(dist);
            return dist;
        }

    }
}

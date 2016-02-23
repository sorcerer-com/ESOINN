using Main.Network;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;

namespace Main.Samples
{
    public abstract class SampleBase : IDisposable
    {
        public virtual Network.ESOINN Model { get; protected set; }
        public abstract Rectangle Rectangle { get; }
        public virtual int SamplesCount { get; protected set; }
        public virtual bool ReduceNoise { get; set; }
        public virtual List<Vertex> Winners { get; protected set; }

        public abstract bool Process(int sample, bool learn = true);
        public abstract Point GetVertexPosition(Vertex vertex);

        public virtual void Dispose() { }
    }
}

namespace Photo.Net.Core.Struct
{
    internal struct Edge
    {
        public readonly int Miny;   // int
        public readonly int Maxy;   // int
        public int X;      // fixed point: 24.8
        public readonly int Dxdy;   // fixed point: 24.8

        public Edge(int miny, int maxy, int x, int dxdy)
        {
            this.Miny = miny;
            this.Maxy = maxy;
            this.X = x;
            this.Dxdy = dxdy;
        }
    }
}

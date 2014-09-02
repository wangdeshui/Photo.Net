namespace Photo.Net.Core
{
    /// <summary>
    /// Description a line of bit array table, it could be entire line or just a part.
    /// </summary>
    public struct Scanline
    {

        public int X, Y, Length;

        public Scanline(int x, int y, int length)
        {
            X = x;
            Y = y;
            Length = length;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Length.GetHashCode() + X.GetHashCode() + Y.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Scanline)
            {
                var rhs = (Scanline)obj;
                return X == rhs.X && Y == rhs.Y && Length == rhs.Length;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(Scanline lhs, Scanline rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Length == rhs.Length;
        }

        public static bool operator !=(Scanline lhs, Scanline rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + "):[" + Length + "]";
        }
    }
}

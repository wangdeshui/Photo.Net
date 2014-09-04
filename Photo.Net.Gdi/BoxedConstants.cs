namespace Photo.Net.Gdi
{
    /// <summary>
    /// Provides access to a cached group of boxed, commonly used constants.
    /// This helps to avoid boxing overhead, much of which consists of transferring
    /// the item to the heap. Unboxing, on the other hand, is quite cheap.
    /// This is commonly used to pass index values to worker threads.
    /// </summary>
    public static class BoxedConstants
    {
        private static readonly object[] BoxedInt32 = new object[1024];
        private static readonly object BoxedTrue = true;
        private static readonly object BoxedFalse = false;

        public static object GetInt32(int value)
        {
            if (value >= BoxedInt32.Length || value < 0)
            {
                return value;
            }

            return BoxedInt32[value] ?? (BoxedInt32[value] = value);
        }

        public static object GetBoolean(bool value)
        {
            return value ? BoxedTrue : BoxedFalse;
        }

        static BoxedConstants()
        {
        }
    }
}

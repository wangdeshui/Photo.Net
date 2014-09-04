using System;
using System.Collections.Generic;

namespace Photo.Net.Core
{
    public sealed class Vector<T>
    {
        private int _count;
        private T[] _array;

        public Vector()
            : this(10)
        {
        }

        public Vector(int capacity)
        {
            this._array = new T[capacity];
        }

        public Vector(IEnumerable<T> copyMe)
        {
            foreach (T t in copyMe)
            {
                Add(t);
            }
        }

        public void Add(T pt)
        {
            if (this._count >= this._array.Length)
            {
                Grow(this._count + 1);
            }

            this._array[this._count] = pt;
            ++this._count;
        }

        public void Insert(int index, T item)
        {
            if (this._count >= this._array.Length)
            {
                Grow(this._count + 1);
            }

            ++this._count;

            for (int i = this._count - 1; i >= index + 1; --i)
            {
                this._array[i] = this._array[i - 1];
            }

            this._array[index] = item;
        }

        public void Clear()
        {
            this._count = 0;
        }

        public T this[int index]
        {
            get
            {
                return Get(index);
            }

            set
            {
                Set(index, value);
            }
        }

        public T Get(int index)
        {
            if (index < 0 || index >= this._count)
            {
                throw new ArgumentOutOfRangeException("index", index, "0 <= index < count");
            }

            return this._array[index];
        }

        public unsafe T GetUnchecked(int index)
        {
            return this._array[index];
        }

        public void Set(int index, T pt)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", index, "0 <= index");
            }

            if (index >= this._array.Length)
            {
                Grow(index + 1);
            }

            this._array[index] = pt;
        }

        public int Count
        {
            get
            {
                return this._count;
            }
        }

        private void Grow(int min)
        {
            int newSize = this._array.Length;

            if (newSize <= 0)
            {
                newSize = 1;
            }

            while (newSize < min)
            {
                newSize = 1 + ((newSize * 10) / 8);
            }

            var replacement = new T[newSize];

            for (int i = 0; i < this._count; i++)
            {
                replacement[i] = this._array[i];
            }

            this._array = replacement;
        }

        public T[] ToArray()
        {
            var ret = new T[this._count];

            for (int i = 0; i < this._count; i++)
            {
                ret[i] = this._array[i];
            }

            return ret;
        }

        public unsafe T[] UnsafeArray
        {
            get
            {
                return this._array;
            }
        }

        /// <summary>
        /// Gets direct access to the array held by the Vector.
        /// The caller must not modify the array.
        /// </summary>
        /// <remarks>This method is supplied strictly for performance-critical purposes.</remarks>
        public unsafe void GetArrayReadOnly(out T[] arrayResult, out int lengthResult)
        {
            arrayResult = this._array;
            lengthResult = this._count;
        }
    }
}

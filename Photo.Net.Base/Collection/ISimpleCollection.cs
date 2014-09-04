namespace Photo.Net.Base.Collection
{
    public interface ISimpleCollection<K, V>
    {
        V Get(K key);
        void Set(K key, V value);
    }
}

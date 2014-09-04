namespace Photo.Net.Base.Delegate
{
    public delegate void Procedure();
    public delegate void Procedure<in T>(T t);
    public delegate void Procedure<in T, in TU>(T t, TU u);
    public delegate void Procedure<in T, in TU, in TV>(T t, TU u, TV v);
    public delegate void Procedure<in T, in TU, in TV, in TW>(T t, TU u, TV v, TW w);
}

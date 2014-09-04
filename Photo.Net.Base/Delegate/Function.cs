namespace Photo.Net.Base.Delegate
{
    public delegate TR Function<out TR>();
    public delegate TR Function<out TR, in T>(T t);
    public delegate TR Function<out TR, in T, in TU>(T t, TU u);
}

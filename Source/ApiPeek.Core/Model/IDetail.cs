namespace ApiPeek.Core.Model
{
    public interface IDetail
    {
        string Detail { get; }
        string InDetail(string indent);
        string ShortName { get; }
        string ShortString { get; }
    }

    public interface IApiType
    {
        string Name { get; }
        string ShortName { get; }
    }
}
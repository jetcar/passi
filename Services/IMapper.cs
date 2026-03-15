namespace Services
{
    public interface IMapper
    {
        TDestination Map<TDestination>(object source);
    }
}

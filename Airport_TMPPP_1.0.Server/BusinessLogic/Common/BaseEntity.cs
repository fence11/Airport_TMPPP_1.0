namespace Airport_TMPPP_1._0.Server.BusinessLogic.Common
{
    public abstract class BaseEntity : IEntity
    {
        public int Id { get; protected set; }

        public abstract void Validate(); // throw exception if invalid
    }
}

using Airport_TMPPP_1._0.Server.BusinessLogic.Common;

namespace Airport_TMPPP_1._0.Server.BusinessLogic.Entities
{
    public class Airport : BaseEntity
    {
        public string Name { get; private set; } = null!;
        public string Code { get; private set; } = null!;

        private Airport() { }

        public Airport(string name, string code)
        {
            Name = name;
            Code = code;
        }
        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(Code))
                throw new Exception("Airport code required");
        }
    }
}

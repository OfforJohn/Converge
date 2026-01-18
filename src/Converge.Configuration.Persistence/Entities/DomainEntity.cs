using System;

namespace Converge.Configuration.Persistence.Entities
{
    public class DomainEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }
}

using System;

namespace Converge.Configuration.DTOs
{
    public class RollbackRequest
    {
        public int Version { get; set; }
        public Guid? TenantId { get; set; }
    }
}

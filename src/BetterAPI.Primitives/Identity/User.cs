using System;
using System.ComponentModel.DataAnnotations;

namespace BetterAPI.Identity
{
    [InternalResource]
    public sealed class User : IResource
    {
        [Required]
        public Guid Id { get; set; }
        
        public byte[]? PublicKey { get; set; }
    }
}

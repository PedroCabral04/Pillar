using Riok.Mapperly.Abstractions;
using erp.Models;
using erp.DTOs.User;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace erp.Mappings 
{
    [Mapper]
    [SuppressMessage("Mapper","RMG020")]
    public partial class UserMapper
    {
        public partial UserDto UserToUserDto(User user);
        public partial IEnumerable<UserDto> UsersToUserDtos(IEnumerable<User> users);
        
        // Mapeamento que ignora a senha - será gerada pelo UserService
        [MapperIgnoreTarget(nameof(User.PasswordHash))]
        [MapperIgnoreTarget(nameof(User.PasswordChangedAt))]
        [MapperIgnoreTarget(nameof(User.IsActive))]
        [MapperIgnoreTarget(nameof(User.FailedLoginAttempts))]
        [MapperIgnoreTarget(nameof(User.LockedUntil))]
        [MapperIgnoreTarget(nameof(User.LastLoginAt))]
        [MapperIgnoreTarget(nameof(User.CreatedAt))]
        [MapperIgnoreTarget(nameof(User.UserRoles))]
        [MapperIgnoreTarget(nameof(User.Id))]
        
        public partial User CreateUserDtoToUser(CreateUserDto dto);
        
        public partial void UpdateUserDtoToUser(UpdateUserDto dto, User user);
    }
}
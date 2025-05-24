using Riok.Mapperly.Abstractions;
using erp.Models;
using erp.DTOs.User;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;



namespace erp.Mappings {
[Mapper]
[SuppressMessage("Mapper","RMG020")]
public partial class UserMapper
{
    public partial UserDto UserToUserDto(User user);
    public partial IEnumerable<UserDto> UsersToUserDtos(IEnumerable<User> users);
    public partial User CreateUserDtoToUser(CreateUserDto dto);
    public partial void UpdateUserDtoToUser(UpdateUserDto dto, User user);
    }
}

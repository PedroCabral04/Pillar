using Microsoft.AspNetCore.Mvc;
using erp.Services;
using erp.DTOs.User;
using erp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using erp.Mappings;

namespace erp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserMapper _mapper; // Agora usando UserMapper de Mapperly

        public UsersController(IUserService userService, UserMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllAsync();
            var userDtos = _mapper.UsersToUserDtos(users);
            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound($"Usuário com ID {id} não encontrado.");

            var userDto = _mapper.UserToUserDto(user);
            return Ok(userDto);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            var userToCreate = _mapper.CreateUserDtoToUser(createUserDto);

            try
            {
                var createdUser = await _userService.CreateAsync(userToCreate, createUserDto.Password);
                var createdUserDto = _mapper.UserToUserDto(createdUser);
                return CreatedAtAction(nameof(GetUserById), new { id = createdUserDto.Id }, createdUserDto);
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao criar usuário: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            var existingUser = await _userService.GetByIdAsync(id);
            if (existingUser == null)
                return NotFound($"Usuário com ID {id} não encontrado.");

            _mapper.UpdateUserDtoToUser(updateUserDto, existingUser);

            try
            {
                await _userService.UpdateAsync(existingUser, updateUserDto.Password);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao atualizar usuário: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var userToDelete = await _userService.GetByIdAsync(id);
            if (userToDelete == null)
                return NotFound($"Usuário com ID {id} não encontrado.");

            try
            {
                await _userService.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao deletar usuário: {ex.Message}");
            }
        }
    }
}
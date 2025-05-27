using System.Text;
using erp.DAOs;
using erp.Models;
using BCrypt.Net;
using erp.DTOs.User;
using Microsoft.AspNetCore;


namespace erp.Services
{
    public class UserService(IUserDao userDao) : IUserService {
        private const int WorkFactor = 12;

        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*?_-";
            const string uppercase = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*?_-";

            var random = new Random();
            var password = new char[length];
            var charSetBuilder = new StringBuilder();

            // Garantir pelo menos um de cada tipo
            password[0] = uppercase[random.Next(uppercase.Length)];
            password[1] = lowercase[random.Next(lowercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = specialChars[random.Next(specialChars.Length)];

            // Preencher o restante da senha com caracteres aleatórios de todos os conjuntos
            for (int i = 4; i < length; i++)
            {
                password[i] = validChars[random.Next(validChars.Length)];
            }

            // Embaralhar os caracteres da senha para maior aleatoriedade
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }


        public async Task<User> GetByIdAsync(int id)
        {
            return await userDao.GetByIdAsync(id);
        }
        
        // MODIFICADO para chamar GetAllAsyncProjected e retornar IEnumerable<UserDto>
        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            return await userDao.GetAllAsyncProjected();
        }
        
        public async Task<User> CreateAsync(User user)
        {
            // Gera senha temporária ao criar usuário
            string temporaryPassword = GenerateRandomPassword();
                
            ValidatePasswordStrength(temporaryPassword);
            
            user.PasswordHash = HashPassword(temporaryPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            
            return await userDao.CreateAsync(user);
        }
        
        public async Task UpdateAsync(User user, string password = null!)
        {
            var existingUser = await userDao.GetByIdAsync(user.Id);
            
            if (existingUser == null)
                throw new Exception("Usuário não encontrado");
                
            // Atualiza senha se enviada pelo usuário
            if (!string.IsNullOrEmpty(password))
            {
                ValidatePasswordStrength(password);
                user.PasswordHash = HashPassword(password);
                user.PasswordChangedAt = DateTime.UtcNow;
            }
            else
            {
                user.PasswordHash = existingUser.PasswordHash;
                user.PasswordChangedAt = existingUser.PasswordChangedAt;
            }
            
            await userDao.UpdateAsync(user);
        }
        
        public async Task DeleteAsync(int id)
        {
            await userDao.DeleteAsync(id);
        }
        
        public async Task<User> AuthenticateAsync(string email, string password)
        {
            var user = await userDao.GetByEmailAsync(email);
            
            if (user == null! || !user.IsActive)
                return null!;

            // Verifica se a conta está bloqueada
            if (user.IsLocked)
                return null!;
                
            // Verifica a senha
            if (!VerifyPassword(password, user.PasswordHash))
            {
                // Incrementa contagem de falhas e possivelmente bloqueia a conta
                user.FailedLoginAttempts++;
                
                // Se exceder o limite de tentativas (5), bloqueia por 30 minutos
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                }
                
                await userDao.UpdateAsync(user);
                return null!;
            }
            
            // Autenticação bem-sucedida, reseta contadores
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;
            
            await userDao.UpdateAsync(user);
            return user;
        }
        
        private static void ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Senha não pode ser vazia");
                
            if (password.Length < 8)
                throw new ArgumentException("Senha deve ter pelo menos 8 caracteres");
                
            bool hasUpperCase = false;
            bool hasLowerCase = false;
            bool hasDigit = false;
            bool hasSpecialChar = false;
            
            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpperCase = true;
                else if (char.IsLower(c)) hasLowerCase = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSpecialChar = true;
            }
            
            if (!hasUpperCase || !hasLowerCase || !hasDigit || !hasSpecialChar)
                throw new ArgumentException("Senha deve conter letra maiúscula, letra minúscula, número e caractere especial");
        }
        
        private string HashPassword(string password)
        {
            // Usa o BCrypt.Net com salt e fator de trabalho adequados
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
        }
        
        private bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                // Verifica se o hash está em um formato válido antes de tentar verificar
                if (string.IsNullOrEmpty(passwordHash) || !passwordHash.StartsWith("$2"))
                    return false;
                    
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception e)
            {
                Console.WriteLine("Erro ao verificar senha: " + e.Message);
                return false;
            }
        }
        
        public async Task ResetPasswordAsync(int userId, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword))
                throw new ArgumentException("Nova senha não pode ser vazia");
                
            ValidatePasswordStrength(newPassword);
            
            var user = await userDao.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("Usuário não encontrado");
                
            user.PasswordHash = HashPassword(newPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            
            await userDao.UpdateAsync(user);
        }
        
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await userDao.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("Usuário não encontrado");
                
            // Verifica senha atual
            if (!VerifyPassword(currentPassword, user.PasswordHash))
                return false;
                
            ValidatePasswordStrength(newPassword);
            
            user.PasswordHash = HashPassword(newPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            
            await userDao.UpdateAsync(user);
            return true;
        }
        
        public async Task UnlockUserAsync(int userId)
        {
            var user = await userDao.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("Usuário não encontrado");
                
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            
            await userDao.UpdateAsync(user);
        }
        
        public Task<bool> IsPasswordValidAsync(string password)
        {
            try
            {
                ValidatePasswordStrength(password);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
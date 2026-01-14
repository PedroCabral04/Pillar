using erp.DAOs.Financial;
using erp.DTOs.Financial;
using erp.Mappings;
using erp.Models.Financial;

namespace erp.Services.Financial;

public interface IAccountReceivableService
{
    Task<(List<AccountReceivableDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? customerId = null, AccountStatus? status = null,
        DateTime? dueDateFrom = null, DateTime? dueDateTo = null,
        int? categoryId = null, int? costCenterId = null,
        string? sortBy = null, bool sortDescending = false, string? searchText = null);
    
    Task<AccountReceivableDto?> GetByIdAsync(int id);
    Task<List<AccountReceivableDto>> GetOverdueAsync();
    Task<List<AccountReceivableDto>> GetDueSoonAsync(int days = 7);
    Task<decimal> GetTotalByStatusAsync(AccountStatus status);
    Task<decimal> GetTotalByCustomerAsync(int customerId);
    Task<List<AccountReceivableDto>> GetInstallmentsAsync(int parentAccountId);
    
    Task<AccountReceivableDto> CreateAsync(CreateAccountReceivableDto createDto, int userId);
    Task<AccountReceivableDto> UpdateAsync(int id, UpdateAccountReceivableDto updateDto, int userId);
    Task DeleteAsync(int id);
    
    Task<AccountReceivableDto> ReceivePaymentAsync(
        int id, decimal amount, PaymentMethod method, DateTime paymentDate, int userId, 
        string? bankSlipNumber = null, string? pixKey = null,
        decimal? additionalDiscount = null, decimal? additionalInterest = null, decimal? additionalFine = null);
    Task<List<AccountReceivableDto>> CreateInstallmentsAsync(CreateAccountReceivableDto baseDto, int installments, int userId, decimal monthlyInterestRate = 0);
    Task UpdateOverdueStatusAsync();
}

public class AccountReceivableService : IAccountReceivableService
{
    private readonly IAccountReceivableDao _dao;
    private readonly FinancialMapper _mapper;
    private readonly IAccountingService _accountingService;

    public AccountReceivableService(
        IAccountReceivableDao dao,
        FinancialMapper mapper,
        IAccountingService accountingService)
    {
        _dao = dao;
        _mapper = mapper;
        _accountingService = accountingService;
    }

    public async Task<(List<AccountReceivableDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? customerId = null, AccountStatus? status = null,
        DateTime? dueDateFrom = null, DateTime? dueDateTo = null,
        int? categoryId = null, int? costCenterId = null,
        string? sortBy = null, bool sortDescending = false, string? searchText = null)
    {
        var (items, totalCount) = await _dao.GetPagedAsync(
            page, pageSize, customerId, status, dueDateFrom, dueDateTo,
            categoryId, costCenterId, sortBy, sortDescending, searchText);
        
        var dtos = items.Select(x => MapToDto(x)).ToList();
        return (dtos, totalCount);
    }

    public async Task<AccountReceivableDto?> GetByIdAsync(int id)
    {
        var entity = await _dao.GetByIdWithRelationsAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<List<AccountReceivableDto>> GetOverdueAsync()
    {
        var entities = await _dao.GetOverdueAsync();
        return entities.Select(x => MapToDto(x)).ToList();
    }

    public async Task<List<AccountReceivableDto>> GetDueSoonAsync(int days = 7)
    {
        var entities = await _dao.GetDueSoonAsync(days);
        return entities.Select(x => MapToDto(x)).ToList();
    }

    public async Task<decimal> GetTotalByStatusAsync(AccountStatus status)
    {
        return await _dao.GetTotalByStatusAsync(status);
    }

    public async Task<decimal> GetTotalByCustomerAsync(int customerId)
    {
        return await _dao.GetTotalByCustomerAsync(customerId);
    }

    public async Task<List<AccountReceivableDto>> GetInstallmentsAsync(int parentAccountId)
    {
        var entities = await _dao.GetInstallmentsAsync(parentAccountId);
        return entities.Select(x => MapToDto(x)).ToList();
    }

    public async Task<AccountReceivableDto> CreateAsync(CreateAccountReceivableDto createDto, int userId)
    {
        if (createDto.OriginalAmount <= 0)
            throw new ArgumentException("O valor original deve ser maior que zero");

        var entity = _mapper.ToEntity(createDto);
        entity.Status = AccountStatus.Pending;
        
        // Ensure dates are in UTC for PostgreSQL
        entity.IssueDate = DateTime.SpecifyKind(entity.IssueDate, DateTimeKind.Utc);
        entity.DueDate = DateTime.SpecifyKind(entity.DueDate, DateTimeKind.Utc);
        
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedByUserId = userId;

        var created = await _dao.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<AccountReceivableDto> UpdateAsync(int id, UpdateAccountReceivableDto updateDto, int userId)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Conta a receber com ID {id} não encontrada");

        if (entity.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Não é possível editar uma conta já paga");

        _mapper.UpdateEntity(updateDto, entity);
        
        // Ensure dates are in UTC for PostgreSQL
        entity.IssueDate = DateTime.SpecifyKind(entity.IssueDate, DateTimeKind.Utc);
        entity.DueDate = DateTime.SpecifyKind(entity.DueDate, DateTimeKind.Utc);
        if (entity.PaymentDate.HasValue)
            entity.PaymentDate = DateTime.SpecifyKind(entity.PaymentDate.Value, DateTimeKind.Utc);
        
        entity.UpdatedAt = DateTime.UtcNow;

        var updated = await _dao.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Conta a receber com ID {id} não encontrada");

        if (entity.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Não é possível excluir uma conta já paga");

        // Check if has child installments
        var installments = await _dao.GetInstallmentsAsync(id);
        if (installments.Any())
            throw new InvalidOperationException("Não é possível excluir uma conta que possui parcelas");

        // Soft delete (Cancel) instead of hard delete
        entity.Status = AccountStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dao.UpdateAsync(entity);
    }

    public async Task<AccountReceivableDto> ReceivePaymentAsync(
        int id, decimal amount, PaymentMethod method, DateTime paymentDate, int userId,
        string? bankSlipNumber = null, string? pixKey = null,
        decimal? additionalDiscount = null, decimal? additionalInterest = null, decimal? additionalFine = null)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Conta a receber com ID {id} não encontrada");

        if (entity.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Esta conta já foi paga");

        if (amount <= 0)
            throw new ArgumentException("O valor do pagamento deve ser maior que zero");

        // Apply additional values from payment dialog (these are adjustments at payment time)
        if (additionalDiscount.HasValue && additionalDiscount > 0)
            entity.DiscountAmount += additionalDiscount.Value;
        
        if (additionalInterest.HasValue && additionalInterest > 0)
            entity.InterestAmount += additionalInterest.Value;
        
        if (additionalFine.HasValue && additionalFine > 0)
            entity.FineAmount += additionalFine.Value;

        // Update payment info
        entity.PaidAmount += amount;
        entity.PaymentDate = DateTime.SpecifyKind(paymentDate, DateTimeKind.Utc);
        entity.PaymentMethod = method;
        entity.BankSlipNumber = bankSlipNumber;
        entity.PixKey = pixKey;
        entity.ReceivedByUserId = userId;

        // Update status based on the new NetAmount (which considers the additional values)
        entity.Status = _accountingService.DetermineAccountStatus(
            entity.NetAmount,
            entity.PaidAmount,
            entity.DueDate);

        entity.UpdatedAt = DateTime.UtcNow;

        var updated = await _dao.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task<List<AccountReceivableDto>> CreateInstallmentsAsync(
        CreateAccountReceivableDto baseDto, int installments, int userId, decimal monthlyInterestRate = 0)
    {
        if (installments < 2)
            throw new ArgumentException("O número de parcelas deve ser maior que 1");

        var installmentCalculations = _accountingService.CalculateInstallments(
            baseDto.OriginalAmount,
            installments,
            monthlyInterestRate);

        var createdAccounts = new List<AccountReceivableDto>();
        AccountReceivable? parentAccount = null;
        
        // Ensure base dates are in UTC for PostgreSQL
        var issueDate = DateTime.SpecifyKind(baseDto.IssueDate, DateTimeKind.Utc);
        var baseDueDate = DateTime.SpecifyKind(baseDto.DueDate, DateTimeKind.Utc);

        for (int i = 0; i < installmentCalculations.Count; i++)
        {
            var calc = installmentCalculations[i];
            
            // Calculate due date for this installment (add months)
            var dueDate = baseDueDate.AddMonths(i);
            
            var entity = new AccountReceivable
            {
                CustomerId = baseDto.CustomerId,
                InvoiceNumber = $"{baseDto.InvoiceNumber}/{calc.InstallmentNumber}",
                OriginalAmount = calc.PrincipalAmount,
                DiscountAmount = 0,
                InterestAmount = calc.InterestAmount,
                FineAmount = 0,
                PaidAmount = 0,
                IssueDate = issueDate,
                DueDate = dueDate,
                Status = AccountStatus.Pending,
                PaymentMethod = baseDto.PaymentMethod,
                CategoryId = baseDto.CategoryId,
                CostCenterId = baseDto.CostCenterId,
                InstallmentNumber = calc.InstallmentNumber,
                TotalInstallments = installments,
                Notes = baseDto.Notes,
                InternalNotes = baseDto.InternalNotes,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            if (i == 0)
            {
                // First installment becomes the parent
                parentAccount = await _dao.CreateAsync(entity);
                createdAccounts.Add(MapToDto(parentAccount));
            }
            else
            {
                // Subsequent installments reference the parent
                entity.ParentAccountId = parentAccount!.Id;
                var created = await _dao.CreateAsync(entity);
                createdAccounts.Add(MapToDto(created));
            }
        }

        return createdAccounts;
    }

    public async Task UpdateOverdueStatusAsync()
    {
        var allAccounts = await _dao.GetAllAsync();
        var today = DateTime.UtcNow.Date;

        foreach (var account in allAccounts.Where(x => 
            x.Status == AccountStatus.Pending && x.DueDate.Date < today))
        {
            account.Status = AccountStatus.Overdue;
            account.UpdatedAt = DateTime.UtcNow;
            await _dao.UpdateAsync(account);
        }
    }

    private AccountReceivableDto MapToDto(AccountReceivable entity)
    {
        var dto = _mapper.ToDto(entity);
        dto.StatusDescription = entity.Status.ToString();
        dto.PaymentMethodDescription = entity.PaymentMethod.ToString();
        
        if (entity.Customer != null)
            dto.CustomerName = entity.Customer.Name;
        
        if (entity.Category != null)
            dto.CategoryName = entity.Category.Name;
        
        if (entity.CostCenter != null)
            dto.CostCenterName = entity.CostCenter.Name;
        
        if (entity.CreatedByUser != null)
            dto.CreatedByUserName = entity.CreatedByUser.UserName;
        
        if (entity.ReceivedByUser != null)
            dto.ReceivedByUserName = entity.ReceivedByUser.UserName;
        
        return dto;
    }
}

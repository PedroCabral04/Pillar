using erp.DAOs.Financial;
using erp.DTOs.Financial;
using erp.Mappings;
using erp.Models.Financial;

namespace erp.Services.Financial;

public interface IAccountPayableService
{
    Task<(List<AccountPayableDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? supplierId = null, AccountStatus? status = null,
        DateTime? dueDateFrom = null, DateTime? dueDateTo = null,
        int? categoryId = null, int? costCenterId = null, bool? pendingApproval = null,
        string? sortBy = null, bool sortDescending = false);
    
    Task<AccountPayableDto?> GetByIdAsync(int id);
    Task<List<AccountPayableDto>> GetOverdueAsync();
    Task<List<AccountPayableDto>> GetDueSoonAsync(int days = 7);
    Task<List<AccountPayableDto>> GetPendingApprovalAsync();
    Task<decimal> GetTotalByStatusAsync(AccountStatus status);
    Task<decimal> GetTotalBySupplierAsync(int supplierId);
    Task<List<AccountPayableDto>> GetInstallmentsAsync(int parentAccountId);
    
    Task<AccountPayableDto> CreateAsync(CreateAccountPayableDto createDto, int userId);
    Task<AccountPayableDto> UpdateAsync(int id, UpdateAccountPayableDto updateDto, int userId);
    Task DeleteAsync(int id);
    
    Task<AccountPayableDto> ApproveAsync(int id, int userId, string? notes = null);
    Task<AccountPayableDto> PayAsync(int id, decimal amount, PaymentMethod method, DateTime paymentDate, int userId, string? proofUrl = null, string? bankSlipNumber = null, string? pixKey = null);
    Task<List<AccountPayableDto>> CreateInstallmentsAsync(CreateAccountPayableDto baseDto, int installments, int userId, decimal monthlyInterestRate = 0);
    Task UpdateOverdueStatusAsync();
}

public class AccountPayableService : IAccountPayableService
{
    private readonly IAccountPayableDao _dao;
    private readonly FinancialMapper _mapper;
    private readonly IAccountingService _accountingService;
    private const decimal ApprovalThreshold = 5000m;

    public AccountPayableService(
        IAccountPayableDao dao,
        FinancialMapper mapper,
        IAccountingService accountingService)
    {
        _dao = dao;
        _mapper = mapper;
        _accountingService = accountingService;
    }

    public async Task<(List<AccountPayableDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? supplierId = null, AccountStatus? status = null,
        DateTime? dueDateFrom = null, DateTime? dueDateTo = null,
        int? categoryId = null, int? costCenterId = null, bool? pendingApproval = null,
        string? sortBy = null, bool sortDescending = false)
    {
        var (items, totalCount) = await _dao.GetPagedAsync(
            page, pageSize, supplierId, status, null, pendingApproval,
            dueDateFrom, dueDateTo, categoryId, costCenterId, sortBy, sortDescending);
        
        var dtos = items.Select(x => MapToDto(x)).ToList();
        return (dtos, totalCount);
    }

    public async Task<AccountPayableDto?> GetByIdAsync(int id)
    {
        var entity = await _dao.GetByIdWithRelationsAsync(id);
        return entity != null ? MapToDto(entity) : null;
    }

    public async Task<List<AccountPayableDto>> GetOverdueAsync()
    {
        var entities = await _dao.GetOverdueAsync();
        return entities.Select(x => MapToDto(x)).ToList();
    }

    public async Task<List<AccountPayableDto>> GetDueSoonAsync(int days = 7)
    {
        var entities = await _dao.GetDueSoonAsync(days);
        return entities.Select(x => MapToDto(x)).ToList();
    }

    public async Task<List<AccountPayableDto>> GetPendingApprovalAsync()
    {
        var allAccounts = await _dao.GetAllAsync();
        var pending = allAccounts.Where(x => x.RequiresApproval && !x.ApprovedByUserId.HasValue).ToList();
        return pending.Select(x => MapToDto(x)).ToList();
    }

    public async Task<decimal> GetTotalByStatusAsync(AccountStatus status)
    {
        return await _dao.GetTotalByStatusAsync(status);
    }

    public async Task<decimal> GetTotalBySupplierAsync(int supplierId)
    {
        return await _dao.GetTotalBySupplierAsync(supplierId);
    }

    public async Task<List<AccountPayableDto>> GetInstallmentsAsync(int parentAccountId)
    {
        var entities = await _dao.GetInstallmentsAsync(parentAccountId);
        return entities.Select(x => MapToDto(x)).ToList();
    }

    public async Task<AccountPayableDto> CreateAsync(CreateAccountPayableDto createDto, int userId)
    {
        if (createDto.OriginalAmount <= 0)
            throw new ArgumentException("O valor original deve ser maior que zero");

        var entity = _mapper.ToEntity(createDto);
        entity.Status = AccountStatus.Pending;
        
        // Check if requires approval based on amount
        var netAmount = entity.OriginalAmount - entity.DiscountAmount + entity.InterestAmount + entity.FineAmount;
        if (netAmount >= ApprovalThreshold)
            entity.RequiresApproval = true;
        
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedByUserId = userId;

        var created = await _dao.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<AccountPayableDto> UpdateAsync(int id, UpdateAccountPayableDto updateDto, int userId)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Conta a pagar com ID {id} não encontrada");

        if (entity.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Não é possível editar uma conta já paga");

        _mapper.UpdateEntity(updateDto, entity);
        
        // Re-check approval requirement if amount changed
        var netAmount = entity.OriginalAmount - entity.DiscountAmount + entity.InterestAmount + entity.FineAmount;
        if (netAmount >= ApprovalThreshold && !entity.RequiresApproval)
        {
            entity.RequiresApproval = true;
            entity.ApprovedByUserId = null;
            entity.ApprovalDate = null;
        }
        
        entity.UpdatedAt = DateTime.UtcNow;

        var updated = await _dao.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Conta a pagar com ID {id} não encontrada");

        if (entity.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Não é possível excluir uma conta já paga");

        // Check if has child installments
        var installments = await _dao.GetInstallmentsAsync(id);
        if (installments.Any())
            throw new InvalidOperationException("Não é possível excluir uma conta que possui parcelas");

        await _dao.DeleteAsync(id);
    }

    public async Task<AccountPayableDto> ApproveAsync(int id, int userId, string? notes = null)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Conta a pagar com ID {id} não encontrada");

        if (!entity.RequiresApproval)
            throw new InvalidOperationException("Esta conta não requer aprovação");

        if (entity.ApprovedByUserId.HasValue)
            throw new InvalidOperationException("Esta conta já foi aprovada");

        if (entity.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Não é possível aprovar uma conta já paga");

        entity.ApprovedByUserId = userId;
        entity.ApprovalDate = DateTime.UtcNow;
        entity.ApprovalNotes = notes;
        entity.UpdatedAt = DateTime.UtcNow;

        var updated = await _dao.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task<AccountPayableDto> PayAsync(
        int id, decimal amount, PaymentMethod method, DateTime paymentDate, int userId,
        string? proofUrl = null, string? bankSlipNumber = null, string? pixKey = null)
    {
        var entity = await _dao.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Conta a pagar com ID {id} não encontrada");

        if (entity.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Esta conta já foi paga");

        // Check if requires approval
        if (entity.RequiresApproval && !entity.ApprovedByUserId.HasValue)
            throw new InvalidOperationException("Esta conta precisa ser aprovada antes do pagamento");

        if (amount <= 0)
            throw new ArgumentException("O valor do pagamento deve ser maior que zero");

        // Calculate interest and fines if overdue
        if (DateTime.UtcNow > entity.DueDate)
        {
            var (interest, fine) = _accountingService.CalculateOverdueCharges(
                entity.OriginalAmount,
                entity.DueDate);
            
            entity.InterestAmount = interest;
            entity.FineAmount = fine;
        }

        // Update payment info
        entity.PaidAmount += amount;
        entity.PaymentDate = paymentDate;
        entity.PaymentMethod = method;
        entity.BankSlipNumber = bankSlipNumber;
        entity.PixKey = pixKey;
        entity.ProofOfPaymentUrl = proofUrl;
        entity.PaidByUserId = userId;

        // Update status
        entity.Status = _accountingService.DetermineAccountStatus(
            entity.NetAmount,
            entity.PaidAmount,
            entity.DueDate);

        entity.UpdatedAt = DateTime.UtcNow;

        var updated = await _dao.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task<List<AccountPayableDto>> CreateInstallmentsAsync(
        CreateAccountPayableDto baseDto, int installments, int userId, decimal monthlyInterestRate = 0)
    {
        if (installments < 2)
            throw new ArgumentException("O número de parcelas deve ser maior que 1");

        var installmentCalculations = _accountingService.CalculateInstallments(
            baseDto.OriginalAmount,
            installments,
            monthlyInterestRate);

        var createdAccounts = new List<AccountPayableDto>();
        AccountPayable? parentAccount = null;

        for (int i = 0; i < installmentCalculations.Count; i++)
        {
            var calc = installmentCalculations[i];
            
            // Calculate due date for this installment (add months)
            var dueDate = baseDto.DueDate.AddMonths(i);
            
            var entity = new AccountPayable
            {
                SupplierId = baseDto.SupplierId,
                InvoiceNumber = $"{baseDto.InvoiceNumber}/{calc.InstallmentNumber}",
                OriginalAmount = calc.PrincipalAmount,
                DiscountAmount = 0,
                InterestAmount = calc.InterestAmount,
                FineAmount = 0,
                PaidAmount = 0,
                IssueDate = baseDto.IssueDate,
                DueDate = dueDate,
                Status = AccountStatus.Pending,
                PaymentMethod = baseDto.PaymentMethod,
                RequiresApproval = calc.TotalAmount >= ApprovalThreshold,
                CategoryId = baseDto.CategoryId,
                CostCenterId = baseDto.CostCenterId,
                InstallmentNumber = calc.InstallmentNumber,
                TotalInstallments = installments,
                InvoiceAttachmentUrl = baseDto.InvoiceAttachmentUrl,
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

    private AccountPayableDto MapToDto(AccountPayable entity)
    {
        var dto = _mapper.ToDto(entity);
        dto.StatusDescription = entity.Status.ToString();
        dto.PaymentMethodDescription = entity.PaymentMethod.ToString();
        
        if (entity.Supplier != null)
            dto.SupplierName = entity.Supplier.Name;
        
        if (entity.Category != null)
            dto.CategoryName = entity.Category.Name;
        
        if (entity.CostCenter != null)
            dto.CostCenterName = entity.CostCenter.Name;
        
        if (entity.CreatedByUser != null)
            dto.CreatedByUserName = entity.CreatedByUser.UserName;
        
        if (entity.ApprovedByUser != null)
            dto.ApprovedByUserName = entity.ApprovedByUser.UserName;
        
        if (entity.PaidByUser != null)
            dto.PaidByUserName = entity.PaidByUser.UserName;
        
        return dto;
    }
}

using erp.DAOs.Financial;
using erp.DAOs.ServiceOrders;
using erp.Data;
using erp.DTOs.Financial;
using erp.Mappings;
using erp.Models.Financial;
using Microsoft.EntityFrameworkCore;

namespace erp.Services.Financial;

public interface IEmployeePaymentService
{
    Task<(List<EmployeePaymentDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? employeeId = null, EmployeePaymentType? type = null,
        AccountStatus? status = null, int? referenceMonth = null, int? referenceYear = null,
        string? sortBy = null, bool sortDescending = false);

    Task<EmployeePaymentDto?> GetByIdAsync(int id);
    Task<EmployeePaymentDto> CreateAsync(CreateEmployeePaymentDto dto, int userId);
    Task<EmployeePaymentDto> UpdateAsync(int id, UpdateEmployeePaymentDto dto);
    Task DeleteAsync(int id);
    Task<EmployeePaymentDto> PayAsync(int id, PayEmployeePaymentDto dto, int userId);
    Task<List<EmployeePaymentDto>> GenerateServicePaymentsAsync(GenerateServicePaymentsDto dto, int userId);
    Task<object> GetTotalsByStatusAsync();
}

public class EmployeePaymentService : IEmployeePaymentService
{
    private readonly IEmployeePaymentDao _dao;
    private readonly FinancialMapper _mapper;
    private readonly IServiceOrderDao _serviceOrderDao;
    private readonly ApplicationDbContext _context;

    public EmployeePaymentService(
        IEmployeePaymentDao dao,
        FinancialMapper mapper,
        IServiceOrderDao serviceOrderDao,
        ApplicationDbContext context)
    {
        _dao = dao;
        _mapper = mapper;
        _serviceOrderDao = serviceOrderDao;
        _context = context;
    }

    public async Task<(List<EmployeePaymentDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? employeeId = null, EmployeePaymentType? type = null,
        AccountStatus? status = null, int? referenceMonth = null, int? referenceYear = null,
        string? sortBy = null, bool sortDescending = false)
    {
        var (items, totalCount) = await _dao.GetPagedAsync(
            page, pageSize, employeeId, type, status, referenceMonth, referenceYear,
            sortBy, sortDescending);

        var dtos = items.Select(MapWithRelations).ToList();
        return (dtos, totalCount);
    }

    public async Task<EmployeePaymentDto?> GetByIdAsync(int id)
    {
        var payment = await _dao.GetByIdAsync(id);
        return payment == null ? null : MapWithRelations(payment);
    }

    public async Task<EmployeePaymentDto> CreateAsync(CreateEmployeePaymentDto dto, int userId)
    {
        var employee = await _context.Users.FindAsync(dto.EmployeeId ?? 0);
        if (employee == null)
            throw new InvalidOperationException("Funcionário não encontrado");

        if (dto.Type == EmployeePaymentType.SalaryFixed)
        {
            dto.Amount = employee.Salary ?? 0;
        }

        if ((dto.DiscountAmount ?? 0) > (dto.Amount ?? 0))
            throw new InvalidOperationException("Desconto não pode ser maior que o valor");

        var payment = _mapper.ToEntity(dto);
        payment.NetAmount = payment.Amount - payment.DiscountAmount;
        payment.CreatedByUserId = userId;

        var created = await _dao.CreateAsync(payment);
        var loaded = await _dao.GetByIdAsync(created.Id);
        return MapWithRelations(loaded!);
    }

    public async Task<EmployeePaymentDto> UpdateAsync(int id, UpdateEmployeePaymentDto dto)
    {
        var payment = await _dao.GetByIdAsync(id);
        if (payment == null)
            throw new InvalidOperationException("Pagamento não encontrado");

        if (payment.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Não é possível editar um pagamento já realizado");

        if ((dto.DiscountAmount ?? 0) > (dto.Amount ?? 0))
            throw new InvalidOperationException("Desconto não pode ser maior que o valor");

        payment.Amount = dto.Amount ?? 0;
        payment.DiscountAmount = dto.DiscountAmount ?? 0;
        payment.NetAmount = (dto.Amount ?? 0) - (dto.DiscountAmount ?? 0);
        payment.Description = dto.Description;
        payment.DueDate = dto.DueDate;
        payment.PaymentMethod = dto.PaymentMethod;
        payment.CategoryId = dto.CategoryId;
        payment.CostCenterId = dto.CostCenterId;

        await _dao.UpdateAsync(payment);
        var loaded = await _dao.GetByIdAsync(id);
        return MapWithRelations(loaded!);
    }

    public async Task DeleteAsync(int id)
    {
        var payment = await _dao.GetByIdAsync(id);
        if (payment == null)
            throw new InvalidOperationException("Pagamento não encontrado");

        if (payment.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Não é possível excluir um pagamento já realizado");

        payment.Status = AccountStatus.Cancelled;
        await _dao.UpdateAsync(payment);
    }

    public async Task<EmployeePaymentDto> PayAsync(int id, PayEmployeePaymentDto dto, int userId)
    {
        var payment = await _dao.GetByIdAsync(id);
        if (payment == null)
            throw new InvalidOperationException("Pagamento não encontrado");

        if (payment.Status == AccountStatus.Paid)
            throw new InvalidOperationException("Pagamento já realizado");

        payment.Status = AccountStatus.Paid;
        payment.PaymentDate = dto.PaymentDate;
        payment.PaidByUserId = userId;
        if (dto.PaymentMethod.HasValue)
            payment.PaymentMethod = dto.PaymentMethod.Value;

        await _dao.UpdateAsync(payment);
        var loaded = await _dao.GetByIdAsync(id);
        return MapWithRelations(loaded!);
    }

    public async Task<List<EmployeePaymentDto>> GenerateServicePaymentsAsync(
        GenerateServicePaymentsDto dto, int userId)
    {
        var startDate = new DateTime(dto.ReferenceYear ?? 0, dto.ReferenceMonth ?? 0, 1);
        var endDate = startDate.AddMonths(1);

        var completedOrders = await _context.ServiceOrders
            .Include(so => so.Items)
            .Where(so => so.ActualCompletionDate >= startDate
                      && so.ActualCompletionDate < endDate
                      && so.Status == "Completed")
            .ToListAsync();

        var groupedByUser = completedOrders
            .GroupBy(so => so.UserId)
            .ToList();

        var results = new List<EmployeePaymentDto>();

        foreach (var group in groupedByUser)
        {
            var orderCount = group.Count();
            var totalNet = group.Sum(so => so.NetAmount);

            var employee = await _context.Users.FindAsync(group.Key);
            if (employee == null) continue;

            var existingPayments = await _dao.GetByEmployeeAndPeriodAsync(
                group.Key, dto.ReferenceMonth ?? 0, dto.ReferenceYear ?? 0);
            if (existingPayments.Any(p => p.Type == EmployeePaymentType.SalaryByService))
                continue;

            var payment = new EmployeePayment
            {
                EmployeeId = group.Key,
                Type = EmployeePaymentType.SalaryByService,
                Status = AccountStatus.Pending,
                Amount = totalNet,
                DiscountAmount = 0,
                NetAmount = totalNet,
                Description = $"Pagamento por serviços - {orderCount} OS concluídas",
                ReferenceMonth = dto.ReferenceMonth ?? 0,
                ReferenceYear = dto.ReferenceYear ?? 0,
                DueDate = dto.DueDate,
                PaymentMethod = dto.PaymentMethod,
                ServiceCount = orderCount,
                CreatedByUserId = userId,
                TenantId = employee.TenantId ?? 0
            };

            var created = await _dao.CreateAsync(payment);
            var loaded = await _dao.GetByIdAsync(created.Id);
            results.Add(MapWithRelations(loaded!));
        }

        return results;
    }

    public async Task<object> GetTotalsByStatusAsync()
    {
        var pending = await _dao.GetTotalByStatusAsync(AccountStatus.Pending);
        var paid = await _dao.GetTotalByStatusAsync(AccountStatus.Paid);
        var overdue = await _dao.GetTotalByStatusAsync(AccountStatus.Overdue);
        var cancelled = await _dao.GetTotalByStatusAsync(AccountStatus.Cancelled);

        return new { Pending = pending, Paid = paid, Overdue = overdue, Cancelled = cancelled };
    }

    private EmployeePaymentDto MapWithRelations(EmployeePayment payment)
    {
        var dto = _mapper.ToDtoWithRelations(payment);
        dto.TypeName = payment.Type switch
        {
            EmployeePaymentType.SalaryFixed => "Salário Fixo",
            EmployeePaymentType.SalaryByService => "Por Serviço",
            EmployeePaymentType.Advance => "Adiantamento",
            _ => payment.Type.ToString()
        };
        dto.StatusDescription = payment.Status switch
        {
            AccountStatus.Pending => "Pendente",
            AccountStatus.Paid => "Pago",
            AccountStatus.Overdue => "Vencido",
            AccountStatus.Cancelled => "Cancelado",
            AccountStatus.PartiallyPaid => "Parcial",
            _ => payment.Status.ToString()
        };
        dto.PaymentMethodDescription = payment.PaymentMethod switch
        {
            PaymentMethod.Cash => "Dinheiro",
            PaymentMethod.BankSlip => "Boleto",
            PaymentMethod.Pix => "Pix",
            PaymentMethod.CreditCard => "Cartão Crédito",
            PaymentMethod.DebitCard => "Cartão Débito",
            PaymentMethod.BankTransfer => "Transferência",
            PaymentMethod.Check => "Cheque",
            PaymentMethod.Other => "Outro",
            _ => payment.PaymentMethod.ToString()
        };
        return dto;
    }
}

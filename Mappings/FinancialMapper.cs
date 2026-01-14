using Riok.Mapperly.Abstractions;
using erp.Models.Financial;
using erp.DTOs.Financial;

namespace erp.Mappings;

[Mapper]
public partial class FinancialMapper
{
    // Supplier mappings
    public partial SupplierDto ToDto(Supplier supplier);
    public partial SupplierSummaryDto ToSummaryDto(Supplier supplier);
    public partial Supplier ToEntity(CreateSupplierDto dto);
    public partial void UpdateEntity(UpdateSupplierDto dto, Supplier entity);

    [MapProperty(nameof(Supplier.CreatedByUser.UserName), nameof(SupplierDto.CreatedByUserName))]
    [MapProperty(nameof(Supplier.Category.Name), nameof(SupplierDto.CategoryName))]
    public partial SupplierDto ToDtoWithRelations(Supplier supplier);

    [MapProperty(nameof(Supplier.Category.Name), nameof(SupplierSummaryDto.CategoryName))]
    public partial SupplierSummaryDto ToSummaryDtoWithRelations(Supplier supplier);
    
    // Account Receivable mappings
    [MapperIgnoreTarget(nameof(AccountReceivableDto.NetAmount))]
    [MapperIgnoreTarget(nameof(AccountReceivableDto.RemainingAmount))]
    public partial AccountReceivableDto ToDto(AccountReceivable account);
    
    [MapperIgnoreTarget(nameof(AccountReceivableSummaryDto.NetAmount))]
    [MapperIgnoreTarget(nameof(AccountReceivableSummaryDto.RemainingAmount))]
    public partial AccountReceivableSummaryDto ToSummaryDto(AccountReceivable account);
    public partial AccountReceivable ToEntity(CreateAccountReceivableDto dto);
    public partial void UpdateEntity(UpdateAccountReceivableDto dto, AccountReceivable entity);
    
    [MapperIgnoreTarget(nameof(AccountReceivableDto.NetAmount))]
    [MapperIgnoreTarget(nameof(AccountReceivableDto.RemainingAmount))]
    [MapProperty(nameof(AccountReceivable.Customer.Name), nameof(AccountReceivableDto.CustomerName))]
    [MapProperty(nameof(AccountReceivable.Category.Name), nameof(AccountReceivableDto.CategoryName))]
    [MapProperty(nameof(AccountReceivable.CostCenter.Name), nameof(AccountReceivableDto.CostCenterName))]
    [MapProperty(nameof(AccountReceivable.CreatedByUser.UserName), nameof(AccountReceivableDto.CreatedByUserName))]
    [MapProperty(nameof(AccountReceivable.ReceivedByUser.UserName), nameof(AccountReceivableDto.ReceivedByUserName))]
    public partial AccountReceivableDto ToDtoWithRelations(AccountReceivable account);
    
    [MapperIgnoreTarget(nameof(AccountReceivableSummaryDto.NetAmount))]
    [MapperIgnoreTarget(nameof(AccountReceivableSummaryDto.RemainingAmount))]
    [MapProperty(nameof(AccountReceivable.Customer.Name), nameof(AccountReceivableSummaryDto.CustomerName))]
    public partial AccountReceivableSummaryDto ToSummaryDtoWithRelations(AccountReceivable account);
    
    // Account Payable mappings
    [MapperIgnoreTarget(nameof(AccountPayableDto.NetAmount))]
    [MapperIgnoreTarget(nameof(AccountPayableDto.RemainingAmount))]
    public partial AccountPayableDto ToDto(AccountPayable account);
    
    [MapperIgnoreTarget(nameof(AccountPayableSummaryDto.NetAmount))]
    [MapperIgnoreTarget(nameof(AccountPayableSummaryDto.RemainingAmount))]
    public partial AccountPayableSummaryDto ToSummaryDto(AccountPayable account);
    public partial AccountPayable ToEntity(CreateAccountPayableDto dto);
    public partial void UpdateEntity(UpdateAccountPayableDto dto, AccountPayable entity);
    
    [MapperIgnoreTarget(nameof(AccountPayableDto.NetAmount))]
    [MapperIgnoreTarget(nameof(AccountPayableDto.RemainingAmount))]
    [MapProperty(nameof(AccountPayable.Supplier.Name), nameof(AccountPayableDto.SupplierName))]
    [MapProperty(nameof(AccountPayable.Category.Name), nameof(AccountPayableDto.CategoryName))]
    [MapProperty(nameof(AccountPayable.CostCenter.Name), nameof(AccountPayableDto.CostCenterName))]
    [MapProperty(nameof(AccountPayable.CreatedByUser.UserName), nameof(AccountPayableDto.CreatedByUserName))]
    [MapProperty(nameof(AccountPayable.PaidByUser.UserName), nameof(AccountPayableDto.PaidByUserName))]
    [MapProperty(nameof(AccountPayable.ApprovedByUser.UserName), nameof(AccountPayableDto.ApprovedByUserName))]
    public partial AccountPayableDto ToDtoWithRelations(AccountPayable account);
    
    [MapperIgnoreTarget(nameof(AccountPayableSummaryDto.NetAmount))]
    [MapperIgnoreTarget(nameof(AccountPayableSummaryDto.RemainingAmount))]
    [MapProperty(nameof(AccountPayable.Supplier.Name), nameof(AccountPayableSummaryDto.SupplierName))]
    public partial AccountPayableSummaryDto ToSummaryDtoWithRelations(AccountPayable account);
    
    // Financial Category mappings
    [MapperIgnoreSource(nameof(FinancialCategory.ParentCategory))]
    [UserMapping(Default = true)]
    public partial FinancialCategoryDto ToDto(FinancialCategory category);
    public partial FinancialCategory ToEntity(CreateFinancialCategoryDto dto);
    public partial void UpdateEntity(UpdateFinancialCategoryDto dto, FinancialCategory entity);
    
    [MapProperty(nameof(FinancialCategory.ParentCategory.Name), nameof(FinancialCategoryDto.ParentCategoryName))]
    public partial FinancialCategoryDto ToDtoWithRelations(FinancialCategory category);
    
    // Cost Center mappings
    public partial CostCenterDto ToDto(CostCenter costCenter);
    public partial CostCenter ToEntity(CreateCostCenterDto dto);
    public partial void UpdateEntity(UpdateCostCenterDto dto, CostCenter entity);
    
    [MapProperty(nameof(CostCenter.Manager.UserName), nameof(CostCenterDto.ManagerName))]
    public partial CostCenterDto ToDtoWithRelations(CostCenter costCenter);
}

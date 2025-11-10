using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using erp.DTOs.Financial;
using erp.Services.Financial;
using erp.Services.Financial.Validation;
using System.Security.Claims;

namespace erp.Controllers;

/// <summary>
/// Controller para gerenciamento de fornecedores (cadastro, consulta e integrações externas)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(ISupplierService supplierService, ILogger<SuppliersController> logger)
    {
        _supplierService = supplierService;
        _logger = logger;
    }

    /// <summary>
    /// Lista fornecedores com paginação, busca e ordenação
    /// </summary>
    /// <param name="page">Número da página (para paginação)</param>
    /// <param name="pageSize">Itens por página</param>
    /// <param name="search">Termo de busca (nome, razão social, CNPJ/CPF)</param>
    /// <param name="activeOnly">Filtrar apenas fornecedores ativos (true/false/null=todos)</param>
    /// <param name="sortBy">Campo para ordenação (Name, Document, CreatedAt)</param>
    /// <param name="sortDescending">Ordenação descendente (true/false)</param>
    /// <returns>Lista de fornecedores ou resultado paginado</returns>
    /// <response code="200">Fornecedores retornados com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// **Modo de operação:**
    /// - **Sem paginação:** Se page e pageSize não forem fornecidos, retorna lista completa
    /// - **Com paginação:** Se page e pageSize forem fornecidos, retorna objeto paginado com metadados
    /// 
    /// Busca (search) procura em:
    /// - Nome fantasia
    /// - Razão social
    /// - CNPJ/CPF
    /// 
    /// Ordenação disponível por:
    /// - Name (nome/razão social)
    /// - Document (CNPJ/CPF)
    /// - CreatedAt (data de cadastro)
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        try
        {
            if (page.HasValue && pageSize.HasValue)
            {
                var result = await _supplierService.GetPagedAsync(page.Value, pageSize.Value, search, activeOnly, sortBy, sortDescending);
                return Ok(result);
            }
            else
            {
                var suppliers = await _supplierService.GetAllAsync(activeOnly ?? true);
                return Ok(suppliers);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suppliers");
            return StatusCode(500, "Erro ao buscar fornecedores");
        }
    }

    /// <summary>
    /// Busca fornecedor por ID
    /// </summary>
    /// <param name="id">ID do fornecedor</param>
    /// <returns>Dados completos do fornecedor incluindo endereço e contatos</returns>
    /// <response code="200">Fornecedor encontrado</response>
    /// <response code="404">Fornecedor não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierDto>> GetById(int id)
    {
        try
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            if (supplier == null)
            {
                return NotFound($"Fornecedor com ID {id} não encontrado");
            }
            return Ok(supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier {SupplierId}", id);
            return StatusCode(500, "Erro ao buscar fornecedor");
        }
    }

    /// <summary>
    /// Cadastra novo fornecedor
    /// </summary>
    /// <param name="dto">Dados do fornecedor: documento (CNPJ/CPF), razão social, nome fantasia, endereço, contatos</param>
    /// <returns>Fornecedor criado</returns>
    /// <response code="201">Fornecedor cadastrado com sucesso</response>
    /// <response code="400">Dados inválidos ou CNPJ/CPF duplicado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Validações automáticas:
    /// - CNPJ/CPF deve ser válido (validação de dígitos verificadores)
    /// - CNPJ/CPF não pode estar duplicado no sistema
    /// - Email deve estar em formato válido
    /// - Telefone é obrigatório
    /// - Endereço completo é recomendado (CEP, logradouro, cidade, estado)
    /// 
    /// Campos obrigatórios:
    /// - Document (CNPJ ou CPF)
    /// - CompanyName (Razão Social) ou TradeName (Nome Fantasia)
    /// - Phone
    /// 
    /// **Dica:** Use GET /receita-ws/{cnpj} para preencher automaticamente dados da empresa.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId))
            {
                return Unauthorized();
            }

            var supplier = await _supplierService.CreateAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
            return StatusCode(500, "Erro ao criar fornecedor");
        }
    }

    /// <summary>
    /// Atualiza dados de fornecedor existente
    /// </summary>
    /// <param name="id">ID do fornecedor</param>
    /// <param name="dto">Dados atualizados do fornecedor</param>
    /// <returns>Fornecedor atualizado</returns>
    /// <response code="200">Fornecedor atualizado com sucesso</response>
    /// <response code="400">Dados inválidos ou CNPJ/CPF duplicado</response>
    /// <response code="404">Fornecedor não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// Permite atualizar todos os campos do fornecedor exceto ID e data de criação.
    /// 
    /// Validações:
    /// - Se CNPJ/CPF for alterado, valida duplicidade
    /// - Email e telefone mantêm mesmas validações do cadastro
    /// </remarks>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierDto>> Update(int id, [FromBody] UpdateSupplierDto dto)
    {
        try
        {
            var supplier = await _supplierService.UpdateAsync(id, dto);
            if (supplier == null)
            {
                return NotFound($"Fornecedor com ID {id} não encontrado");
            }
            return Ok(supplier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
            return StatusCode(500, "Erro ao atualizar fornecedor");
        }
    }

    /// <summary>
    /// Remove fornecedor do sistema
    /// </summary>
    /// <param name="id">ID do fornecedor</param>
    /// <returns>Confirmação de exclusão</returns>
    /// <response code="204">Fornecedor excluído com sucesso</response>
    /// <response code="400">Fornecedor possui contas a pagar vinculadas e não pode ser excluído</response>
    /// <response code="404">Fornecedor não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno</response>
    /// <remarks>
    /// **Atenção:** Exclusão é bloqueada se existirem contas a pagar associadas ao fornecedor.
    /// 
    /// Regras de exclusão:
    /// - Fornecedor com contas a pagar em aberto: **Bloqueado**
    /// - Fornecedor com contas pagas: **Bloqueado** (histórico financeiro)
    /// - Fornecedor sem movimentação financeira: **Permitido**
    /// 
    /// **Alternativa:** Use inativação (campo IsActive) em vez de exclusão para manter histórico.
    /// </remarks>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _supplierService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier {SupplierId}", id);
            return StatusCode(500, "Erro ao excluir fornecedor");
        }
    }

    /// <summary>
    /// Consulta dados de empresa na Receita Federal por CNPJ
    /// </summary>
    /// <param name="cnpj">CNPJ da empresa (com ou sem máscara)</param>
    /// <returns>Dados da empresa: razão social, nome fantasia, endereço, atividade principal, situação cadastral</returns>
    /// <response code="200">Dados da empresa retornados com sucesso</response>
    /// <response code="404">CNPJ não encontrado ou inválido</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro ao consultar API externa ou limite de requisições excedido</response>
    /// <remarks>
    /// **Integração com ReceitaWS (API pública gratuita)**
    /// 
    /// Retorna dados oficiais da Receita Federal incluindo:
    /// - Razão Social
    /// - Nome Fantasia
    /// - CNAE (atividade econômica)
    /// - Endereço completo
    /// - Situação cadastral (Ativa, Baixada, Suspensa)
    /// - Data de abertura
    /// - Capital social
    /// 
    /// **Uso recomendado:** 
    /// 1. Usuário digita CNPJ
    /// 2. Sistema consulta este endpoint
    /// 3. Preenche automaticamente formulário de cadastro
    /// 4. Usuário revisa e confirma
    /// 
    /// **Limitações:**
    /// - ReceitaWS tem limite de 3 requisições por minuto
    /// - API pode estar indisponível em horários de pico
    /// - CNPJ deve ser válido e estar ativo na Receita
    /// 
    /// **Exemplo:** GET /receita-ws/12345678000190
    /// </remarks>
    [HttpGet("receita-ws/{cnpj}")]
    [ProducesResponseType(typeof(ReceitaWsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReceitaWsResponse>> GetCompanyData(string cnpj)
    {
        try
        {
            var data = await _supplierService.GetCompanyDataAsync(cnpj);
            if (data == null)
            {
                return NotFound($"Dados não encontrados para o CNPJ {cnpj}");
            }
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company data for CNPJ {Cnpj}", cnpj);
            return StatusCode(500, "Erro ao consultar dados da empresa");
        }
    }

    /// <summary>
    /// Consulta endereço por CEP usando ViaCEP
    /// </summary>
    /// <param name="cep">CEP para consulta (com ou sem máscara)</param>
    /// <returns>Endereço completo: logradouro, bairro, cidade, estado</returns>
    /// <response code="200">Endereço encontrado com sucesso</response>
    /// <response code="404">CEP não encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro ao consultar API externa</response>
    /// <remarks>
    /// **Integração com ViaCEP (API pública gratuita)**
    /// 
    /// Retorna dados de endereço incluindo:
    /// - Logradouro (rua, avenida, etc)
    /// - Complemento (quando disponível)
    /// - Bairro
    /// - Cidade
    /// - Estado (UF)
    /// - IBGE (código da cidade)
    /// 
    /// **Uso recomendado:**
    /// 1. Usuário digita CEP no formulário
    /// 2. Sistema consulta este endpoint ao sair do campo
    /// 3. Preenche automaticamente campos de endereço
    /// 4. Usuário informa apenas número e complemento
    /// 
    /// **CEP aceito com ou sem máscara:**
    /// - 01310-100 ✓
    /// - 01310100 ✓
    /// 
    /// **Exemplo:** GET /viacep/01310100
    /// </remarks>
    [HttpGet("viacep/{cep}")]
    [ProducesResponseType(typeof(ViaCepResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ViaCepResponse>> GetAddress(string cep)
    {
        try
        {
            var data = await _supplierService.GetAddressAsync(cep);
            if (data == null)
            {
                return NotFound($"Endereço não encontrado para o CEP {cep}");
            }
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting address for CEP {Cep}", cep);
            return StatusCode(500, "Erro ao consultar endereço");
        }
    }
}

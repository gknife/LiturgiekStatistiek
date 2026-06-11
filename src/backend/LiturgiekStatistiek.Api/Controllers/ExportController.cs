using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.IO;

namespace LiturgiekStatistiek.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IQueryService _queryService;

    public ExportController(IQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpPost("excel")]
    public async Task<IActionResult> ExportToExcel([FromBody] QueryRequest request, CancellationToken ct)
    {
        QueryResult result;
        if (!string.IsNullOrEmpty(request.NaturalLanguageQuery))
            result = await _queryService.ExecuteNaturalLanguageAsync(request.NaturalLanguageQuery, ct);
        else if (!string.IsNullOrEmpty(request.TemplateId) && request.Parameters != null)
            result = await _queryService.ExecuteTemplateAsync(request.TemplateId, request.Parameters, ct);
        else
            return BadRequest();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(result.Title.Length > 31 ? result.Title[..31] : result.Title);

        // Headers
        for (int col = 0; col < result.Columns.Count; col++)
        {
            worksheet.Cell(1, col + 1).Value = result.Columns[col];
            worksheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        // Data rows
        for (int row = 0; row < result.Rows.Count; row++)
        {
            for (int col = 0; col < result.Columns.Count; col++)
            {
                var key = result.Columns[col];
                if (result.Rows[row].TryGetValue(key, out var value) && value != null)
                    worksheet.Cell(row + 2, col + 1).Value = value.ToString();
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{result.Title}.xlsx");
    }
}

using GSheetConnector.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GSheetConnector.Controllers
{
    public class GoogleSheetsController : ControllerBase
    {
        private readonly GoogleSheetsService _googleSheetsService;

        public GoogleSheetsController(GoogleSheetsService googleSheetsService)
        {
            _googleSheetsService = googleSheetsService;
        }

        [HttpGet("spreadsheet-data")]
        public IActionResult GetSpreadsheetData()
        {
            _googleSheetsService.UpdateCellAsync("Лист1!B1", "Updated Value");
            return Ok("Data fetched");
        }

        [HttpPost("update-cell-with-datetime")]
        public async Task<IActionResult> UpdateCellWithDateTime(string range)
        {
            try
            {
                // Обновляем ячейку
                await _googleSheetsService.UpdateCellWithCurrentDateTimeAsync(range);
                return Ok("Дата и время успешно обновлены в ячейке.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка при обновлении ячейки: {ex.Message}");
            }
        }

        [HttpPost("update-first-empty-cell")]
        public async Task<IActionResult> UpdateFirstEmptyCell()
        {
            try
            {
                // Обновляем первую пустую ячейку
                await _googleSheetsService.UpdateFirstEmptyCellInColumnAsync();
                return Ok("Первая пустая ячейка успешно обновлена с текущей датой и временем.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка при обновлении ячейки: {ex.Message}");
            }
        }
    }
}

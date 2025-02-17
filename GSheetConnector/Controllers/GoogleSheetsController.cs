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
    }
}

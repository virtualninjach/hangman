using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Hangman.Models;
using Hangman.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hangman.Pages.Rooms
{
    public class IndexModel : PageModel
    {
        private readonly IGameRoomServiceAsync _gameRoomService;

        public IndexModel(IGameRoomServiceAsync gameRoomService)
        {
            _gameRoomService = gameRoomService;
        }

        public IEnumerable<GameRoom> GameRooms { get; set; } = null!;

        [TempData]
        public string Message { get; set; } = null!;

        public async Task OnGetAsync()
        {
            GameRooms = await _gameRoomService.GetAll();
        }

        // ADD THIS METHOD
        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            try
            {
                var deleted = await _gameRoomService.Delete(id);
                
                if (deleted)
                {
                    Message = "Room deleted successfully!";
                }
                else
                {
                    Message = "Room not found.";
                }
            }
            catch (Exception ex)
            {
                Message = $"Error deleting room: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
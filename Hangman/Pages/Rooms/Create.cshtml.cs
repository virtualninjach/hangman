using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Hangman.DTOs;
using Hangman.Models;
using Hangman.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hangman.Pages.Rooms
{
    public class CreateModel : PageModel
    {
        private readonly IGameRoomServiceAsync _gameRoomService;

        public CreateModel(IGameRoomServiceAsync gameRoomService)
        {
            _gameRoomService = gameRoomService;
        }

        [BindProperty]
        [Required(ErrorMessage = "Room name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Room name must be between 3 and 100 characters")]
        public string RoomName { get; set; } = null!;

        public GameRoom? CreatedRoom { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var gameRoomDTO = new GameRoomDTO { Name = RoomName };
            CreatedRoom = await _gameRoomService.Create(gameRoomDTO);

            return Page();
        }
    }
}
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Hangman.Models;
using Hangman.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hangman.Pages.Players
{
    public class CreateModel : PageModel
    {
        private readonly IPlayerServiceAsync _playerService;

        public CreateModel(IPlayerServiceAsync playerService)
        {
            _playerService = playerService;
        }

        [BindProperty]
        [Required(ErrorMessage = "Player name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string PlayerName { get; set; } = null!;

        public Player? CreatedPlayer { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var newPlayerData = new NewPlayerData { Name = PlayerName };
            CreatedPlayer = await _playerService.Create(newPlayerData);

            // Store player ID in session for convenience
            HttpContext.Session.SetString("PlayerId", CreatedPlayer.Id.ToString());
            HttpContext.Session.SetString("PlayerName", CreatedPlayer.Name);

            return Page();
        }
    }
}
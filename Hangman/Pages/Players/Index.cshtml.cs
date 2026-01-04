using System.Collections.Generic;
using System.Threading.Tasks;
using Hangman.Models;
using Hangman.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hangman.Pages.Players
{
    public class IndexModel : PageModel
    {
        private readonly IPlayerServiceAsync _playerService;

        public IndexModel(IPlayerServiceAsync playerService)
        {
            _playerService = playerService;
        }

        public IEnumerable<Player> Players { get; set; } = null!;

        public async Task OnGetAsync()
        {
            Players = await _playerService.GetAll();
        }
    }
}
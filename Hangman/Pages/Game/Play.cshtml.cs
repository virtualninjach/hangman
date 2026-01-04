using System;
using System.Threading.Tasks;
using Hangman.DTOs;
using Hangman.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hangman.Pages.Game
{
    public class PlayModel : PageModel
    {
        private readonly IGameRoomServiceAsync _gameRoomService;

        public PlayModel(IGameRoomServiceAsync gameRoomService)
        {
            _gameRoomService = gameRoomService;
        }

        public GameStateDTO? GameState { get; set; }
        public Guid RoomId { get; set; }
        public Guid WordId { get; set; }

        [BindProperty]
        public Guid PlayerId { get; set; }

        [BindProperty]
        public string GuessLetter { get; set; } = null!;

        [TempData]
        public string Message { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(Guid roomId, Guid wordId)
        {
            RoomId = roomId;
            WordId = wordId;

            try
            {
                var guessWord = await _gameRoomService.GetGuessedWord(wordId);
                if (guessWord == null)
                {
                    Message = "Game not found";
                    return Page();
                }

                var gameRound = guessWord.Round;
                var guessWordIfRoundIsOver = gameRound.IsOver ? guessWord.Word : null;

                GameState = new GameStateDTO
                {
                    GuessWord = guessWordIfRoundIsOver,
                    IsOver = gameRound.IsOver,
                    PlayerHealth = guessWord.Round.Health,
                    GuessWordSoFar = _gameRoomService.GetGuessWordStateSoFar(guessWord),
                    GuessedLetters = System.Linq.Enumerable.Select(guessWord.GuessLetters, letter => letter.Letter)
                };

                return Page();
            }
            catch (Exception ex)
            {
                Message = $"Error loading game: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(Guid roomId, Guid wordId)
        {
            if (string.IsNullOrWhiteSpace(GuessLetter) || GuessLetter.Length != 1)
            {
                Message = "Please enter a single letter!";
                return RedirectToPage(new { roomId, wordId });
            }

            var newGuessLetterDTO = new NewGuessLetterDTO
            {
                GameRoomId = roomId,
                GuessWordId = wordId,
                PlayerId = PlayerId,
                GuessLetter = GuessLetter.ToUpper()
            };

            try
            {
                var updatedGameState = await _gameRoomService.UpdateGameRoundState(newGuessLetterDTO);
                Message = "Letter submitted!";
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
            }

            return RedirectToPage(new { roomId, wordId });
        }
    }
}
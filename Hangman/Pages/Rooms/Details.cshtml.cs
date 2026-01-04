using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangman.DTOs;
using Hangman.Models;
using Hangman.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hangman.Pages.Rooms
{
    public class DetailsModel : PageModel
    {
        private readonly IGameRoomServiceAsync _gameRoomService;

        public DetailsModel(IGameRoomServiceAsync gameRoomService)
        {
            _gameRoomService = gameRoomService;
        }

        public GameRoom GameRoom { get; set; } = null!;
        public IEnumerable<GameRoomPlayer> PlayersInRoom { get; set; } = Enumerable.Empty<GameRoomPlayer>();

        [BindProperty]
        public Guid PlayerId { get; set; }

        [BindProperty]
        public string NewWord { get; set; } = null!;

        [TempData]
        public string Message { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            GameRoom = await _gameRoomService.GetById(id);
            if (GameRoom == null)
            {
                return NotFound();
            }

            PlayersInRoom = GameRoom.GameRoomPlayers?.Where(p => p.IsInRoom) ?? Enumerable.Empty<GameRoomPlayer>();
            return Page();
        }

        public async Task<IActionResult> OnPostJoinAsync(Guid id)
        {
            var joinDTO = new JoinRoomDTO
            {
                GameRoomId = id,
                PlayerId = PlayerId,
                IsHost = false
            };

            try
            {
                await _gameRoomService.JoinRoom(joinDTO);
                Message = "Successfully joined the room!";
            }
            catch (Exception ex)
            {
                Message = $"Error joining room: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostLeaveAsync(Guid id)
        {
            var leaveDTO = new LeaveRoomDTO
            {
                GameRoomId = id,
                PlayerId = PlayerId
            };

            try
            {
                await _gameRoomService.LeaveRoom(leaveDTO);
                Message = "Successfully left the room!";
            }
            catch (Exception ex)
            {
                Message = $"Error leaving room: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostCreateWordAsync(Guid id)
        {
            if (string.IsNullOrWhiteSpace(NewWord))
            {
                Message = "Please enter a word!";
                return RedirectToPage(new { id });
            }

            var wordDTO = new GuessWordDTO
            {
                GameRoomId = id,
                PlayerId = PlayerId,
                GuessWord = NewWord
            };

            try
            {
                await _gameRoomService.CreateGuessWord(wordDTO);
                Message = "Word created successfully!";
            }
            catch (Exception ex)
            {
                Message = $"Error creating word: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }
    }
}
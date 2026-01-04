using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangman.Services;
using Hangman.DTOs;
using Hangman.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hangman.Controllers.V1
{
    /// <summary>
    /// Game room management endpoints for creating rooms, joining/leaving, and managing game rounds
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class GameRoomController : ControllerBase
    {
        private readonly IGameRoomServiceAsync _gameRoomServiceAsync;
        private readonly IPlayerServiceAsync _playerServiceAsync;
        private readonly ILogger<GameRoomController> _logger;

        public GameRoomController(IGameRoomServiceAsync gameRoomServiceAsync,
            IPlayerServiceAsync playerServiceAsync,
            ILogger<GameRoomController> logger)
        {
            _gameRoomServiceAsync = gameRoomServiceAsync;
            _playerServiceAsync = playerServiceAsync;
            _logger = logger;
        }

        /// <summary>
        /// Get a game room by ID
        /// </summary>
        /// <param name="gameRoomId">The unique identifier of the game room</param>
        /// <returns>The game room details</returns>
        /// <response code="200">Returns the game room</response>
        /// <response code="404">If the game room is not found</response>
        [HttpGet]
        [Route("{gameRoomId}")]
        [ProducesResponseType(typeof(GameRoom), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<GameRoom>> GetById(Guid gameRoomId)
        {
            _logger.LogInformation("Calling gameRoomService to get room with id: {id:l}", gameRoomId);
            var gameRoom = await _gameRoomServiceAsync.GetById(gameRoomId);

            if (gameRoom != null) return Ok(gameRoom);
            return NotFound();
        }

        /// <summary>
        /// Get all game rooms
        /// </summary>
        /// <returns>A list of all game rooms</returns>
        /// <response code="200">Returns all game rooms</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GameRoom>), 200)]
        public async Task<ActionResult<IEnumerable<GameRoom>>> All()
        {
            _logger.LogInformation("Calling gameRoomService to get all rooms...");
            var gameRooms = await _gameRoomServiceAsync.GetAll();

            _logger.LogInformation("Returning all gameRooms...");
            return Ok(gameRooms);
        }

        /// <summary>
        /// Create a new game room
        /// </summary>
        /// <param name="gameRoomDTO">Game room creation details</param>
        /// <returns>The newly created game room</returns>
        /// <response code="201">Returns the newly created game room</response>
        /// <response code="400">If the game room data is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(GameRoom), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<GameRoom>> Create(GameRoomDTO gameRoomDTO)
        {
            var gameRoom = await _gameRoomServiceAsync.Create(gameRoomDTO);
            _logger.LogInformation("New room has been created: {@gameRoom}", gameRoom);

            return CreatedAtAction(nameof(GetById), new { gameRoomId = gameRoom.Id }, gameRoom);
        }

        /// <summary>
        /// Join a game room
        /// </summary>
        /// <param name="gameRoomId">The ID of the game room to join</param>
        /// <param name="playerDTO">Player information</param>
        /// <returns>Player in room details</returns>
        /// <response code="200">Player successfully joined the room</response>
        /// <response code="400">If validation fails (player already in room, banned, etc.)</response>
        [HttpPost]
        [Route("{gameRoomId}/join")]
        [ProducesResponseType(typeof(PlayerInRoomDTO), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PlayerInRoomDTO>> JoinRoom(Guid gameRoomId, PlayerDTO playerDTO)
        {
            var joinRoomDTO = new JoinRoomDTO { GameRoomId = gameRoomId, PlayerId = playerDTO.PlayerId, IsHost = false };
            _logger.LogInformation("Start join room validation: {@joinRoomDTO}", joinRoomDTO);

            var validator = new GameRoomPlayerValidator(_gameRoomServiceAsync, _playerServiceAsync);
            var validationResult = await validator.ValidateAsync(joinRoomDTO);

            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            _logger.LogInformation("Validations were successfull, adding player to the room...");
            var playerInRoomDTO = await _gameRoomServiceAsync.JoinRoom(joinRoomDTO);

            return StatusCode(200, playerInRoomDTO);
        }

        /// <summary>
        /// Leave a game room
        /// </summary>
        /// <param name="gameRoomId">The ID of the game room to leave</param>
        /// <param name="playerDTO">Player information</param>
        /// <returns>Updated player in room details</returns>
        /// <response code="200">Player successfully left the room</response>
        /// <response code="400">If validation fails</response>
        [HttpPost]
        [Route("{gameRoomId}/leave")]
        [ProducesResponseType(typeof(PlayerInRoomDTO), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PlayerInRoomDTO>> LeaveRoom(Guid gameRoomId, PlayerDTO playerDTO)
        {
            var leaveRoomDTO = new LeaveRoomDTO { GameRoomId = gameRoomId, PlayerId = playerDTO.PlayerId };
            _logger.LogInformation("Leave room data received: {@leaveRoomDTO}", leaveRoomDTO);

            var validator = new PlayerPreviouslyInRoomValidator(_gameRoomServiceAsync, _playerServiceAsync);
            var validationResult = await validator.ValidateAsync(leaveRoomDTO);

            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            _logger.LogInformation("Validations were successfull, removing player from the room...");
            var playerInRoomDTO = await _gameRoomServiceAsync.LeaveRoom(leaveRoomDTO);

            return StatusCode(200, playerInRoomDTO);
        }

        /// <summary>
        /// Get all guess words in a game room
        /// </summary>
        /// <param name="gameRoomId">The game room ID</param>
        /// <returns>List of guess words</returns>
        /// <response code="200">Returns all guess words in the room</response>
        [HttpGet]
        [Route("{gameRoomId}/guessword")]
        [ProducesResponseType(typeof(IEnumerable<GuessWord>), 200)]
        public async Task<ActionResult<IEnumerable<GuessWord>>> GetGuessWordsInRoom(Guid gameRoomId)
        {
            _logger.LogInformation("Getting all guessed words for room {:l}", gameRoomId);
            var guessedWords = await _gameRoomServiceAsync.GetAllGuessedWords(gameRoomId);

            return Ok(guessedWords);
        }

        /// <summary>
        /// Create a new guess word in a game room (host only)
        /// </summary>
        /// <param name="gameRoomId">The game room ID</param>
        /// <param name="guessWordRequestDTO">Guess word details</param>
        /// <returns>The created guess word</returns>
        /// <response code="201">Guess word created successfully</response>
        /// <response code="400">If validation fails or player is not the host</response>
        [HttpPost]
        [Route("{gameRoomId}/guessword")]
        [ProducesResponseType(typeof(GuessWord), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<GuessWord>> CreateGuessWordInRoom(Guid gameRoomId, GuessWordRequestDTO guessWordRequestDTO)
        {
            _logger.LogInformation("New guess word creation: {@guessWordRequestDTO}", guessWordRequestDTO);
            var guessWordDTO = new GuessWordDTO
            {
                GameRoomId = gameRoomId,
                PlayerId = guessWordRequestDTO.PlayerId,
                GuessWord = guessWordRequestDTO.GuessWord
            };

            var validator = new GuessWordCreationHostValidation(_gameRoomServiceAsync, _playerServiceAsync);
            var validationResult = await validator.ValidateAsync(guessWordDTO);

            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            _logger.LogInformation("Validations were successfull, removing player from the room...");
            var guessWordResponseDTO = await _gameRoomServiceAsync.CreateGuessWord(guessWordDTO);

            return StatusCode(201, guessWordResponseDTO);
        }

        /// <summary>
        /// Get the current game state for a specific guess word
        /// </summary>
        /// <param name="gameRoomId">The game room ID</param>
        /// <param name="guessWordId">The guess word ID</param>
        /// <returns>Current game state including health, guessed letters, and word progress</returns>
        /// <response code="200">Returns the current game state</response>
        /// <response code="400">If validation fails</response>
        [HttpGet]
        [Route("{gameRoomId}/guessword/{guessWordId}")]
        [ProducesResponseType(typeof(GameStateDTO), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<GameStateDTO>> GetGuessWord(Guid gameRoomId, Guid guessWordId)
        {
            var guessWordInGuessRoomDTO = new GuessWordInGuessRoomDTO { GameRoomId = gameRoomId, GuessWordId = guessWordId };
            _logger.LogInformation("Getting game state for: {@guessWordInGuessRoomDTO}", guessWordInGuessRoomDTO);

            var validator = new GuessWordInGameRoomValidator(_gameRoomServiceAsync, _playerServiceAsync);
            var validationResult = await validator.ValidateAsync(guessWordInGuessRoomDTO);

            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            var guessWord = await _gameRoomServiceAsync.GetGuessedWord(guessWordId);
            var gameRound = guessWord!.Round;  // previously validated
            var guessWordIfRoundIsOver = gameRound.IsOver ? guessWord.Word : null;

            return Ok(new GameStateDTO
            {
                GuessWord = guessWordIfRoundIsOver,
                IsOver = gameRound.IsOver,
                PlayerHealth = guessWord.Round.Health,
                GuessWordSoFar = _gameRoomServiceAsync.GetGuessWordStateSoFar(guessWord),
                GuessedLetters = guessWord.GuessLetters.Select(letter => letter.Letter),
            });
        }

        /// <summary>
        /// Submit a letter guess for the current game round
        /// </summary>
        /// <param name="gameRoomId">The game room ID</param>
        /// <param name="guessWordId">The guess word ID</param>
        /// <param name="newGuessLetterRequestDTO">Letter guess details</param>
        /// <returns>Updated game state after the guess</returns>
        /// <response code="201">Letter submitted successfully</response>
        /// <response code="400">If validation fails or game is over</response>
        [HttpPost]
        [Route("{gameRoomId}/guessword/{guessWordId}/guessletter")]
        [ProducesResponseType(typeof(GameStateDTO), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<GameStateDTO>> CreateGuessWord(Guid gameRoomId, Guid guessWordId,
            NewGuessLetterRequestDTO newGuessLetterRequestDTO)
        {
            var newGuessLetterDTO = new NewGuessLetterDTO
            {
                GameRoomId = gameRoomId,
                GuessWordId = guessWordId,
                PlayerId = newGuessLetterRequestDTO.PlayerId,
                GuessLetter = newGuessLetterRequestDTO.GuessLetter
            };
            _logger.LogInformation("New guess letter creation: {@newGuessLetterDTO}", newGuessLetterDTO);

            var validator = new NewGuessLetterValidator(_gameRoomServiceAsync, _playerServiceAsync);
            var validationResult = await validator.ValidateAsync(newGuessLetterDTO);

            if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

            var gameStateDTO = await _gameRoomServiceAsync.UpdateGameRoundState(newGuessLetterDTO);
            return StatusCode(201, gameStateDTO);
        }

        /// <summary>
        /// Delete a game room
        /// </summary>
        /// <param name="gameRoomId">The unique identifier of the game room to delete</param>
        /// <returns>No content on success</returns>
        /// <response code="204">Game room successfully deleted</response>
        /// <response code="404">If the game room is not found</response>
        [HttpDelete]
        [Route("{gameRoomId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid gameRoomId)
        {
            _logger.LogInformation("Delete request received for game room: {GameRoomId}", gameRoomId);
            
            var deleted = await _gameRoomServiceAsync.Delete(gameRoomId);
            
            if (!deleted)
            {
                _logger.LogWarning("Game room {GameRoomId} not found for deletion", gameRoomId);
                return NotFound(new { message = $"Game room with ID {gameRoomId} not found" });
            }

            _logger.LogInformation("Game room {GameRoomId} deleted successfully", gameRoomId);
            return NoContent();
        }
    }
}
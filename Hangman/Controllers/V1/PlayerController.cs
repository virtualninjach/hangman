using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangman.Services;
using Hangman.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hangman.Controllers.V1
{
    /// <summary>
    /// Player management endpoints for creating and retrieving players
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly IPlayerServiceAsync _playerServiceAsync;
        private readonly ILogger<PlayerController> _logger;

        public PlayerController(ILogger<PlayerController> logger, IPlayerServiceAsync playerServiceAsync)
        {
            _playerServiceAsync = playerServiceAsync;
            _logger = logger;
        }

        /// <summary>
        /// Get a player by ID
        /// </summary>
        /// <param name="id">The unique identifier of the player</param>
        /// <returns>The player details</returns>
        /// <response code="200">Returns the player</response>
        /// <response code="404">If the player is not found</response>
        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(Player), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Player>> GetById(Guid id)
        {
            _logger.LogInformation("Calling playerService to get player with id: {id:l}", id);
            var player = await _playerServiceAsync.GetById(id);

            if (player != null) return Ok(player);

            return NotFound();
        }

        /// <summary>
        /// Get all players
        /// </summary>
        /// <returns>A list of all players</returns>
        /// <response code="200">Returns all players</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Player>), 200)]
        public async Task<ActionResult<IEnumerable<Player>>> All()
        {
            _logger.LogInformation("Calling playerService to get all players...");
            var players = await _playerServiceAsync.GetAll();

            _logger.LogInformation($"Returning all players: {@players}", players);
            return Ok(players);
        }

        /// <summary>
        /// Create a new player
        /// </summary>
        /// <param name="newPlayerData">Player creation details</param>
        /// <returns>The newly created player</returns>
        /// <response code="201">Returns the newly created player</response>
        /// <response code="400">If the player data is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(Player), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Player>> Create(NewPlayerData newPlayerData)
        {
            var player = await _playerServiceAsync.Create(newPlayerData);
            _logger.LogInformation("New player has been created, name: {name}", player.Name);

            return CreatedAtAction(nameof(GetById), new { id = player.Id }, player);
        }
    }
}
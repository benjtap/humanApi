// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaieApi.DTOs;
using PaieApi.Services;
using PaieApi.Services.PaieApi.Services;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthService authService,
        JwtService jwtService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Inscription d'un nouvel utilisateur
    /// </summary>
    [HttpPost("inscription")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Inscription([FromBody] InscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Telephone))
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Message = "Username et téléphone requis"
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation($"Tentative d'inscription: {request.Username} depuis {ipAddress}");

        var (succes, message) = await _authService.Inscription(
            request.Username,
            request.Telephone
        );

        if (succes)
        {
            _logger.LogInformation($"Inscription réussie: {request.Username}");
        }

        return Ok(new AuthResponse
        {
            Succes = succes,
            Message = message
        });
    }

    /// <summary>
    /// Inscription d'un nouvel utilisateur (Israël)
    /// </summary>
    [HttpPost("InscriptionIsrael")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> InscriptionIsrael([FromBody] InscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Telephone))
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Message = "Username et téléphone requis"
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation($"Tentative d'inscription (IL): {request.Username} depuis {ipAddress}");

        var (succes, message) = await _authService.InscriptionIsrael(
            request.Username,
            request.Telephone
        );

        if (succes)
        {
            _logger.LogInformation($"Inscription (IL) réussie: {request.Username}");
        }

        return Ok(new AuthResponse
        {
            Succes = succes,
            Message = message
        });
    }




    /// <summary>
    /// Renvoyer un code de vérification pour terminer l'inscription
    /// </summary>
    [HttpPost("inscription/renvoyer-code")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    public async Task<IActionResult> RenvoyerCodeInscription([FromBody] ConnexionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Message = "Username requis"
            });
        }

        var (succes, message) = await _authService.RenvoyerCodeVerification(request.Username);

        return Ok(new AuthResponse
        {
            Succes = succes,
            Message = message
        });
    }

    /// <summary>
    /// Vérification du code OTP après inscription
    /// </summary>
    [HttpPost("inscription/verifier")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifierInscription([FromBody] VerificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Message = "Username et code requis"
            });
        }

        var (succes, message, utilisateur) = await _authService.VerifierInscription(
            request.Username,
            request.Code
        );

        if (!succes)
        {
            return Ok(new AuthResponse
            {
                Succes = false,
                Message = message
            });
        }

        _logger.LogInformation($"Téléphone vérifié: {request.Username}");

        // Générer le token pour connecter directement l'utilisateur
        var token = _jwtService.GenerateToken(utilisateur);
        var utilisateurDto = await _authService.ObtenirUtilisateurDto(request.Username);

        return Ok(new AuthResponse
        {
            Succes = true,
            Message = message,
            Data = new ConnexionResponse
            {
                Token = token,
                Utilisateur = utilisateurDto
            }
        });
    }

    /// <summary>
    /// Demander un code OTP pour connexion
    /// </summary>
    [HttpPost("connexion/demander")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DemanderConnexion([FromBody] ConnexionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Message = "Username requis"
            });
        }

        var (succes, message, utilisateur) = await _authService.DemanderConnexion(request.Username);

        if (succes && utilisateur != null)
        {
             // Connexion directe réussie (car déjà vérifié)
             var token = _jwtService.GenerateToken(utilisateur);
             var utilisateurDto = await _authService.ObtenirUtilisateurDto(request.Username);

             return Ok(new AuthResponse
             {
                 Succes = true,
                 Message = "Connexion reussie (Vérifié)",
                 Data = new ConnexionResponse
                 {
                     Token = token,
                     Utilisateur = utilisateurDto
                 }
             });
        }

        return Ok(new AuthResponse
        {
            Succes = succes,
            Message = message
        });
    }

    /// <summary>
    /// Connexion avec code OTP - retourne un JWT
    /// </summary>
    [HttpPost("connexion/verifier")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Connexion([FromBody] VerificationConnexionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new AuthResponse
            {
                Succes = false,
                Message = "Username et code requis"
            });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (succes, message, utilisateur) = await _authService.Connexion(
            request.Username,
            request.Code,
            ipAddress
        );

        if (!succes)
        {
            return Ok(new AuthResponse
            {
                Succes = false,
                Message = message
            });
        }

        // Générer le JWT
        var token = _jwtService.GenerateToken(utilisateur);

        var utilisateurDto = await _authService.ObtenirUtilisateurDto(request.Username);

        _logger.LogInformation($"Connexion réussie: {request.Username}");

        return Ok(new AuthResponse
        {
            Succes = true,
            Message = message,
            Data = new ConnexionResponse
            {
                Token = token,
                Utilisateur = utilisateurDto
            }
        });
    }

    /// <summary>
    /// Obtenir le profil de l'utilisateur connecté (protégé)
    /// </summary>
    [Authorize]
    [HttpGet("profil")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ObtenirProfil()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var utilisateur = await _authService.ObtenirUtilisateurDto(username);

        if (utilisateur == null)
        {
            return NotFound(new AuthResponse
            {
                Succes = false,
                Message = "Utilisateur introuvable"
            });
        }

        return Ok(new AuthResponse
        {
            Succes = true,
            Message = "Profil récupéré",
            Data = utilisateur
        });
    }

    /// <summary>
    /// Liste des utilisateurs (ADMIN - à protéger davantage en production)
    /// </summary>
    [Authorize]
    [HttpGet("utilisateurs")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    public async Task<IActionResult> ObtenirUtilisateurs()
    {
        var utilisateurs = await _authService.ObtenirTousUtilisateurs();

        var utilisateursDto = utilisateurs.Select(u => new UtilisateurDto
        {
            Id = u.Id,
            Username = u.Username,
            Telephone = u.Telephone,
            TelephoneVerifie = u.TelephoneVerifie,
            DateCreation = u.DateCreation,
            DerniereConnexion = u.DerniereConnexion
        }).ToList();

        return Ok(new AuthResponse
        {
            Succes = true,
            Message = $"{utilisateurs.Count} utilisateurs trouvés",
            Data = utilisateursDto
        });
    }

    /// <summary>
    /// Historique des connexions
    /// </summary>
    [Authorize]
    [HttpGet("historique")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    public async Task<IActionResult> ObtenirHistorique([FromQuery] int limite = 10)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        var historique = await _authService.ObtenirHistoriqueConnexions(username, limite);

        return Ok(new AuthResponse
        {
            Succes = true,
            Message = "Historique récupéré",
            Data = historique
        });
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaieApi.DTOs
{
    // DTOs/InscriptionRequest.cs
    public class InscriptionRequest
    {
        public string Username { get; set; }

        [Required(ErrorMessage = "Le numéro de téléphone est requis")]
        public string Telephone { get; set; }
    }

    // DTOs/VerificationRequest.cs
    public class VerificationRequest
    {
        public string Username { get; set; }
        public string Code { get; set; }
    }

    // DTOs/ConnexionRequest.cs
    public class ConnexionRequest
    {
        public string Username { get; set; }
    }

    // DTOs/VerificationConnexionRequest.cs
    public class VerificationConnexionRequest
    {
        public string Username { get; set; }
        public string Code { get; set; }
    }

    // DTOs/AuthResponse.cs
    public class AuthResponse
    {
        public bool Succes { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    // DTOs/ConnexionResponse.cs
    public class ConnexionResponse
    {
        public string Token { get; set; }
        public UtilisateurDto Utilisateur { get; set; }
    }

    // DTOs/UtilisateurDto.cs
    public class UtilisateurDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Telephone { get; set; }
        public bool TelephoneVerifie { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DerniereConnexion { get; set; }
    }
}

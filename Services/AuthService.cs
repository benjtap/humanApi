using MongoDB.Driver;
using PaieApi.DTOs;
using PaieApi.Models;
using PaieApi.Utils;
using System;
using System.Linq;
using Twilio;
using Twilio.Rest.Verify.V2.Service;


namespace PaieApi.Services
{

   

    namespace PaieApi.Services
    {
        public class AuthService
        {
            private readonly MongoDbService _mongoDb;
            private readonly string _twilioAccountSid;
            private readonly string _twilioAuthToken;
            private readonly string _twilioVerifyServiceSid;

            public AuthService(
                MongoDbService mongoDb,
                string twilioAccountSid,
                string twilioAuthToken,
                string twilioVerifyServiceSid)
            {
                _mongoDb = mongoDb;
                _twilioAccountSid = twilioAccountSid;
                _twilioAuthToken = twilioAuthToken;
                _twilioVerifyServiceSid = twilioVerifyServiceSid;

                TwilioClient.Init(_twilioAccountSid, _twilioAuthToken);
            }


            public async Task<(bool succes, string message)> InscriptionIsrael(string username, string telephone)
            {
                try
                {
                    // Normaliser le numéro
                    telephone = ValidationTelephone.NormaliserNumeroIsraelien(telephone);

                    // Valider le format
                    if (!ValidationTelephone.EstNumeroIsraelienValide(telephone))
                    {
                        return (false, "Format de numéro israélien invalide. Utilisez +972501234567");
                    }

                    // Vérifier si username existe
                    var userExiste = await _mongoDb.Utilisateurs
                        .Find(u => u.Username == username)
                        .FirstOrDefaultAsync();

                    if (userExiste != null)
                    {
                        return (false, "Ce username est déjà utilisé");
                    }

                    // Vérifier si téléphone existe
                    var telExiste = await _mongoDb.Utilisateurs
                        .Find(u => u.Telephone == telephone)
                        .FirstOrDefaultAsync();

                    if (telExiste != null)
                    {
                        return (false, "Ce numéro est déjà enregistré");
                    }

                    // Créer l'utilisateur
                    var nouvelUtilisateur = new Utilisateur
                    {
                        Username = username,
                        Telephone = telephone,
                        TelephoneVerifie = false,
                        DateCreation = DateTime.UtcNow,
                        Actif = true
                    };

                    await _mongoDb.Utilisateurs.InsertOneAsync(nouvelUtilisateur);

                    // Envoyer OTP avec configuration pour Israël
                    var otpEnvoye = await EnvoyerCodeOTPIsrael(username, telephone, "inscription");

                    if (!otpEnvoye)
                    {
                        await _mongoDb.Utilisateurs.DeleteOneAsync(u => u.Username == username);
                        return (false, "Erreur lors de l'envoi du code OTP");
                    }

                    return (true, $"Compte créé ! Code envoyé au {MasquerTelephone(telephone)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur inscription: {ex.Message}");
                    return (false, "Erreur lors de l'inscription");
                }
            }



            private async Task<bool> EnvoyerCodeOTPIsrael(string username, string telephone, string type)
            {
                // MOCK POUR TEST
                if (telephone.Contains("0500000000"))
                {
                    var session = new SessionVerification
                    {
                        Username = username,
                        Telephone = telephone,
                        Type = type,
                        DateDemande = DateTime.UtcNow,
                        ExpireA = DateTime.UtcNow.AddMinutes(10),
                        Tentatives = 0
                    };
                    await _mongoDb.SessionsVerification.InsertOneAsync(session);
                    return true;
                }

                try
                {
                    var verification = VerificationResource.Create(
                        to: telephone,
                        channel: "sms",
                        locale: "he",  // Hébreu (ou "en" pour anglais)
                        pathServiceSid: _twilioVerifyServiceSid
                    );
                    
                    if (verification.Status == "pending")
                    {
                        var session = new SessionVerification
                        {
                            Username = username,
                            Telephone = telephone,
                            Type = type,
                            DateDemande = DateTime.UtcNow,
                            ExpireA = DateTime.UtcNow.AddMinutes(10),
                            Tentatives = 0
                        };

                        await _mongoDb.SessionsVerification.InsertOneAsync(session);
                        return true;
                    }

                    return false;
                }
                catch (Twilio.Exceptions.ApiException ex)
                {
                    Console.WriteLine($"Erreur Twilio pour Israël: {ex.Message}");
                    Console.WriteLine($"Code erreur: {ex.Code}");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur envoi OTP: {ex.Message}");
                    return false;
                }
            }
            // ========== INSCRIPTION ==========

            public async Task<(bool succes, string message)> Inscription(string username, string telephone)
            {
                try
                {
                    // MOCK POUR TEST
                    if (telephone.Contains("0500000000"))
                    {
                         // Vérifier si username existe
                        var userMock = await _mongoDb.Utilisateurs
                            .Find(u => u.Username == username)
                            .FirstOrDefaultAsync();

                        if (userMock == null)
                        {
                            var mockUser = new Utilisateur
                            {
                                Username = username,
                                Telephone = telephone,
                                TelephoneVerifie = false,
                                DateCreation = DateTime.UtcNow,
                                Actif = true
                            };
                            await _mongoDb.Utilisateurs.InsertOneAsync(mockUser);
                        }
                        
                         // Envoyer le code OTP (MOCK)
                        await EnvoyerCodeOTP(username, telephone, "inscription");
                        return (true, "Compte créé (Mock) ! Code OTP envoyé.");
                    }

                    // Valider le format du téléphone
                    if (!telephone.StartsWith("+"))
                    {
                        return (false, "Le téléphone doit commencer par + et l'indicatif pays (ex: +33612345678)");
                    }

                    // Vérifier si username existe
                    var userExiste = await _mongoDb.Utilisateurs
                        .Find(u => u.Username == username)
                        .FirstOrDefaultAsync();

                    if (userExiste != null)
                    {
                        return (false, "Ce username est déjà utilisé");
                    }

                    // Vérifier si téléphone existe
                    var telExiste = await _mongoDb.Utilisateurs
                        .Find(u => u.Telephone == telephone)
                        .FirstOrDefaultAsync();

                    if (telExiste != null)
                    {
                        return (false, "Ce numéro de téléphone est déjà enregistré");
                    }

                    // Créer l'utilisateur
                    var nouvelUtilisateur = new Utilisateur
                    {
                        Username = username,
                        Telephone = telephone,
                        TelephoneVerifie = false,
                        DateCreation = DateTime.UtcNow,
                        Actif = true
                    };

                    await _mongoDb.Utilisateurs.InsertOneAsync(nouvelUtilisateur);

                    // Envoyer le code OTP
                    var otpEnvoye = await EnvoyerCodeOTP(username, telephone, "inscription");

                    if (!otpEnvoye)
                    {
                        // Supprimer l'utilisateur si l'envoi échoue
                        await _mongoDb.Utilisateurs.DeleteOneAsync(u => u.Username == username);
                        return (false, "Erreur lors de l'envoi du code OTP");
                    }

                    return (true, $"Compte créé ! Code OTP envoyé au {MasquerTelephone(telephone)}");
                }
                catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    return (false, "Username ou téléphone déjà utilisé");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur inscription: {ex.Message}");
                    return (false, "Erreur lors de l'inscription");
                }
            }

            // ========== VÉRIFICATION OTP (après inscription) ==========

            public async Task<(bool succes, string message, Utilisateur utilisateur)> VerifierInscription(string username, string code)
            {
                try
                {
                    // Récupérer l'utilisateur
                    var utilisateur = await _mongoDb.Utilisateurs
                        .Find(u => u.Username == username)
                        .FirstOrDefaultAsync();

                    if (utilisateur == null)
                    {
                        return (false, "Utilisateur introuvable", null);
                    }

                    if (utilisateur.TelephoneVerifie)
                    {
                        return (false, "Téléphone déjà vérifié", null);
                    }

                    // Vérifier la session
                    var session = await _mongoDb.SessionsVerification
                        .Find(s => s.Username == username && s.Type == "inscription")
                        .SortByDescending(s => s.DateDemande)
                        .FirstOrDefaultAsync();

                    if (session == null || DateTime.UtcNow > session.ExpireA)
                    {
                        return (false, "Session expirée. Demandez un nouveau code", null);
                    }

                    // MOCK VERIFICATION
                    if (utilisateur.Telephone.Contains("0500000000"))
                    {
                        if (code == "123456") 
                        {
                            // Succès mock
                            // Marquer comme vérifié (copie du bloc success ci-dessous)
                            var update = Builders<Utilisateur>.Update
                                .Set(u => u.TelephoneVerifie, true);

                            await _mongoDb.Utilisateurs.UpdateOneAsync(
                                u => u.Username == username,
                                update
                            );
                            await _mongoDb.SessionsVerification.DeleteOneAsync(s => s.Id == session.Id);
                            utilisateur.TelephoneVerifie = true;
                            return (true, "Téléphone vérifié (Mode Test) !", utilisateur);
                        }
                    }

                    // Vérifier avec Twilio
                    var verification = VerificationCheckResource.Create(
                        to: utilisateur.Telephone,
                        code: code,
                        pathServiceSid: _twilioVerifyServiceSid
                    );

                    if (verification.Status == "approved")
                    {
                        // Marquer comme vérifié
                        var update = Builders<Utilisateur>.Update
                            .Set(u => u.TelephoneVerifie, true);

                        await _mongoDb.Utilisateurs.UpdateOneAsync(
                            u => u.Username == username,
                            update
                        );

                        // Supprimer la session
                        await _mongoDb.SessionsVerification.DeleteOneAsync(s => s.Id == session.Id);

                        // Mettre à jour l'objet utilisateur retourné
                        utilisateur.TelephoneVerifie = true;

                        return (true, "Téléphone vérifié ! Vous pouvez maintenant vous connecter", utilisateur);
                    }

                    // Incrémenter les tentatives
                    session.Tentatives++;
                    var updateSession = Builders<SessionVerification>.Update
                        .Set(s => s.Tentatives, session.Tentatives);

                    await _mongoDb.SessionsVerification.UpdateOneAsync(
                        s => s.Id == session.Id,
                        updateSession
                    );

                    // Bloquer après 5 tentatives
                    if (session.Tentatives >= 5)
                    {
                        await _mongoDb.SessionsVerification.DeleteOneAsync(s => s.Id == session.Id);
                        return (false, "Trop de tentatives. Demandez un nouveau code", null);
                    }

                    return (false, $"Code incorrect ({5 - session.Tentatives} tentatives restantes)", null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur vérification: {ex.Message}");
                    return (false, "Erreur lors de la vérification", null);
                }
            }



            // ========== RenvoyerCodeVerification ==========
            public async Task<(bool succes, string message)> RenvoyerCodeVerification(string username)
            {
                try
                {
                    var utilisateur = await _mongoDb.Utilisateurs
                        .Find(u => u.Username == username)
                        .FirstOrDefaultAsync();

                    if (utilisateur == null)
                    {
                        return (false, "Utilisateur introuvable");
                    }

                    if (utilisateur.TelephoneVerifie)
                    {
                        return (false, "Votre téléphone est déjà vérifié. Vous pouvez vous connecter.");
                    }

                    // Supprimer l'ancienne session expirée
                    await _mongoDb.SessionsVerification.DeleteManyAsync(
                        s => s.Username == username && s.Type == "inscription"
                    );

                    // Envoyer un nouveau code
                    bool envoye = await EnvoyerCodeOTP(username, utilisateur.Telephone, "inscription");

                    if (envoye)
                    {
                        return (true, $"Nouveau code envoyé au {MasquerTelephone(utilisateur.Telephone)}");
                    }

                    return (false, "Erreur lors de l'envoi du code");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur renvoie code: {ex.Message}");
                    return (false, "Erreur lors de l'envoi");
                }
            }

            public async Task<(bool succes, string message, Utilisateur utilisateur)> DemanderConnexion(string username)
            {
                try
                {
                    var utilisateur = await _mongoDb.Utilisateurs
                        .Find(u => u.Username == username && u.Actif)
                        .FirstOrDefaultAsync();

                    if (utilisateur == null)
                    {
                        return (false, "Utilisateur introuvable ou inactif", null);
                    }

                    // *** MODIFICATION: Si déjà vérifié, connexion directe ***
                    if (utilisateur.TelephoneVerifie)
                    {
                        // Mettre à jour la dernière connexion
                        var update = Builders<Utilisateur>.Update
                            .Set(u => u.DerniereConnexion, DateTime.UtcNow);

                        await _mongoDb.Utilisateurs.UpdateOneAsync(
                            u => u.Id == utilisateur.Id,
                            update
                        );

                        await EnregistrerTentative(username, utilisateur.Telephone, true, null);

                        return (true, "Connexion automatique", utilisateur);
                    }
                    else
                    {
                        // Si pas vérifié, on bloque la connexion (le frontend devra déclencher le renvoi de code inscription)
                        return (false, "Vous devez d'abord vérifier votre téléphone", null);
                    }

                    // Code inaccessible si on retourne dans le else, mais pour la structure :
                    /*
                    // Vérifier les tentatives récentes (protection anti-spam)
                    var tentativesRecentes = await _mongoDb.TentativesConnexion
                    ...
                     */
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur demande connexion: {ex.Message}");
                    return (false, "Erreur lors de la demande de connexion", null);
                }
            }

            // ========== CONNEXION ==========
            public async Task<(bool succes, string message, Utilisateur utilisateur)> Connexion(
                string username,
                string code,
                string ipAddress = null)
            {
                try
                {
                    var utilisateur = await _mongoDb.Utilisateurs
                        .Find(u => u.Username == username && u.Actif && u.TelephoneVerifie)
                        .FirstOrDefaultAsync();

                    if (utilisateur == null)
                    {
                        await EnregistrerTentative(username, null, false, ipAddress);
                        return (false, "Connexion impossible", null);
                    }

                    // Vérifier la session
                    var session = await _mongoDb.SessionsVerification
                        .Find(s => s.Username == username && s.Type == "connexion")
                        .SortByDescending(s => s.DateDemande)
                        .FirstOrDefaultAsync();

                    if (session == null || DateTime.UtcNow > session.ExpireA)
                    {
                        return (false, "Session expirée. Demandez un nouveau code", null);
                    }

                    // MOCK CONNEXION
                    if (utilisateur.Telephone.Contains("0500000000"))
                    {
                        if (code == "123456")
                        {
                             // Mettre à jour la dernière connexion
                            var update = Builders<Utilisateur>.Update
                                .Set(u => u.DerniereConnexion, DateTime.UtcNow);

                            await _mongoDb.Utilisateurs.UpdateOneAsync(
                                u => u.Id == utilisateur.Id,
                                update
                            );
                            await _mongoDb.SessionsVerification.DeleteOneAsync(s => s.Id == session.Id);
                            await EnregistrerTentative(username, utilisateur.Telephone, true, ipAddress);

                            return (true, "Connexion réussie (Mode Test)", utilisateur);
                        }
                    }

                    // Vérifier avec Twilio
                    var verification = VerificationCheckResource.Create(
                        to: utilisateur.Telephone,
                        code: code,
                        pathServiceSid: _twilioVerifyServiceSid
                    );

                    if (verification.Status == "approved")
                    {
                        // Mettre à jour la dernière connexion
                        var update = Builders<Utilisateur>.Update
                            .Set(u => u.DerniereConnexion, DateTime.UtcNow);

                        await _mongoDb.Utilisateurs.UpdateOneAsync(
                            u => u.Id == utilisateur.Id,
                            update
                        );

                        // Supprimer la session
                        await _mongoDb.SessionsVerification.DeleteOneAsync(s => s.Id == session.Id);

                        // Enregistrer tentative réussie
                        await EnregistrerTentative(username, utilisateur.Telephone, true, ipAddress);

                        return (true, "Connexion réussie", utilisateur);
                    }

                    // Incrémenter tentatives
                    session.Tentatives++;
                    await _mongoDb.SessionsVerification.UpdateOneAsync(
                        s => s.Id == session.Id,
                        Builders<SessionVerification>.Update.Set(s => s.Tentatives, session.Tentatives)
                    );

                    // Enregistrer tentative échouée
                    await EnregistrerTentative(username, utilisateur.Telephone, false, ipAddress);

                    if (session.Tentatives >= 5)
                    {
                        await _mongoDb.SessionsVerification.DeleteOneAsync(s => s.Id == session.Id);
                        return (false, "Trop de tentatives. Demandez un nouveau code", null);
                    }

                    return (false, $"Code incorrect ({5 - session.Tentatives} tentatives restantes)", null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur connexion: {ex.Message}");
                    return (false, "Erreur lors de la connexion", null);
                }
            }

            // ========== MÉTHODES UTILITAIRES ==========



            private async Task<bool> EnvoyerCodeOTP(string username, string telephone, string type)
            {
                // MOCK POUR TEST
                if (telephone.Contains("0500000000"))
                {
                    var session = new SessionVerification
                    {
                        Username = username,
                        Telephone = telephone,
                        Type = type,
                        DateDemande = DateTime.UtcNow,
                        ExpireA = DateTime.UtcNow.AddMinutes(10),
                        Tentatives = 0
                    };
                    await _mongoDb.SessionsVerification.InsertOneAsync(session);
                    return true;
                }

                try
                {
                    var verification = VerificationResource.Create(
                        to: telephone,
                        channel: "sms",
                        locale: "fr",
                        pathServiceSid: _twilioVerifyServiceSid
                    );
                    
                    if (verification.Status == "pending")
                    {
                        // Créer une session de vérification
                        var session = new SessionVerification
                        {
                            Username = username,
                            Telephone = telephone,
                            Type = type,
                            DateDemande = DateTime.UtcNow,
                            ExpireA = DateTime.UtcNow.AddMinutes(10),
                            Tentatives = 0
                        };

                        await _mongoDb.SessionsVerification.InsertOneAsync(session);
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur envoi OTP: {ex.Message}");
                    return false;
                }
            }

            private async Task EnregistrerTentative(string username, string telephone, bool succes, string ipAddress)
            {
                try
                {
                    var tentative = new TentativeConnexion
                    {
                        Username = username,
                        Telephone = telephone,
                        DateTentative = DateTime.UtcNow,
                        Succes = succes,
                        IpAddress = ipAddress
                    };

                    await _mongoDb.TentativesConnexion.InsertOneAsync(tentative);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur enregistrement tentative: {ex.Message}");
                }
            }

            public async Task<UtilisateurDto> ObtenirUtilisateurDto(string username)
            {
                var utilisateur = await _mongoDb.Utilisateurs
                    .Find(u => u.Username == username)
                    .FirstOrDefaultAsync();

                if (utilisateur == null) return null;

                return new UtilisateurDto
                {
                    Id = utilisateur.Id,
                    Username = utilisateur.Username,
                    Telephone = utilisateur.Telephone,
                    TelephoneVerifie = utilisateur.TelephoneVerifie,
                    DateCreation = utilisateur.DateCreation,
                    DerniereConnexion = utilisateur.DerniereConnexion
                };
            }

            private string MasquerTelephone(string telephone)
            {
                if (telephone.Length < 8) return telephone;
                return telephone.Substring(0, 6) + "****" + telephone.Substring(telephone.Length - 2);
            }

            // ========== MÉTHODES ADMIN ==========

            public async Task<List<Utilisateur>> ObtenirTousUtilisateurs()
            {
                return await _mongoDb.Utilisateurs
                    .Find(_ => true)
                    .ToListAsync();
            }

            public async Task<Utilisateur> ObtenirUtilisateur(string username)
            {
                return await _mongoDb.Utilisateurs
                    .Find(u => u.Username == username)
                    .FirstOrDefaultAsync();
            }

            public async Task<List<TentativeConnexion>> ObtenirHistoriqueConnexions(string username, int limite = 10)
            {
                return await _mongoDb.TentativesConnexion
                    .Find(t => t.Username == username)
                    .SortByDescending(t => t.DateTentative)
                    .Limit(limite)
                    .ToListAsync();
            }
        }
    }

}

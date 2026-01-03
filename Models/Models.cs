using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PaieApi.Models
{


    public class Utilisateur
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("telephone")]
        public string Telephone { get; set; }

        [BsonElement("telephone_verifie")]
        public bool TelephoneVerifie { get; set; }

        [BsonElement("date_creation")]
        public DateTime DateCreation { get; set; }

        [BsonElement("derniere_connexion")]
        public DateTime? DerniereConnexion { get; set; }

        [BsonElement("actif")]
        public bool Actif { get; set; }
    }

    // Collection pour suivre les tentatives de connexion
    public class TentativeConnexion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("telephone")]
        public string Telephone { get; set; }

        [BsonElement("date_tentative")]
        public DateTime DateTentative { get; set; }

        [BsonElement("succes")]
        public bool Succes { get; set; }

        [BsonElement("ip_address")]
        public string IpAddress { get; set; }
    }

    // Sessions de vérification temporaires
    public class SessionVerification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("telephone")]
        public string Telephone { get; set; }

        [BsonElement("type")] // "inscription" ou "connexion"
        public string Type { get; set; }

        [BsonElement("date_demande")]
        public DateTime DateDemande { get; set; }

        [BsonElement("expire_a")]
        public DateTime ExpireA { get; set; }

        [BsonElement("tentatives")]
        public int Tentatives { get; set; }
    }
}
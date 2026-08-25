using GestionEspaces.Application.DTOs.Agents;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.DTOs.Actifs;
using GestionEspaces.Application.DTOs.Batiments;
using GestionEspaces.Application.DTOs.Bureaux;
using GestionEspaces.Application.DTOs.Demandes;
using GestionEspaces.Application.DTOs.Sites;
using GestionEspaces.Domain.Entities;

namespace GestionEspaces.Application.Common;

/// <summary>
/// Maps domain entities to transport DTOs.
/// </summary>
public static class MappingExtensions
{
    public static AgentDto ToDto(this Agent agent) => new(
        agent.IdAgent,
        agent.Version.Length > 0 ? Convert.ToBase64String(agent.Version) : null,
        agent.Nom,
        agent.Prenom,
        agent.Matricule,
        agent.Email,
        agent.Telephone,
        agent.Fonction,
        agent.Departement,
        agent.DateEmbauche,
        agent.Image);

    public static AssignmentUseCaseResult ToResult(this AffectationPoste affectation) => new(
        affectation.IdAffectationPoste,
        affectation.IdAgent,
        affectation.IdBureau,
        affectation.DateAffectation,
        affectation.DateFin);

    public static AffectationPosteDto ToDto(this AffectationPoste affectation) => new(
        affectation.IdAffectationPoste,
        affectation.IdAgent,
        affectation.IdBureau,
        affectation.DateAffectation,
        affectation.DateFin,
        affectation.Motif);

    public static AssignmentUseCaseResult ToResult(this AffectationActif affectation) => new(
        affectation.IdAffectationActif,
        affectation.IdAgent,
        affectation.IdActif,
        affectation.DateAffectation,
        affectation.DateFin);

    public static AffectationActifDto ToDto(this AffectationActif affectation) => new(
        affectation.IdAffectationActif,
        affectation.IdAgent,
        affectation.IdActif,
        affectation.DateAffectation,
        affectation.DateFin);

    public static SiteDto ToDto(this Site site) => new(
        site.IdSite,
        site.Version.Length > 0 ? Convert.ToBase64String(site.Version) : null,
        site.Nom,
        site.Code,
        site.Adresse.Rue,
        site.Adresse.Ville,
        site.Adresse.CodePostal,
        site.Adresse.Pays,
        site.Telephone,
        site.Email,
        site.Image);

    public static BatimentDto ToDto(this Batiment batiment) => new(
        batiment.IdBatiment,
        batiment.Version.Length > 0 ? Convert.ToBase64String(batiment.Version) : null,
        batiment.Nom,
        batiment.NombreEtages,
        batiment.Superficie,
        batiment.Image,
        batiment.IdSite);

    public static BureauDto ToDto(this Bureau bureau) => new(
        bureau.IdBureau,
        bureau.Version.Length > 0 ? Convert.ToBase64String(bureau.Version) : null,
        bureau.Numero,
        bureau.Type,
        bureau.Capacite,
        bureau.Superficie,
        bureau.Etage,
        bureau.Statut,
        bureau.Image,
        bureau.IdBatiment);

    public static ActifDto ToDto(this Actif actif) => new(
        actif.IdActif,
        actif.Version.Length > 0 ? Convert.ToBase64String(actif.Version) : null,
        actif.Nom,
        actif.Type,
        actif.Marque,
        actif.Modele,
        actif.NumeroSerie,
        actif.DateAchat,
        actif.Etat,
        actif.Image);

    public static DemandeDto ToDto(this DemandeAgent demande) => new(
        demande.IdDemande,
        demande.Version.Length > 0 ? Convert.ToBase64String(demande.Version) : null,
        demande.IdAgent,
        $"{demande.Agent.Nom.ToUpperInvariant()} {demande.Agent.Prenom}",
        demande.Type,
        demande.Description,
        demande.Statut,
        demande.DateCreation,
        demande.DateTraitement,
        demande.Reponse);
}
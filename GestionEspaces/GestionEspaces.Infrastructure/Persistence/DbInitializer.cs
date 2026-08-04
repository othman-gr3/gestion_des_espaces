using GestionEspaces.Domain.Entities;
using GestionEspaces.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GestionEspaces.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(GestionEspacesDbContext context)
    {
        // Apply migrations automatically just in case
        await context.Database.MigrateAsync();

        // Clean slate to ensure the mock data is populated correctly
        context.Reservations.RemoveRange(context.Reservations);
        context.AffectationsActif.RemoveRange(context.AffectationsActif);
        context.AffectationsPoste.RemoveRange(context.AffectationsPoste);
        context.Actifs.RemoveRange(context.Actifs);
        context.Agents.RemoveRange(context.Agents);
        context.Bureaux.RemoveRange(context.Bureaux);
        context.Batiments.RemoveRange(context.Batiments);
        context.Sites.RemoveRange(context.Sites);
        await context.SaveChangesAsync();

        // 1. SITES
        var site1 = new Site(
            "Siège ONEE - Branche Électricité",
            "SIEGE",
            new AdresseSite("65, Rue Othman Ben Affane", "Casablanca", "20000", "Maroc"),
            "site_siege.jpg"
        );

        var site2 = new Site(
            "Direction Régionale de Casablanca",
            "DR-CASA",
            new AdresseSite("Bd Ahl Loughlam, BP 2590, Ain Sebaa", "Casablanca", "20250", "Maroc"),
            "site_dr_casa.jpg"
        );

        await context.Sites.AddRangeAsync(site1, site2);
        await context.SaveChangesAsync();

        // 2. BATIMENTS
        var bat1 = new Batiment("Bâtiment Direction Générale", 6, 4200f, "bat_dg.jpg", site1.IdSite);
        var bat2 = new Batiment("Bâtiment Annexe Ben Affane", 3, 1800f, "bat_annexe.jpg", site1.IdSite);
        var bat3 = new Batiment("Bâtiment Exploitation Ain Sebaa", 4, 3100f, "bat_exploit.jpg", site2.IdSite);

        await context.Batiments.AddRangeAsync(bat1, bat2, bat3);
        await context.SaveChangesAsync();

        // 3. BUREAUX
        var bur1 = new Bureau("101", "Bureau individuel", 1, 14f, 1, "bur_101.jpg", bat1.IdBatiment);
        var bur2 = new Bureau("205", "Salle de réunion", 12, 32f, 2, "bur_205.jpg", bat1.IdBatiment);
        var bur3 = new Bureau("310", "Open space", 20, 85f, 3, "bur_310.jpg", bat1.IdBatiment);
        var bur4 = new Bureau("402", "Bureau individuel", 1, 12f, 4, "bur_402.jpg", bat1.IdBatiment);
        bur4.MettreEnMaintenance();

        var bur5 = new Bureau("12", "Bureau partagé", 4, 28f, 1, "bur_12.jpg", bat2.IdBatiment);
        var bur6 = new Bureau("07", "Salle de formation", 15, 40f, 0, "bur_07.jpg", bat3.IdBatiment);
        var bur7 = new Bureau("15", "Bureau individuel", 1, 13f, 1, "bur_15.jpg", bat3.IdBatiment);

        await context.Bureaux.AddRangeAsync(bur1, bur2, bur3, bur4, bur5, bur6, bur7);
        await context.SaveChangesAsync();

        // 4. AGENTS
        var agent1 = new Agent("El Amrani", "Youssef", "ONE-4521", "y.elamrani@onee.ma", "0661234501", "Directeur Régional", "Direction Régionale Casablanca", new DateTime(2010, 3, 15), "agent1.jpg");
        var agent2 = new Agent("Benjelloun", "Salma", "ONE-4877", "s.benjelloun@onee.ma", "0661234502", "Responsable Ressources Humaines", "Direction Ressources Humaines", new DateTime(2013, 9, 1), "agent2.jpg");
        var agent3 = new Agent("Tazi", "Karim", "ONE-5102", "k.tazi@onee.ma", "0661234503", "Ingénieur Réseau", "Direction Exploitation", new DateTime(2016, 1, 20), "agent3.jpg");
        var agent4 = new Agent("Fassi", "Amina", "ONE-5390", "a.fassi@onee.ma", "0661234504", "Comptable Senior", "Direction Financière", new DateTime(2017, 6, 10), "agent4.jpg");
        var agent5 = new Agent("Idrissi", "Hamza", "ONE-5644", "h.idrissi@onee.ma", "0661234505", "Technicien Maintenance", "Direction Exploitation", new DateTime(2019, 11, 5), "agent5.jpg");
        var agent6 = new Agent("Chraibi", "Nadia", "ONE-5901", "n.chraibi@onee.ma", "0661234506", "Chargée Communication", "Division Communication", new DateTime(2021, 2, 14), "agent6.jpg");
        var agent7 = new Agent("Ouazzani", "Reda", "ONE-6120", "r.ouazzani@onee.ma", "0661234507", "Chef de Projet IT", "Direction des Systèmes d'Information", new DateTime(2022, 4, 18), "agent7.jpg");

        await context.Agents.AddRangeAsync(agent1, agent2, agent3, agent4, agent5, agent6, agent7);
        await context.SaveChangesAsync();

        // 5. ACTIFS
        var act1 = new Actif("Laptop Dell Latitude 5540", "Ordinateur portable", "Dell", "Latitude 5540", "DL5540-88231", new DateTime(2023, 2, 10), "actif1.jpg");
        var act2 = new Actif("Écran Samsung 24\"", "Écran", "Samsung", "S24R350", "SS24-77120", new DateTime(2022, 11, 5), "actif2.jpg");
        var act3 = new Actif("Imprimante HP LaserJet Pro", "Imprimante", "HP", "M404dn", "HPLJ-56412", new DateTime(2021, 7, 19), "actif3.jpg");
        act3.MarquerARepairer();

        var act4 = new Actif("Téléphone IP Cisco", "Téléphone", "Cisco", "8841", "CS8841-33290", new DateTime(2020, 5, 22), "actif4.jpg");
        var act5 = new Actif("Chaise ergonomique", "Mobilier", "Kinnarps", "Capella", "KN-9012", new DateTime(2024, 1, 15), "actif5.jpg");
        var act6 = new Actif("Badge d'accès RFID", "Sécurité", "HID", "ProxCard II", "HID-14023", new DateTime(2023, 8, 30), "actif6.jpg");
        var act7 = new Actif("Vidéoprojecteur Epson", "Matériel technique", "Epson", "EB-X41", "EP-X41-70981", new DateTime(2022, 3, 12), "actif7.jpg");

        await context.Actifs.AddRangeAsync(act1, act2, act3, act4, act5, act6, act7);
        await context.SaveChangesAsync();

        // 6. AFFECTATIONS POSTES (Seat assignments)
        // id_bureau: 1 -> bur1, id_agent: 1 -> agent1
        // id_bureau: 5 -> bur5, id_agent: 2 -> agent2
        // id_bureau: 3 -> bur3, id_agent: 3 -> agent3
        // id_bureau: 7 -> bur7, id_agent: 5 -> agent5
        // id_bureau: 3 -> bur3, id_agent: 7 -> agent7 (finished)
        agent1.AffecterAuBureau(bur1, new DateTime(2020, 1, 10));
        agent2.AffecterAuBureau(bur5, new DateTime(2021, 3, 1));
        agent3.AffecterAuBureau(bur3, new DateTime(2022, 6, 15));
        agent5.AffecterAuBureau(bur7, new DateTime(2023, 1, 5));

        var finishedOfficeAff = agent7.AffecterAuBureau(bur2, new DateTime(2022, 5, 1));
        finishedOfficeAff.Clore(new DateTime(2024, 9, 30));

        // 7. AFFECTATIONS ACTIFS (Asset assignments)
        // agent1 -> act1
        // agent1 -> act6
        // agent3 -> act2
        // agent5 -> act4
        // agent7 -> act7 (finished)
        agent1.AffecterActif(act1, new DateTime(2023, 2, 12));
        agent1.AffecterActif(act6, new DateTime(2023, 8, 30));
        agent3.AffecterActif(act2, new DateTime(2022, 11, 10));
        agent5.AffecterActif(act4, new DateTime(2020, 5, 25));

        var finishedAssetAff = agent7.AffecterActif(act7, new DateTime(2022, 3, 15));
        finishedAssetAff.Clore(new DateTime(2024, 9, 30));
        act7.MarquerHorsService();

        await context.SaveChangesAsync();
    }
}

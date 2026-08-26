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

        // Only seed an empty database — never wipe existing data
        if (context.Sites.Any())
            return;

        // 1. SITES — Siège (Casablanca) + 5 directions régionales/branches couvrant les deux
        // branches de l'ONEE (Électricité, Eau), pour donner à la recherche IA de vrais noms
        // de bâtiments/sites à reconnaître dans une requête en langage naturel.
        var site1 = new Site(
            "Siège ONEE - Branche Électricité",
            "SIEGE",
            new AdresseSite("65, Rue Othman Ben Affane", "Casablanca", "20000", "Maroc"),
            "0522123456",
            "contact.siege@onee.ma",
            "https://picsum.photos/seed/onee-siege-casablanca/400/300"
        );

        var site2 = new Site(
            "Direction Régionale de Casablanca",
            "DR-CASA",
            new AdresseSite("Bd Ahl Loughlam, BP 2590, Ain Sebaa", "Casablanca", "20250", "Maroc"),
            "0522987654",
            "dr.casa@onee.ma",
            "https://picsum.photos/seed/onee-dr-casablanca/400/300"
        );

        var site3 = new Site(
            "Direction Régionale de Rabat-Salé-Kénitra",
            "DR-RABAT",
            new AdresseSite("Avenue Al Abtal, Agdal", "Rabat", "10080", "Maroc"),
            "0537701122",
            "dr.rabat@onee.ma",
            "https://picsum.photos/seed/onee-dr-rabat/400/300"
        );

        var site4 = new Site(
            "Direction Régionale de Marrakech-Safi",
            "DR-MARRAKECH",
            new AdresseSite("Route de Casablanca, Sidi Ghanem", "Marrakech", "40000", "Maroc"),
            "0524334455",
            "dr.marrakech@onee.ma",
            "https://picsum.photos/seed/onee-dr-marrakech/400/300"
        );

        var site5 = new Site(
            "Direction Régionale de Fès-Meknès",
            "DR-FES",
            new AdresseSite("Route d'Immouzer, Route de Sefrou", "Fès", "30000", "Maroc"),
            "0535660077",
            "dr.fes@onee.ma",
            "https://picsum.photos/seed/onee-dr-fes/400/300"
        );

        var site6 = new Site(
            "Siège ONEE - Branche Eau",
            "SIEGE-EAU",
            new AdresseSite("Station de traitement, Route de Zaers", "Rabat", "10090", "Maroc"),
            "0537778899",
            "contact.eau@onee.ma",
            "https://picsum.photos/seed/onee-siege-eau/400/300"
        );

        await context.Sites.AddRangeAsync(site1, site2, site3, site4, site5, site6);
        await context.SaveChangesAsync();

        // 2. BATIMENTS
        var bat1 = new Batiment("Bâtiment Direction Générale", 6, 4200f, "bat_dg.jpg", site1.IdSite);
        var bat2 = new Batiment("Bâtiment Annexe Ben Affane", 3, 1800f, "bat_annexe.jpg", site1.IdSite);
        var bat3 = new Batiment("Bâtiment Exploitation Ain Sebaa", 4, 3100f, "bat_exploit.jpg", site2.IdSite);
        var bat4 = new Batiment("Bâtiment Direction Clientèle et Marketing", 4, 2600f, "bat_clientele.jpg", site1.IdSite);
        var bat5 = new Batiment("Bâtiment Direction des Systèmes d'Information", 3, 1900f, "bat_dsi.jpg", site1.IdSite);
        var bat6 = new Batiment("Bâtiment Distribution Électricité", 3, 2200f, "bat_distribution.jpg", site2.IdSite);
        var bat7 = new Batiment("Centre de Formation Ain Sebaa", 2, 1400f, "bat_formation.jpg", site2.IdSite);
        var bat8 = new Batiment("Bâtiment Direction Régionale Rabat", 5, 3300f, "bat_dr_rabat.jpg", site3.IdSite);
        var bat9 = new Batiment("Bâtiment Exploitation Réseau Rabat", 3, 2000f, "bat_exploit_rabat.jpg", site3.IdSite);
        var bat10 = new Batiment("Bâtiment Direction Régionale Marrakech", 4, 2800f, "bat_dr_marrakech.jpg", site4.IdSite);
        var bat11 = new Batiment("Bâtiment Clientèle Marrakech", 2, 1200f, "bat_clientele_mrk.jpg", site4.IdSite);
        var bat12 = new Batiment("Bâtiment Direction Régionale Fès", 4, 2500f, "bat_dr_fes.jpg", site5.IdSite);
        var bat13 = new Batiment("Bâtiment Direction Production Eau", 3, 2100f, "bat_production_eau.jpg", site6.IdSite);
        var bat14 = new Batiment("Bâtiment Direction Technique Eau", 3, 1900f, "bat_technique_eau.jpg", site6.IdSite);

        await context.Batiments.AddRangeAsync(bat1, bat2, bat3, bat4, bat5, bat6, bat7, bat8, bat9, bat10, bat11, bat12, bat13, bat14);
        await context.SaveChangesAsync();

        // 3. BUREAUX
        var bur1 = new Bureau("101", TypeBureau.Individuel, 1, 14f, 1, "bur_101.jpg", bat1.IdBatiment);
        var bur2 = new Bureau("205", TypeBureau.SalleReunion, 12, 32f, 2, "bur_205.jpg", bat1.IdBatiment);
        var bur3 = new Bureau("310", TypeBureau.OpenSpace, 20, 85f, 3, "bur_310.jpg", bat1.IdBatiment);
        var bur4 = new Bureau("402", TypeBureau.Individuel, 1, 12f, 4, "bur_402.jpg", bat1.IdBatiment);
        bur4.MettreEnMaintenance();

        var bur5 = new Bureau("12", TypeBureau.OpenSpace, 4, 28f, 1, "bur_12.jpg", bat2.IdBatiment);
        var bur6 = new Bureau("07", TypeBureau.SalleReunion, 15, 40f, 0, "bur_07.jpg", bat3.IdBatiment);
        var bur7 = new Bureau("15", TypeBureau.Individuel, 1, 13f, 1, "bur_15.jpg", bat3.IdBatiment);

        var bur8 = new Bureau("C-101", TypeBureau.Individuel, 1, 12f, 1, "bur_c101.jpg", bat4.IdBatiment);
        var bur9 = new Bureau("C-205", TypeBureau.SalleReunion, 8, 24f, 2, "bur_c205.jpg", bat4.IdBatiment);
        var bur10 = new Bureau("C-310", TypeBureau.OpenSpace, 10, 55f, 3, "bur_c310.jpg", bat4.IdBatiment);

        var bur11 = new Bureau("SI-101", TypeBureau.Individuel, 1, 13f, 1, "bur_si101.jpg", bat5.IdBatiment);
        var bur12 = new Bureau("SI-201", TypeBureau.OpenSpace, 8, 45f, 2, "bur_si201.jpg", bat5.IdBatiment);
        var bur13 = new Bureau("SI-301", TypeBureau.SalleReunion, 6, 20f, 3, "bur_si301.jpg", bat5.IdBatiment);

        var bur14 = new Bureau("DE-101", TypeBureau.Individuel, 1, 12f, 1, "bur_de101.jpg", bat6.IdBatiment);
        var bur15 = new Bureau("DE-102", TypeBureau.Individuel, 1, 12f, 1, "bur_de102.jpg", bat6.IdBatiment);
        var bur16 = new Bureau("DE-201", TypeBureau.OpenSpace, 15, 60f, 2, "bur_de201.jpg", bat6.IdBatiment);
        bur16.MettreEnMaintenance();

        var bur17 = new Bureau("CF-101", TypeBureau.SalleReunion, 20, 70f, 1, "bur_cf101.jpg", bat7.IdBatiment);
        var bur18 = new Bureau("CF-102", TypeBureau.SalleReunion, 15, 50f, 1, "bur_cf102.jpg", bat7.IdBatiment);

        var bur19 = new Bureau("RB-101", TypeBureau.Individuel, 1, 14f, 1, "bur_rb101.jpg", bat8.IdBatiment);
        var bur20 = new Bureau("RB-102", TypeBureau.Individuel, 1, 12f, 1, "bur_rb102.jpg", bat8.IdBatiment);
        var bur21 = new Bureau("RB-205", TypeBureau.SalleReunion, 10, 30f, 2, "bur_rb205.jpg", bat8.IdBatiment);
        var bur22 = new Bureau("RB-310", TypeBureau.OpenSpace, 18, 75f, 3, "bur_rb310.jpg", bat8.IdBatiment);

        var bur23 = new Bureau("ER-101", TypeBureau.Individuel, 1, 11f, 1, "bur_er101.jpg", bat9.IdBatiment);
        var bur24 = new Bureau("ER-201", TypeBureau.OpenSpace, 12, 48f, 2, "bur_er201.jpg", bat9.IdBatiment);

        var bur25 = new Bureau("MR-101", TypeBureau.Individuel, 1, 13f, 1, "bur_mr101.jpg", bat10.IdBatiment);
        var bur26 = new Bureau("MR-205", TypeBureau.SalleReunion, 8, 25f, 2, "bur_mr205.jpg", bat10.IdBatiment);
        var bur27 = new Bureau("MR-310", TypeBureau.OpenSpace, 16, 65f, 3, "bur_mr310.jpg", bat10.IdBatiment);
        bur27.MettreEnMaintenance();

        var bur28 = new Bureau("CM-101", TypeBureau.Individuel, 1, 12f, 1, "bur_cm101.jpg", bat11.IdBatiment);
        var bur29 = new Bureau("CM-102", TypeBureau.OpenSpace, 6, 30f, 1, "bur_cm102.jpg", bat11.IdBatiment);

        var bur30 = new Bureau("FS-101", TypeBureau.Individuel, 1, 14f, 1, "bur_fs101.jpg", bat12.IdBatiment);
        var bur31 = new Bureau("FS-205", TypeBureau.SalleReunion, 10, 28f, 2, "bur_fs205.jpg", bat12.IdBatiment);
        var bur32 = new Bureau("FS-310", TypeBureau.OpenSpace, 14, 58f, 3, "bur_fs310.jpg", bat12.IdBatiment);

        var bur33 = new Bureau("PE-101", TypeBureau.Individuel, 1, 13f, 1, "bur_pe101.jpg", bat13.IdBatiment);
        var bur34 = new Bureau("PE-201", TypeBureau.OpenSpace, 10, 42f, 2, "bur_pe201.jpg", bat13.IdBatiment);

        var bur35 = new Bureau("TE-101", TypeBureau.Individuel, 1, 12f, 1, "bur_te101.jpg", bat14.IdBatiment);
        var bur36 = new Bureau("TE-205", TypeBureau.SalleReunion, 8, 24f, 2, "bur_te205.jpg", bat14.IdBatiment);

        await context.Bureaux.AddRangeAsync(
            bur1, bur2, bur3, bur4, bur5, bur6, bur7, bur8, bur9, bur10,
            bur11, bur12, bur13, bur14, bur15, bur16, bur17, bur18, bur19, bur20,
            bur21, bur22, bur23, bur24, bur25, bur26, bur27, bur28, bur29, bur30,
            bur31, bur32, bur33, bur34, bur35, bur36);
        await context.SaveChangesAsync();

        // 4. AGENTS
        // agent1's email matches the "Agent" test login account in appsettings.json (Users section),
        // so the self-service /api/agents/me/office and /me/assets endpoints resolve to this record.
        var agent1 = new Agent("El Amrani", "Youssef", "ONE-4521", "y.elamrani@onee.ma", "0661234501", "Directeur Régional", "Direction Régionale Casablanca", new DateTime(2010, 3, 15), "https://ui-avatars.com/api/?name=Youssef+El+Amrani&background=1B4F8C&color=fff&size=128&bold=true");
        var agent2 = new Agent("Benjelloun", "Salma", "ONE-4877", "s.benjelloun@onee.ma", "0661234502", "Responsable Ressources Humaines", "Direction Ressources Humaines", new DateTime(2013, 9, 1), "agent2.jpg");
        var agent3 = new Agent("Tazi", "Karim", "ONE-5102", "k.tazi@onee.ma", "0661234503", "Ingénieur Réseau", "Direction Exploitation", new DateTime(2016, 1, 20), "agent3.jpg");
        var agent4 = new Agent("Fassi", "Amina", "ONE-5390", "a.fassi@onee.ma", "0661234504", "Comptable Senior", "Direction Financière", new DateTime(2017, 6, 10), "agent4.jpg");
        var agent5 = new Agent("Idrissi", "Hamza", "ONE-5644", "h.idrissi@onee.ma", "0661234505", "Technicien Maintenance", "Direction Exploitation", new DateTime(2019, 11, 5), "agent5.jpg");
        var agent6 = new Agent("Chraibi", "Nadia", "ONE-5901", "n.chraibi@onee.ma", "0661234506", "Chargée Communication", "Division Communication", new DateTime(2021, 2, 14), "agent6.jpg");
        var agent7 = new Agent("Ouazzani", "Reda", "ONE-6120", "r.ouazzani@onee.ma", "0661234507", "Chef de Projet IT", "Direction des Systèmes d'Information", new DateTime(2022, 4, 18), "agent7.jpg");

        var agent8 = new Agent("Alaoui", "Mehdi", "ONE-6301", "m.alaoui@onee.ma", "0661234508", "Directeur Régional", "Direction Régionale Rabat-Salé-Kénitra", new DateTime(2011, 5, 12), "agent8.jpg");
        var agent9 = new Agent("Bennani", "Fatima-Zahra", "ONE-6355", "f.bennani@onee.ma", "0661234509", "Responsable Clientèle", "Direction Clientèle et Marketing", new DateTime(2015, 9, 3), "agent9.jpg");
        var agent10 = new Agent("Berrada", "Anas", "ONE-6420", "a.berrada@onee.ma", "0661234510", "Ingénieur Systèmes d'Information", "Direction des Systèmes d'Information", new DateTime(2018, 2, 17), "agent10.jpg");
        var agent11 = new Agent("Cherkaoui", "Meryem", "ONE-6488", "m.cherkaoui@onee.ma", "0661234511", "Analyste Financier", "Direction Financière", new DateTime(2019, 6, 25), "agent11.jpg");
        var agent12 = new Agent("Doukkali", "Younes", "ONE-6512", "y.doukkali@onee.ma", "0661234512", "Technicien Distribution", "Direction Distribution Électricité", new DateTime(2020, 3, 14), "agent12.jpg");
        var agent13 = new Agent("El Fassi", "Khadija", "ONE-6577", "k.elfassi@onee.ma", "0661234513", "Formatrice", "Centre de Formation", new DateTime(2017, 11, 8), "agent13.jpg");
        var agent14 = new Agent("Guessous", "Omar", "ONE-6603", "o.guessous@onee.ma", "0661234514", "Chef de Division Exploitation", "Direction Exploitation", new DateTime(2014, 1, 22), "agent14.jpg");
        var agent15 = new Agent("Hajji", "Zineb", "ONE-6650", "z.hajji@onee.ma", "0661234515", "Juriste", "Direction Juridique", new DateTime(2016, 8, 30), "agent15.jpg");
        var agent16 = new Agent("Kabbaj", "Rachid", "ONE-6702", "r.kabbaj@onee.ma", "0661234516", "Directeur Régional", "Direction Régionale Marrakech-Safi", new DateTime(2009, 4, 5), "agent16.jpg");
        var agent17 = new Agent("Lahlou", "Souad", "ONE-6745", "s.lahlou@onee.ma", "0661234517", "Responsable RH", "Direction Ressources Humaines", new DateTime(2012, 10, 19), "agent17.jpg");
        var agent18 = new Agent("Mekouar", "Adil", "ONE-6790", "a.mekouar@onee.ma", "0661234518", "Ingénieur Réseau", "Direction Exploitation Réseau", new DateTime(2020, 7, 1), "agent18.jpg");
        var agent19 = new Agent("Naciri", "Ghita", "ONE-6833", "g.naciri@onee.ma", "0661234519", "Chargée Clientèle", "Direction Clientèle et Marketing", new DateTime(2021, 5, 16), "agent19.jpg");
        var agent20 = new Agent("Oudghiri", "Zakaria", "ONE-6870", "z.oudghiri@onee.ma", "0661234520", "Auditeur Interne", "Direction Audit et Contrôle de Gestion", new DateTime(2018, 9, 27), "agent20.jpg");
        var agent21 = new Agent("Qadiri", "Hanane", "ONE-6915", "h.qadiri@onee.ma", "0661234521", "Directrice Régionale", "Direction Régionale Fès-Meknès", new DateTime(2010, 12, 3), "agent21.jpg");
        var agent22 = new Agent("Rifai", "Mustapha", "ONE-6960", "m.rifai@onee.ma", "0661234522", "Responsable Achats", "Direction des Achats et Approvisionnements", new DateTime(2013, 3, 11), "agent22.jpg");
        var agent23 = new Agent("Sbai", "Ilham", "ONE-7002", "i.sbai@onee.ma", "0661234523", "Ingénieure Production Eau", "Direction Production Eau", new DateTime(2019, 1, 28), "agent23.jpg");
        var agent24 = new Agent("Tahiri", "Said", "ONE-7045", "s.tahiri@onee.ma", "0661234524", "Technicien Réseau Eau", "Direction Technique Eau", new DateTime(2020, 11, 9), "agent24.jpg");
        var agent25 = new Agent("Yousfi", "Nawal", "ONE-7088", "n.yousfi@onee.ma", "0661234525", "Comptable", "Direction Financière", new DateTime(2022, 2, 14), "agent25.jpg");
        var agent26 = new Agent("Zniber", "Hicham", "ONE-7130", "h.zniber@onee.ma", "0661234526", "Technicien Distribution", "Direction Distribution Électricité", new DateTime(2021, 8, 23), "agent26.jpg");
        var agent27 = new Agent("Bouzidi", "Samira", "ONE-7175", "s.bouzidi@onee.ma", "0661234527", "Assistante de Direction", "Direction Régionale Marrakech-Safi", new DateTime(2017, 4, 6), "agent27.jpg");
        var agent28 = new Agent("Chaoui", "Khalid", "ONE-7210", "k.chaoui@onee.ma", "0661234528", "Chef de Projet Eau", "Direction Production Eau", new DateTime(2015, 7, 19), "agent28.jpg");

        await context.Agents.AddRangeAsync(
            agent1, agent2, agent3, agent4, agent5, agent6, agent7, agent8, agent9, agent10,
            agent11, agent12, agent13, agent14, agent15, agent16, agent17, agent18, agent19, agent20,
            agent21, agent22, agent23, agent24, agent25, agent26, agent27, agent28);
        await context.SaveChangesAsync();

        // 5. ACTIFS — matériel de bureau classique + équipement terrain propre à un exploitant
        // électricité/eau (véhicules d'intervention, tablettes de relevé, équipement de sécurité).
        var act1 = new Actif("Laptop Dell Latitude 5540", "Ordinateur portable", "Dell", "Latitude 5540", "DL5540-88231", new DateTime(2023, 2, 10), "actif1.jpg");
        var act2 = new Actif("Écran Samsung 24\"", "Écran", "Samsung", "S24R350", "SS24-77120", new DateTime(2022, 11, 5), "actif2.jpg");
        var act3 = new Actif("Imprimante HP LaserJet Pro", "Imprimante", "HP", "M404dn", "HPLJ-56412", new DateTime(2021, 7, 19), "actif3.jpg");
        act3.MarquerARepairer();

        var act4 = new Actif("Téléphone IP Cisco", "Téléphone", "Cisco", "8841", "CS8841-33290", new DateTime(2020, 5, 22), "actif4.jpg");
        var act5 = new Actif("Chaise ergonomique", "Mobilier", "Kinnarps", "Capella", "KN-9012", new DateTime(2024, 1, 15), "actif5.jpg");
        var act6 = new Actif("Badge d'accès RFID", "Sécurité", "HID", "ProxCard II", "HID-14023", new DateTime(2023, 8, 30), "actif6.jpg");
        var act7 = new Actif("Vidéoprojecteur Epson", "Matériel technique", "Epson", "EB-X41", "EP-X41-70981", new DateTime(2022, 3, 12), "actif7.jpg");

        var act8 = new Actif("Laptop HP EliteBook 840", "Ordinateur portable", "HP", "EliteBook 840 G9", "HP840-11023", new DateTime(2023, 5, 14), "actif8.jpg");
        var act9 = new Actif("Écran Dell 27\"", "Écran", "Dell", "P2723DE", "DL27-44012", new DateTime(2022, 9, 8), "actif9.jpg");
        var act10 = new Actif("Imprimante Canon LBP", "Imprimante", "Canon", "LBP6230dw", "CN-88213", new DateTime(2021, 12, 1), "actif10.jpg");
        var act11 = new Actif("Téléphone IP Cisco", "Téléphone", "Cisco", "8845", "CS8845-55021", new DateTime(2021, 6, 15), "actif11.jpg");
        var act12 = new Actif("Tablette relevé compteurs", "Tablette terrain", "Samsung", "Galaxy Tab Active4 Pro", "SGT4-30012", new DateTime(2023, 3, 20), "actif12.jpg");
        var act13 = new Actif("Véhicule de service Dacia Dokker", "Véhicule utilitaire", "Dacia", "Dokker", "VH-70211", new DateTime(2020, 8, 10), "actif13.jpg");
        var act14 = new Actif("Casque de chantier", "Équipement sécurité", "MSA", "V-Gard", "MSA-90211", new DateTime(2023, 1, 5), "actif14.jpg");
        var act15 = new Actif("Talkie-walkie Motorola", "Radio", "Motorola", "GP340", "MT-60122", new DateTime(2019, 4, 18), "actif15.jpg");
        var act16 = new Actif("Laptop Lenovo ThinkPad", "Ordinateur portable", "Lenovo", "ThinkPad T14", "LN-T14-22013", new DateTime(2022, 6, 22), "actif16.jpg");
        var act17 = new Actif("GPS portable Garmin", "Équipement terrain", "Garmin", "GPSMAP 66i", "GM-66-11045", new DateTime(2021, 10, 30), "actif17.jpg");
        var act18 = new Actif("Multimètre de terrain", "Équipement technique", "Fluke", "117", "FL-117-33098", new DateTime(2022, 2, 11), "actif18.jpg");
        var act19 = new Actif("Vidéoprojecteur BenQ", "Matériel technique", "BenQ", "MX528", "BQ-528-40017", new DateTime(2020, 11, 25), "actif19.jpg");
        var act20 = new Actif("Chaise ergonomique", "Mobilier", "Kinnarps", "Capella", "KN-9013", new DateTime(2023, 9, 2), "actif20.jpg");
        var act21 = new Actif("Badge d'accès RFID", "Sécurité", "HID", "ProxCard II", "HID-14024", new DateTime(2023, 10, 11), "actif21.jpg");
        var act22 = new Actif("Scanner de documents", "Équipement bureau", "Fujitsu", "ScanSnap iX1600", "FJ-1600-51023", new DateTime(2022, 4, 19), "actif22.jpg");
        var act23 = new Actif("Photocopieur multifonction", "Équipement bureau", "Xerox", "WorkCentre 6515", "XR-6515-61034", new DateTime(2021, 1, 8), "actif23.jpg");
        var act24 = new Actif("Compteur d'eau intelligent (démo)", "Équipement technique", "Itron", "Cyble Enhanced", "IT-CE-71045", new DateTime(2023, 6, 30), "actif24.jpg");
        var act25 = new Actif("Détecteur de fuites", "Équipement technique", "Sewerin", "Aquaphon A150", "SW-A150-81056", new DateTime(2022, 8, 14), "actif25.jpg");
        var act26 = new Actif("Kit de premiers secours", "Équipement sécurité", "SECURIMED", "Standard Pro", "SM-KP-91067", new DateTime(2022, 5, 19), "actif26.jpg");
        var act27 = new Actif("Onduleur APC", "Équipement bureau", "APC", "Smart-UPS 1500", "APC-1500-01078", new DateTime(2021, 5, 27), "actif27.jpg");
        var act28 = new Actif("Véhicule de service Renault Kangoo", "Véhicule utilitaire", "Renault", "Kangoo Express", "VH-70212", new DateTime(2021, 3, 15), "actif28.jpg");

        await context.Actifs.AddRangeAsync(
            act1, act2, act3, act4, act5, act6, act7, act8, act9, act10,
            act11, act12, act13, act14, act15, act16, act17, act18, act19, act20,
            act21, act22, act23, act24, act25, act26, act27, act28);
        await context.SaveChangesAsync();

        // 6. AFFECTATIONS POSTES (Seat assignments)
        // Environ 40% des bureaux restent volontairement Disponible (démonstration de la
        // recherche IA) ; les techniciens de terrain (Distribution, Exploitation Réseau,
        // Technique Eau) restent volontairement sans bureau fixe, cohérent avec un métier
        // d'exploitant où une partie du personnel travaille majoritairement hors site.
        agent1.AffecterAuBureau(bur1, new DateTime(2020, 1, 10));
        agent2.AffecterAuBureau(bur5, new DateTime(2021, 3, 1));
        agent3.AffecterAuBureau(bur3, new DateTime(2022, 6, 15));
        agent5.AffecterAuBureau(bur7, new DateTime(2023, 1, 5));

        var finishedOfficeAff = agent7.AffecterAuBureau(bur2, new DateTime(2022, 5, 1));
        finishedOfficeAff.Clore(new DateTime(2024, 9, 30));
        // Closing an affectation normally frees the office back up (see CloseAffectationPosteUseCase);
        // seeding bypasses that use case, so mirror it here explicitly.
        bur2.RemettreEnService();

        agent8.AffecterAuBureau(bur19, new DateTime(2015, 1, 10));
        agent9.AffecterAuBureau(bur8, new DateTime(2016, 3, 1));
        agent10.AffecterAuBureau(bur11, new DateTime(2018, 5, 20));
        agent18.AffecterAuBureau(bur23, new DateTime(2020, 7, 3));

        // Kabbaj a d'abord occupé un bureau à Casablanca avant sa mutation comme Directeur
        // Régional à Marrakech — historique conservé pour peupler la page Historique.
        var kabbajFirstOffice = agent16.AffecterAuBureau(bur6, new DateTime(2009, 4, 5));
        kabbajFirstOffice.Clore(new DateTime(2020, 1, 15));
        bur6.RemettreEnService();
        agent16.AffecterAuBureau(bur25, new DateTime(2020, 2, 1));

        agent27.AffecterAuBureau(bur28, new DateTime(2017, 4, 8));
        agent21.AffecterAuBureau(bur30, new DateTime(2010, 12, 5));
        agent23.AffecterAuBureau(bur33, new DateTime(2019, 1, 30));

        // 7. AFFECTATIONS ACTIFS (Asset assignments)
        agent1.AffecterActif(act1, new DateTime(2023, 2, 12));
        agent1.AffecterActif(act6, new DateTime(2023, 8, 30));
        agent3.AffecterActif(act2, new DateTime(2022, 11, 10));
        agent5.AffecterActif(act4, new DateTime(2020, 5, 25));

        var finishedAssetAff = agent7.AffecterActif(act7, new DateTime(2022, 3, 15));
        finishedAssetAff.Clore(new DateTime(2024, 9, 30));
        act7.MarquerHorsService();

        agent9.AffecterActif(act8, new DateTime(2023, 5, 16));
        agent10.AffecterActif(act9, new DateTime(2022, 9, 10));
        agent8.AffecterActif(act10, new DateTime(2021, 12, 3));
        agent17.AffecterActif(act11, new DateTime(2021, 6, 17));
        agent12.AffecterActif(act12, new DateTime(2023, 3, 22));
        agent18.AffecterActif(act13, new DateTime(2020, 8, 12));
        agent26.AffecterActif(act14, new DateTime(2023, 1, 7));
        agent24.AffecterActif(act15, new DateTime(2019, 4, 20));
        agent11.AffecterActif(act16, new DateTime(2022, 6, 24));
        agent14.AffecterActif(act17, new DateTime(2021, 11, 1));
        agent12.AffecterActif(act18, new DateTime(2022, 2, 13));
        agent13.AffecterActif(act19, new DateTime(2020, 11, 27));
        agent16.AffecterActif(act20, new DateTime(2023, 9, 4));
        agent19.AffecterActif(act21, new DateTime(2023, 10, 13));
        agent15.AffecterActif(act22, new DateTime(2022, 4, 21));
        agent20.AffecterActif(act23, new DateTime(2021, 1, 10));
        agent23.AffecterActif(act24, new DateTime(2023, 7, 2));
        agent24.AffecterActif(act25, new DateTime(2022, 8, 16));
        agent28.AffecterActif(act26, new DateTime(2022, 5, 21));
        agent21.AffecterActif(act27, new DateTime(2021, 5, 29));
        agent22.AffecterActif(act28, new DateTime(2021, 3, 17));

        await context.SaveChangesAsync();
    }
}

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Actifs] (
    [IdActif] int NOT NULL IDENTITY,
    [Nom] nvarchar(200) NOT NULL,
    [Type] nvarchar(100) NULL,
    [Marque] nvarchar(100) NULL,
    [Modele] nvarchar(100) NULL,
    [NumeroSerie] nvarchar(150) NULL,
    [DateAchat] datetime2 NULL,
    [Etat] nvarchar(50) NOT NULL,
    [Image] nvarchar(500) NULL,
    CONSTRAINT [PK_Actifs] PRIMARY KEY ([IdActif])
);

CREATE TABLE [Agents] (
    [IdAgent] int NOT NULL IDENTITY,
    [Nom] nvarchar(150) NOT NULL,
    [Prenom] nvarchar(150) NOT NULL,
    [Matricule] nvarchar(100) NOT NULL,
    [Email] nvarchar(250) NULL,
    [Telephone] nvarchar(30) NULL,
    [Fonction] nvarchar(150) NULL,
    [Departement] nvarchar(150) NULL,
    [DateEmbauche] datetime2 NULL,
    [Image] nvarchar(500) NULL,
    CONSTRAINT [PK_Agents] PRIMARY KEY ([IdAgent])
);

CREATE TABLE [Sites] (
    [IdSite] int NOT NULL IDENTITY,
    [Nom] nvarchar(200) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [AdresseRue] nvarchar(250) NOT NULL,
    [AdresseVille] nvarchar(150) NOT NULL,
    [AdresseCodePostal] nvarchar(20) NOT NULL,
    [AdressePays] nvarchar(100) NOT NULL,
    [Image] nvarchar(500) NULL,
    CONSTRAINT [PK_Sites] PRIMARY KEY ([IdSite])
);

CREATE TABLE [AffectationsActif] (
    [IdAffectationActif] int NOT NULL IDENTITY,
    [IdAgent] int NOT NULL,
    [IdActif] int NOT NULL,
    [DateAffectation] datetime2 NOT NULL,
    [DateFin] datetime2 NULL,
    CONSTRAINT [PK_AffectationsActif] PRIMARY KEY ([IdAffectationActif]),
    CONSTRAINT [FK_AffectationsActif_Actifs_IdActif] FOREIGN KEY ([IdActif]) REFERENCES [Actifs] ([IdActif]),
    CONSTRAINT [FK_AffectationsActif_Agents_IdAgent] FOREIGN KEY ([IdAgent]) REFERENCES [Agents] ([IdAgent])
);

CREATE TABLE [Batiments] (
    [IdBatiment] int NOT NULL IDENTITY,
    [Nom] nvarchar(200) NOT NULL,
    [NombreEtages] int NOT NULL,
    [Superficie] real NOT NULL,
    [Image] nvarchar(500) NULL,
    [IdSite] int NOT NULL,
    CONSTRAINT [PK_Batiments] PRIMARY KEY ([IdBatiment]),
    CONSTRAINT [FK_Batiments_Sites_IdSite] FOREIGN KEY ([IdSite]) REFERENCES [Sites] ([IdSite]) ON DELETE NO ACTION
);

CREATE TABLE [Bureaux] (
    [IdBureau] int NOT NULL IDENTITY,
    [Numero] nvarchar(50) NOT NULL,
    [Type] nvarchar(100) NULL,
    [Capacite] int NOT NULL,
    [Superficie] real NOT NULL,
    [Etage] int NOT NULL,
    [Statut] int NOT NULL,
    [Image] nvarchar(500) NULL,
    [IdBatiment] int NOT NULL,
    CONSTRAINT [PK_Bureaux] PRIMARY KEY ([IdBureau]),
    CONSTRAINT [FK_Bureaux_Batiments_IdBatiment] FOREIGN KEY ([IdBatiment]) REFERENCES [Batiments] ([IdBatiment]) ON DELETE NO ACTION
);

CREATE TABLE [AffectationsPoste] (
    [IdAffectationPoste] int NOT NULL IDENTITY,
    [IdAgent] int NOT NULL,
    [IdBureau] int NOT NULL,
    [DateAffectation] datetime2 NOT NULL,
    [DateFin] datetime2 NULL,
    CONSTRAINT [PK_AffectationsPoste] PRIMARY KEY ([IdAffectationPoste]),
    CONSTRAINT [FK_AffectationsPoste_Agents_IdAgent] FOREIGN KEY ([IdAgent]) REFERENCES [Agents] ([IdAgent]),
    CONSTRAINT [FK_AffectationsPoste_Bureaux_IdBureau] FOREIGN KEY ([IdBureau]) REFERENCES [Bureaux] ([IdBureau])
);

CREATE UNIQUE INDEX [IX_Actifs_NumeroSerie] ON [Actifs] ([NumeroSerie]) WHERE [NumeroSerie] IS NOT NULL;

CREATE UNIQUE INDEX [IX_AffectationsActif_IdActif] ON [AffectationsActif] ([IdActif]) WHERE [DateFin] IS NULL;

CREATE INDEX [IX_AffectationsActif_IdAgent] ON [AffectationsActif] ([IdAgent]);

CREATE UNIQUE INDEX [IX_AffectationsPoste_IdAgent] ON [AffectationsPoste] ([IdAgent]) WHERE [DateFin] IS NULL;

CREATE UNIQUE INDEX [IX_AffectationsPoste_IdBureau] ON [AffectationsPoste] ([IdBureau]) WHERE [DateFin] IS NULL;

CREATE UNIQUE INDEX [IX_Agents_Matricule] ON [Agents] ([Matricule]);

CREATE INDEX [IX_Batiments_IdSite] ON [Batiments] ([IdSite]);

CREATE UNIQUE INDEX [IX_Bureaux_IdBatiment_Numero] ON [Bureaux] ([IdBatiment], [Numero]);

CREATE UNIQUE INDEX [IX_Sites_Code] ON [Sites] ([Code]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260729161405_InitialCreate', N'10.0.10');

COMMIT;
GO


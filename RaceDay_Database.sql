/* ============================================================
   RaceDay Database - Part 1
   SQL Server / SQL Server Management Studio (SSMS)
   ============================================================ */

USE master;
GO

IF DB_ID(N'RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* =========================
   TABLE: Users
   ========================= */
CREATE TABLE dbo.Users
(
    UserID       INT IDENTITY(1,1) NOT NULL,
    FirstName    NVARCHAR(50) NOT NULL,
    LastName     NVARCHAR(50) NOT NULL,
    Email        NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Phone        NVARCHAR(30) NULL,
    Role         NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser','Participant')),
    CreatedAt    DATETIME2 NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Users PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

/* =========================
   TABLE: Events
   ========================= */
CREATE TABLE dbo.Events
(
    EventID       INT IDENTITY(1,1) NOT NULL,
    OrganiserID   INT NOT NULL,
    Name          NVARCHAR(150) NOT NULL,
    EventDate     DATE NOT NULL,
    Location      NVARCHAR(150) NOT NULL,
    DistanceKm    DECIMAL(8,2) NOT NULL,
    Description   NVARCHAR(1000) NULL,
    Status        NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Events_Status DEFAULT ('Upcoming')
        CONSTRAINT CK_Events_Status CHECK (Status IN ('Draft','Upcoming','Open','Completed','Cancelled')),

    CONSTRAINT PK_Events PRIMARY KEY (EventID),
    CONSTRAINT CK_Events_Distance CHECK (DistanceKm > 0),
    CONSTRAINT FK_Events_Users
        FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID)
);
GO

/* =========================
   TABLE: Categories
   ========================= */
CREATE TABLE dbo.Categories
(
    CategoryID      INT IDENTITY(1,1) NOT NULL,
    EventID         INT NOT NULL,
    Name            NVARCHAR(100) NOT NULL,
    MaxParticipants INT NOT NULL,
    EntryFee        DECIMAL(10,2) NOT NULL
        CONSTRAINT DF_Categories_EntryFee DEFAULT (0),

    CONSTRAINT PK_Categories PRIMARY KEY (CategoryID),
    CONSTRAINT CK_Categories_MaxParticipants CHECK (MaxParticipants > 0),
    CONSTRAINT CK_Categories_EntryFee CHECK (EntryFee >= 0),
    CONSTRAINT UQ_Categories_Event_Name UNIQUE (EventID, Name),
    CONSTRAINT FK_Categories_Events
        FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

/* =========================
   TABLE: Routes
   ========================= */
CREATE TABLE dbo.Routes
(
    RouteID         INT IDENTITY(1,1) NOT NULL,
    EventID         INT NOT NULL,
    StartPoint      NVARCHAR(255) NOT NULL,
    FinishPoint     NVARCHAR(255) NOT NULL,
    RouteUrl        NVARCHAR(500) NULL,
    ElevationGainM  DECIMAL(10,2) NULL,
    Notes           NVARCHAR(1000) NULL,

    CONSTRAINT PK_Routes PRIMARY KEY (RouteID),
    CONSTRAINT UQ_Routes_Event UNIQUE (EventID),
    CONSTRAINT CK_Routes_Elevation CHECK (ElevationGainM IS NULL OR ElevationGainM >= 0),
    CONSTRAINT FK_Routes_Events
        FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

/* =========================
   TABLE: Enrolments
   ========================= */
CREATE TABLE dbo.Enrolments
(
    EnrolmentID INT IDENTITY(1,1) NOT NULL,
    UserID      INT NOT NULL,
    CategoryID  INT NOT NULL,
    Status      NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Enrolments_Status DEFAULT ('Registered')
        CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Registered','Confirmed','Cancelled')),
    RaceNumber  INT NULL,
    EnrolledAt  DATETIME2 NOT NULL
        CONSTRAINT DF_Enrolments_EnrolledAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentID),
    CONSTRAINT UQ_Enrolments_User_Category UNIQUE (UserID, CategoryID),
    CONSTRAINT UQ_Enrolments_RaceNumber UNIQUE (RaceNumber),
    CONSTRAINT CK_Enrolments_RaceNumber CHECK (RaceNumber IS NULL OR RaceNumber > 0),
    CONSTRAINT FK_Enrolments_Users
        FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Categories
        FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(CategoryID)
);
GO

/* =========================
   TABLE: Results
   ========================= */
CREATE TABLE dbo.Results
(
    ResultID        INT IDENTITY(1,1) NOT NULL,
    EnrolmentID     INT NOT NULL,
    FinishTime      TIME(0) NOT NULL,
    Position        INT NOT NULL,
    PaceMinPerKm    DECIMAL(8,2) NOT NULL,
    ResultStatus    NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Results_ResultStatus DEFAULT ('Finished')
        CONSTRAINT CK_Results_ResultStatus CHECK (ResultStatus IN ('Finished','DNF','DNS','DQ')),

    CONSTRAINT PK_Results PRIMARY KEY (ResultID),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentID),
    CONSTRAINT CK_Results_Position CHECK (Position > 0),
    CONSTRAINT CK_Results_Pace CHECK (PaceMinPerKm >= 0),
    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID)
);
GO

/* ============================================================
   SEED DATA
   ============================================================ */

INSERT INTO dbo.Users
    (FirstName, LastName, Email, PasswordHash, Phone, Role)
VALUES
    ('Thabo', 'Mokoena', 'thabo.organiser@raceday.co.za', 'HASH_DEMO_001', '0710000001', 'Organiser'),
    ('Lerato', 'Naidoo', 'lerato.organiser@raceday.co.za', 'HASH_DEMO_002', '0710000002', 'Organiser'),
    ('Sipho', 'Dlamini', 'sipho.participant@raceday.co.za', 'HASH_DEMO_003', '0710000003', 'Participant'),
    ('Amahle', 'Peters', 'amahle.participant@raceday.co.za', 'HASH_DEMO_004', '0710000004', 'Participant');
GO

INSERT INTO dbo.Events
    (OrganiserID, Name, EventDate, Location, DistanceKm, Description, Status)
VALUES
    (1, 'Gqeberha Coastal Run', '2026-10-18', 'Gqeberha', 10.00,
     'A community road running event along the coast.', 'Open'),
    (1, 'Nelson Mandela Bay Cycle Challenge', '2026-11-08', 'Gqeberha', 42.00,
     'Road cycling event for recreational and competitive riders.', 'Upcoming'),
    (2, 'Eastern Cape Fun Walk', '2026-11-22', 'Gqeberha', 5.00,
     'Family-friendly walking event.', 'Open');
GO

INSERT INTO dbo.Categories
    (EventID, Name, MaxParticipants, EntryFee)
VALUES
    (1, '10 km Open', 500, 150.00),
    (1, '10 km Junior', 200, 80.00),
    (2, '42 km Open', 300, 250.00),
    (2, '42 km Junior', 100, 150.00),
    (3, '5 km Open', 600, 100.00),
    (3, '5 km Family', 300, 60.00);
GO

INSERT INTO dbo.Routes
    (EventID, StartPoint, FinishPoint, RouteUrl, ElevationGainM, Notes)
VALUES
    (1, 'Kings Beach', 'Kings Beach', 'https://example.com/routes/coastal-run', 85.00,
     'Coastal out-and-back route.'),
    (2, 'Boardwalk', 'Boardwalk', 'https://example.com/routes/cycle-challenge', 420.00,
     'Road cycling loop.'),
    (3, 'Settlers Park', 'Settlers Park', 'https://example.com/routes/fun-walk', 35.00,
     'Family-friendly walking route.');
GO

INSERT INTO dbo.Enrolments
    (UserID, CategoryID, Status, RaceNumber)
VALUES
    (3, 1, 'Confirmed', 101),
    (4, 1, 'Confirmed', 102),
    (3, 3, 'Registered', 201),
    (4, 5, 'Confirmed', 301);
GO

INSERT INTO dbo.Results
    (EnrolmentID, FinishTime, Position, PaceMinPerKm, ResultStatus)
VALUES
    (1, '00:48:30', 12, 4.85, 'Finished'),
    (2, '00:55:20', 31, 5.53, 'Finished');
GO

/* =========================
   VERIFICATION QUERIES
   ========================= */
SELECT * FROM dbo.Users;
SELECT * FROM dbo.Events;
SELECT * FROM dbo.Categories;
SELECT * FROM dbo.Routes;
SELECT * FROM dbo.Enrolments;
SELECT * FROM dbo.Results;
GO

/* Relationship verification */
SELECT
    e.EventID,
    e.Name AS EventName,
    u.FirstName + ' ' + u.LastName AS Organiser,
    c.CategoryID,
    c.Name AS CategoryName,
    c.MaxParticipants,
    c.EntryFee
FROM dbo.Events e
INNER JOIN dbo.Users u ON u.UserID = e.OrganiserID
INNER JOIN dbo.Categories c ON c.EventID = e.EventID
ORDER BY e.EventID, c.CategoryID;
GO

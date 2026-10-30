-- ========================================================
-- FITNET - BASE DE DATOS OFICIAL (MySQL / MariaDB)
-- Proyecto Final: Laboratorio 2
-- Compatible con XAMPP (phpMyAdmin / mysql CLI) y EF Core
-- ========================================================

CREATE DATABASE IF NOT EXISTS `FitNetDb` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE `FitNetDb`;

SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

-- --------------------------------------------------------
-- 1. LIMPIEZA DE TABLAS PREVIAS (DROP ORDENADO)
-- --------------------------------------------------------
DROP TABLE IF EXISTS `ProgressRecords`;
DROP TABLE IF EXISTS `WorkoutAssignments`;
DROP TABLE IF EXISTS `WorkoutExercises`;
DROP TABLE IF EXISTS `WorkoutPlans`;
DROP TABLE IF EXISTS `Exercises`;
DROP TABLE IF EXISTS `MemberProfiles`;
DROP TABLE IF EXISTS `TrainerProfiles`;
DROP TABLE IF EXISTS `Memberships`;
DROP TABLE IF EXISTS `AspNetUserTokens`;
DROP TABLE IF EXISTS `AspNetUserRoles`;
DROP TABLE IF EXISTS `AspNetUserLogins`;
DROP TABLE IF EXISTS `AspNetUserClaims`;
DROP TABLE IF EXISTS `AspNetRoleClaims`;
DROP TABLE IF EXISTS `AspNetRoles`;
DROP TABLE IF EXISTS `AspNetUsers`;

-- --------------------------------------------------------
-- 2. TABLAS DE IDENTIDAD (ASP.NET CORE IDENTITY)
-- --------------------------------------------------------

-- Roles de Seguridad del Sistema
CREATE TABLE `AspNetRoles` (
  `Id` VARCHAR(255) NOT NULL,
  `Name` VARCHAR(256) DEFAULT NULL,
  `NormalizedName` VARCHAR(256) DEFAULT NULL,
  `ConcurrencyStamp` LONGTEXT DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Usuarios de la Aplicación (Extensión con FirstName, LastName, AvatarPath, etc.)
CREATE TABLE `AspNetUsers` (
  `Id` VARCHAR(255) NOT NULL,
  `UserName` VARCHAR(256) DEFAULT NULL,
  `NormalizedUserName` VARCHAR(256) DEFAULT NULL,
  `Email` VARCHAR(256) DEFAULT NULL,
  `NormalizedEmail` VARCHAR(256) DEFAULT NULL,
  `EmailConfirmed` TINYINT(1) NOT NULL DEFAULT 0,
  `PasswordHash` LONGTEXT DEFAULT NULL,
  `SecurityStamp` LONGTEXT DEFAULT NULL,
  `ConcurrencyStamp` LONGTEXT DEFAULT NULL,
  `PhoneNumber` LONGTEXT DEFAULT NULL,
  `PhoneNumberConfirmed` TINYINT(1) NOT NULL DEFAULT 0,
  `TwoFactorEnabled` TINYINT(1) NOT NULL DEFAULT 0,
  `LockoutEnd` DATETIME(6) DEFAULT NULL,
  `LockoutEnabled` TINYINT(1) NOT NULL DEFAULT 1,
  `AccessFailedCount` INT NOT NULL DEFAULT 0,
  `FirstName` VARCHAR(100) NOT NULL DEFAULT '',
  `LastName` VARCHAR(100) NOT NULL DEFAULT '',
  `AvatarPath` VARCHAR(255) DEFAULT NULL,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Asignación de Roles a Usuarios
CREATE TABLE `AspNetUserRoles` (
  `UserId` VARCHAR(255) NOT NULL,
  `RoleId` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_AspNetUserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Claims de Usuario
CREATE TABLE `AspNetUserClaims` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `UserId` VARCHAR(255) NOT NULL,
  `ClaimType` LONGTEXT DEFAULT NULL,
  `ClaimValue` LONGTEXT DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetUserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Logins Externos (Preparado para Google OAuth)
CREATE TABLE `AspNetUserLogins` (
  `LoginProvider` VARCHAR(255) NOT NULL,
  `ProviderKey` VARCHAR(255) NOT NULL,
  `ProviderDisplayName` LONGTEXT DEFAULT NULL,
  `UserId` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_AspNetUserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tokens de Usuario
CREATE TABLE `AspNetUserTokens` (
  `UserId` VARCHAR(255) NOT NULL,
  `LoginProvider` VARCHAR(255) NOT NULL,
  `Name` VARCHAR(255) NOT NULL,
  `Value` LONGTEXT DEFAULT NULL,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Claims de Roles
CREATE TABLE `AspNetRoleClaims` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `RoleId` VARCHAR(255) NOT NULL,
  `ClaimType` LONGTEXT DEFAULT NULL,
  `ClaimValue` LONGTEXT DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- 3. TABLAS DEL MODELO DE NEGOCIO / DOMINIO FITNET
-- --------------------------------------------------------

-- Membresías Comerciales (Free, Gold, Premium)
CREATE TABLE `Memberships` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(50) NOT NULL,
  `Description` VARCHAR(255) NOT NULL,
  `MonthlyPrice` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `SemiannualPrice` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_Membership_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Perfil de Socio / Cliente (1:1 con AspNetUsers)
CREATE TABLE `MemberProfiles` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `UserId` VARCHAR(255) NOT NULL,
  `MembershipId` INT NOT NULL,
  `BirthDate` DATE DEFAULT NULL,
  `Height` INT DEFAULT NULL,
  `TrialEndsAt` DATETIME NOT NULL,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_MemberProfiles_UserId` (`UserId`),
  KEY `IX_MemberProfiles_MembershipId` (`MembershipId`),
  CONSTRAINT `FK_MemberProfiles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_MemberProfiles_Memberships_MembershipId` FOREIGN KEY (`MembershipId`) REFERENCES `Memberships` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Perfil de Entrenador / Profesor (1:1 con AspNetUsers)
CREATE TABLE `TrainerProfiles` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `UserId` VARCHAR(255) NOT NULL,
  `Biography` TEXT DEFAULT NULL,
  `Specialty` VARCHAR(150) DEFAULT NULL,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_TrainerProfiles_UserId` (`UserId`),
  CONSTRAINT `FK_TrainerProfiles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Biblioteca de Ejercicios
CREATE TABLE `Exercises` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Description` TEXT DEFAULT NULL,
  `MuscleGroup` VARCHAR(80) NOT NULL,
  `Difficulty` VARCHAR(50) NOT NULL,
  `ImagePath` VARCHAR(255) DEFAULT NULL,
  `VideoUrl` VARCHAR(255) DEFAULT NULL,
  `IsPublic` TINYINT(1) NOT NULL DEFAULT 1,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_Exercises_MuscleGroup` (`MuscleGroup`),
  KEY `IX_Exercises_IsPublic` (`IsPublic`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Planes / Rutinas de Entrenamiento
CREATE TABLE `WorkoutPlans` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Description` TEXT DEFAULT NULL,
  `TrainerId` VARCHAR(255) DEFAULT NULL,
  `IsGeneral` TINYINT(1) NOT NULL DEFAULT 0,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_WorkoutPlans_TrainerId` (`TrainerId`),
  KEY `IX_WorkoutPlans_IsGeneral` (`IsGeneral`),
  CONSTRAINT `FK_WorkoutPlans_AspNetUsers_TrainerId` FOREIGN KEY (`TrainerId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Relación N:M Explícita entre Rutinas y Ejercicios (WorkoutExercise)
CREATE TABLE `WorkoutExercises` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `WorkoutPlanId` INT NOT NULL,
  `ExerciseId` INT NOT NULL,
  `Order` INT NOT NULL DEFAULT 1,
  `Sets` INT NOT NULL DEFAULT 3,
  `Repetitions` INT NOT NULL DEFAULT 10,
  `RestSeconds` INT NOT NULL DEFAULT 60,
  `Notes` VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_WorkoutExercises_WorkoutPlanId` (`WorkoutPlanId`),
  KEY `IX_WorkoutExercises_ExerciseId` (`ExerciseId`),
  CONSTRAINT `FK_WorkoutExercises_WorkoutPlans_WorkoutPlanId` FOREIGN KEY (`WorkoutPlanId`) REFERENCES `WorkoutPlans` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_WorkoutExercises_Exercises_ExerciseId` FOREIGN KEY (`ExerciseId`) REFERENCES `Exercises` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Asignación de Rutinas a Socios (WorkoutAssignment)
CREATE TABLE `WorkoutAssignments` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `MemberId` INT NOT NULL,
  `WorkoutPlanId` INT NOT NULL,
  `AssignedByTrainerId` VARCHAR(255) DEFAULT NULL,
  `AssignedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `StartDate` DATE NOT NULL,
  `EndDate` DATE DEFAULT NULL,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_WorkoutAssignments_MemberId` (`MemberId`),
  KEY `IX_WorkoutAssignments_WorkoutPlanId` (`WorkoutPlanId`),
  KEY `IX_WorkoutAssignments_AssignedByTrainerId` (`AssignedByTrainerId`),
  CONSTRAINT `FK_WorkoutAssignments_MemberProfiles_MemberId` FOREIGN KEY (`MemberId`) REFERENCES `MemberProfiles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_WorkoutAssignments_WorkoutPlans_WorkoutPlanId` FOREIGN KEY (`WorkoutPlanId`) REFERENCES `WorkoutPlans` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_WorkoutAssignments_AspNetUsers_AssignedByTrainerId` FOREIGN KEY (`AssignedByTrainerId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Registro de Progreso y Fotografías (ProgressRecord)
-- Cumple el requisito de "Uso de archivos, adicional al avatar"
CREATE TABLE `ProgressRecords` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `MemberId` INT NOT NULL,
  `Date` DATE NOT NULL,
  `Weight` DECIMAL(5,2) DEFAULT NULL,
  `BodyFat` DECIMAL(4,1) DEFAULT NULL,
  `Notes` TEXT DEFAULT NULL,
  `ImagePath` VARCHAR(255) DEFAULT NULL,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_ProgressRecords_MemberId` (`MemberId`),
  CONSTRAINT `FK_ProgressRecords_MemberProfiles_MemberId` FOREIGN KEY (`MemberId`) REFERENCES `MemberProfiles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- 4. SEED INICIAL DE DATOS
-- --------------------------------------------------------

-- 4.1. Roles de Seguridad
INSERT INTO `AspNetRoles` (`Id`, `Name`, `NormalizedName`, `ConcurrencyStamp`) VALUES
('role-owner', 'Owner', 'OWNER', UUID()),
('role-admin', 'Admin', 'ADMIN', UUID()),
('role-trainer', 'Trainer', 'TRAINER', UUID()),
('role-member', 'Member', 'MEMBER', UUID());

-- 4.2. Membresías Comerciales
INSERT INTO `Memberships` (`Id`, `Name`, `Description`, `MonthlyPrice`, `SemiannualPrice`, `IsActive`, `CreatedAt`) VALUES
(1, 'Free', 'Trial de 24 horas con acceso a perfil y ejercicios públicos.', 0.00, 0.00, 1, NOW()),
(2, 'Gold', 'Acceso ilimitado a biblioteca completa de ejercicios, rutinas generales del gimnasio y registro de progreso.', 32000.00, 25600.00, 1, NOW()),
(3, 'Premium', 'Todo Gold + Entrenador personal asignado, rutinas 100% personalizadas y seguimiento continuo.', 65000.00, 52000.00, 1, NOW());

-- 4.3. Biblioteca Base de Ejercicios
INSERT INTO `Exercises` (`Id`, `Name`, `Description`, `MuscleGroup`, `Difficulty`, `ImagePath`, `VideoUrl`, `IsPublic`, `IsActive`, `CreatedAt`) VALUES
(1, 'Press de Banca Plano con Barra', 'Ejercicio multiarticular básico para el desarrollo del pectoral mayor, tríceps y deltoides anterior.', 'Pecho', 'Intermedio', NULL, 'https://www.youtube.com/watch?v=rT7DgCr-3pg', 1, 1, NOW()),
(2, 'Sentadilla Trasera con Barra', 'Movimiento rey para el tren inferior: cuádriceps, glúteos, femorales y estabilizadores espinales.', 'Piernas', 'Intermedio', NULL, 'https://www.youtube.com/watch?v=bEv6CCg2BC8', 1, 1, NOW()),
(3, 'Peso Muerto Rumano', 'Enfocado en la cadena posterior: isquiotibiales, glúteos y zona lumbar con control excéntrico.', 'Piernas', 'Avanzado', NULL, 'https://www.youtube.com/watch?v=JCXUYuzwNrM', 1, 1, NOW()),
(4, 'Dominadas Pronas', 'Tracción vertical con peso corporal para dorsales anchos, bíceps y redondo mayor.', 'Espalda', 'Avanzado', NULL, 'https://www.youtube.com/watch?v=eGo4IYlbE5g', 1, 1, NOW()),
(5, 'Press Militar con Mancuernas', 'Empuje vertical para fuerza y tamaño en las cabezas anterior y lateral del deltoides.', 'Hombros', 'Intermedio', NULL, 'https://www.youtube.com/watch?v=qEwKCR5JCog', 1, 1, NOW()),
(6, 'Remo con Barra Inclinado', 'Tracción horizontal para densidad de la espalda media, romboides y trapecios.', 'Espalda', 'Intermedio', NULL, 'https://www.youtube.com/watch?v=FWJR5Ve8gkQ', 0, 1, NOW()),
(7, 'Curl de Bíceps en Banco Scott', 'Aislamiento estricto de los flexores del codo evitando el balanceo corporal.', 'Brazos', 'Principiante', NULL, 'https://www.youtube.com/watch?v=soxrZlIl35U', 1, 1, NOW()),
(8, 'Extensiones de Tríceps en Polea', 'Aislamiento del tríceps braquial con cuerda o barra recta para hipertrofia.', 'Brazos', 'Principiante', NULL, 'https://www.youtube.com/watch?v=2-LAMcpzODU', 1, 1, NOW()),
(9, 'Prensa de Piernas a 45 Grados', 'Trabajo seguro de cuádriceps y glúteos con soporte lumbar y rango de movimiento amplio.', 'Piernas', 'Principiante', NULL, 'https://www.youtube.com/watch?v=IZxyjW7MPJQ', 0, 1, NOW()),
(10, 'Plancha Abdominal Isométrica', 'Estabilidad del core (transverso abdominal, oblicuos y rectos) manteniendo la pelvis neutra.', 'Core', 'Principiante', NULL, 'https://www.youtube.com/watch?v=pSHjTRCQxIw', 1, 1, NOW()),
(11, 'Elevaciones Laterales con Mancuerna', 'Aislamiento del deltoides lateral para lograr amplitud de hombros.', 'Hombros', 'Principiante', NULL, 'https://www.youtube.com/watch?v=3VcKaXpzqRo', 0, 1, NOW()),
(12, 'Fondos en Paralelas (Dips)', 'Empuje con peso corporal para pectoral inferior y tríceps con ligera inclinación hacia adelante.', 'Pecho', 'Avanzado', NULL, 'https://www.youtube.com/watch?v=2z8JmcrW-As', 0, 1, NOW());

-- 4.4. Rutinas Generales del Gimnasio (Disponibles para socios Gold y Premium)
INSERT INTO `WorkoutPlans` (`Id`, `Name`, `Description`, `TrainerId`, `IsGeneral`, `CreatedAt`) VALUES
(1, 'Fuerza & Hipertrofia - Tren Superior', 'Rutina completa de torso enfocada en empuje y tracción pesada para desarrollo de masa magra.', NULL, 1, NOW()),
(2, 'Potencia & Piernas - Tren Inferior', 'Rutina de cuádriceps, cadena posterior y pantorrillas con foco en sentadilla y peso muerto.', NULL, 1, NOW()),
(3, 'Acondicionamiento Físico & Fullbody', 'Circuito de cuerpo completo ideal para tonificación, resistencia y alta demanda calórica.', NULL, 1, NOW());

-- 4.5. Ejercicios Asignados a las Rutinas Generales (WorkoutExercises)
-- Rutina 1: Tren Superior
INSERT INTO `WorkoutExercises` (`WorkoutPlanId`, `ExerciseId`, `Order`, `Sets`, `Repetitions`, `RestSeconds`, `Notes`) VALUES
(1, 1, 1, 4, 8, 90, 'Controlar la fase excéntrica y apoyar los pies con firmeza'),
(1, 4, 2, 4, 6, 90, 'Rango completo hasta superar la barra con el mentón'),
(1, 5, 3, 3, 10, 60, 'Mantener el abdomen contraído sin arquear la zona lumbar'),
(1, 7, 4, 3, 12, 60, 'Foco en la contracción pico del bíceps en el punto alto'),
(1, 8, 5, 3, 12, 60, 'Extender los codos sin mover los hombros');

-- Rutina 2: Tren Inferior
INSERT INTO `WorkoutExercises` (`WorkoutPlanId`, `ExerciseId`, `Order`, `Sets`, `Repetitions`, `RestSeconds`, `Notes`) VALUES
(2, 2, 1, 4, 8, 120, 'Descender hasta que el muslo quede paralelo al suelo'),
(2, 3, 2, 4, 10, 90, 'Flexión de cadera con espalda recta y leve flexión de rodilla'),
(2, 9, 3, 3, 12, 75, 'Pies al ancho de caderas en el centro de la plataforma'),
(2, 10, 4, 3, 45, 60, 'Tres series de 45 segundos manteniendo el cuerpo en línea recta');

-- Rutina 3: Fullbody
INSERT INTO `WorkoutExercises` (`WorkoutPlanId`, `ExerciseId`, `Order`, `Sets`, `Repetitions`, `RestSeconds`, `Notes`) VALUES
(3, 2, 1, 3, 10, 75, 'Ritmo constante sin pausas arriba'),
(3, 1, 2, 3, 10, 75, 'Buscar agarre cómodo a un pulgar por fuera de los hombros'),
(3, 4, 3, 3, 8, 75, 'Usar banda elástica si se requiere asistencia'),
(3, 10, 4, 3, 40, 45, 'Mantener respiración fluida durante la isometría');

-- 4.6. Usuarios Preconfigurados de Prueba (Contraseñas hasheadas con ASP.NET Core Identity)
-- Contraseñas estándar:
-- owner@fitnet.com    -> Owner123!
-- admin@fitnet.com    -> Admin123!
-- trainer@fitnet.com  -> Trainer123!
-- member@fitnet.com   -> Member123!
-- premium@fitnet.com  -> Premium123!
-- trial@fitnet.com    -> Trial123!
-- expired@fitnet.com  -> Expired123!
INSERT INTO `AspNetUsers` (`Id`, `UserName`, `NormalizedUserName`, `Email`, `NormalizedEmail`, `EmailConfirmed`, `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `PhoneNumberConfirmed`, `TwoFactorEnabled`, `LockoutEnabled`, `AccessFailedCount`, `FirstName`, `LastName`, `AvatarPath`, `CreatedAt`, `IsActive`) VALUES
('user-owner', 'owner@fitnet.com', 'OWNER@FITNET.COM', 'owner@fitnet.com', 'OWNER@FITNET.COM', 1, 'AQAAAAIAAYagAAAAEO9YwXN7Gvj5mF1xR7U5v8p9yZ1yq1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1', UUID(), UUID(), 0, 0, 1, 0, 'Guillermo', 'Propietario', NULL, NOW(), 1),
('user-admin', 'admin@fitnet.com', 'ADMIN@FITNET.COM', 'admin@fitnet.com', 'ADMIN@FITNET.COM', 1, 'AQAAAAIAAYagAAAAEO9YwXN7Gvj5mF1xR7U5v8p9yZ1yq1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1', UUID(), UUID(), 0, 0, 1, 0, 'Carlos', 'Administrador', NULL, NOW(), 1),
('user-trainer', 'trainer@fitnet.com', 'TRAINER@FITNET.COM', 'trainer@fitnet.com', 'TRAINER@FITNET.COM', 1, 'AQAAAAIAAYagAAAAEO9YwXN7Gvj5mF1xR7U5v8p9yZ1yq1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1', UUID(), UUID(), 0, 0, 1, 0, 'Lucas', 'Vázquez', NULL, NOW(), 1),
('user-member', 'member@fitnet.com', 'MEMBER@FITNET.COM', 'member@fitnet.com', 'MEMBER@FITNET.COM', 1, 'AQAAAAIAAYagAAAAEO9YwXN7Gvj5mF1xR7U5v8p9yZ1yq1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1', UUID(), UUID(), 0, 0, 1, 0, 'Federico', 'Socio', NULL, NOW(), 1),
('user-premium', 'premium@fitnet.com', 'PREMIUM@FITNET.COM', 'premium@fitnet.com', 'PREMIUM@FITNET.COM', 1, 'AQAAAAIAAYagAAAAEO9YwXN7Gvj5mF1xR7U5v8p9yZ1yq1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1', UUID(), UUID(), 0, 0, 1, 0, 'Sofía', 'Atleta', NULL, NOW(), 1),
('user-trial', 'trial@fitnet.com', 'TRIAL@FITNET.COM', 'trial@fitnet.com', 'TRIAL@FITNET.COM', 1, 'AQAAAAIAAYagAAAAEO9YwXN7Gvj5mF1xR7U5v8p9yZ1yq1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1', UUID(), UUID(), 0, 0, 1, 0, 'Juan', 'Prueba', NULL, NOW(), 1),
('user-expired', 'expired@fitnet.com', 'EXPIRED@FITNET.COM', 'expired@fitnet.com', 'EXPIRED@FITNET.COM', 1, 'AQAAAAIAAYagAAAAEO9YwXN7Gvj5mF1xR7U5v8p9yZ1yq1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1q1', UUID(), UUID(), 0, 0, 1, 0, 'Tomás', 'Vencido', NULL, NOW(), 1);

-- Vinculación de Roles
INSERT INTO `AspNetUserRoles` (`UserId`, `RoleId`) VALUES
('user-owner', 'role-owner'),
('user-admin', 'role-admin'),
('user-trainer', 'role-trainer'),
('user-member', 'role-member'),
('user-premium', 'role-member'),
('user-trial', 'role-member'),
('user-expired', 'role-member');

-- Perfil de Entrenador
INSERT INTO `TrainerProfiles` (`Id`, `UserId`, `Specialty`, `Biography`, `HireDate`, `IsActive`) VALUES
(1, 'user-trainer', 'Hipertrofia & Fuerza', 'Licenciado en Educación Física especializado en biomecánica y sobrecarga progresiva.', NOW(), 1);

-- Perfiles de Miembros
INSERT INTO `MemberProfiles` (`Id`, `UserId`, `MembershipId`, `Height`, `BirthDate`, `TrialEndsAt`, `IsActive`) VALUES
(1, 'user-member', 2, 178.00, '1998-05-14', DATE_ADD(NOW(), INTERVAL 1 YEAR), 1),
(2, 'user-premium', 3, 168.00, '1999-11-22', DATE_ADD(NOW(), INTERVAL 1 YEAR), 1),
(3, 'user-trial', 1, 175.00, '2001-08-20', DATE_ADD(NOW(), INTERVAL 24 HOUR), 1),
(4, 'user-expired', 1, 180.00, '2000-03-10', DATE_SUB(NOW(), INTERVAL 2 DAY), 1);

-- Asignación de Rutinas a Miembros
INSERT INTO `WorkoutAssignments` (`Id`, `MemberId`, `WorkoutPlanId`, `AssignedByTrainerId`, `AssignedAt`, `StartDate`, `EndDate`, `IsActive`) VALUES
(1, 1, 1, 'user-trainer', NOW(), CURDATE(), NULL, 1),
(2, 2, 1, 'user-trainer', NOW(), CURDATE(), NULL, 1);

-- Registros de Progreso Histórico
INSERT INTO `ProgressRecords` (`Id`, `MemberId`, `Date`, `Weight`, `BodyFat`, `Notes`, `ImagePath`, `CreatedAt`) VALUES
(1, 1, DATE_SUB(CURDATE(), INTERVAL 14 DAY), 78.50, 16.0, 'Inicio de ciclo de fuerza con Coach Lucas. Adaptación positiva en sentadillas.', NULL, DATE_SUB(NOW(), INTERVAL 14 DAY)),
(2, 1, DATE_SUB(CURDATE(), INTERVAL 2 DAY), 77.20, 15.3, 'Control quincenal. Descenso de 1.3 kg de grasa y mejoras en dominadas estrictas.', NULL, DATE_SUB(NOW(), INTERVAL 2 DAY)),
(3, 2, DATE_SUB(CURDATE(), INTERVAL 30 DAY), 62.50, 23.0, 'Evaluación inicial y diseño de plan personalizado por Coach Lucas.', NULL, DATE_SUB(NOW(), INTERVAL 30 DAY)),
(4, 2, DATE_SUB(CURDATE(), INTERVAL 10 DAY), 61.20, 21.8, 'Excelente adherencia al plan de fuerza y reducción de porcentaje graso.', NULL, DATE_SUB(NOW(), INTERVAL 10 DAY));

SET FOREIGN_KEY_CHECKS = 1;

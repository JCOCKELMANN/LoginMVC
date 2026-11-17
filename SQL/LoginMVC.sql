
-- ============================================
-- PROYECTO: LoginMVC
-- BASE DE DATOS: DB_LoginMVC
-- ============================================

-- 1. CREAR BASE DE DATOS
CREATE DATABASE DB_LoginMVC;
GO

USE DB_LoginMVC;
GO

-- ============================================
-- 2. TABLA DE ROLES
-- ============================================

CREATE TABLE Roles (
    IdRol INT IDENTITY(1,1) PRIMARY KEY,
    NombreRol VARCHAR(50) NOT NULL UNIQUE
);
GO

-- INSERTAR ROLES INICIALES
INSERT INTO Roles (NombreRol)
VALUES
    ('Administrador'),
    ('Usuario');
GO

-- ============================================
-- 3. TABLA DE PUESTOS
-- ============================================

CREATE TABLE Puestos (
    IdPuesto INT IDENTITY(1,1) PRIMARY KEY,
    NombrePuesto VARCHAR(100) NOT NULL UNIQUE
);
GO

-- INSERTAR PUESTOS INICIALES
INSERT INTO Puestos (NombrePuesto)
VALUES
    ('Operador'),
    ('Supervisor'),
    ('Gerente'),
    ('Analista'),
    ('Soporte Técnico'),
    ('Administrativo');
GO

-- ============================================
-- 4. TABLA DE USUARIOS
-- ============================================

CREATE TABLE Usuarios (
    IdUsuario UNIQUEIDENTIFIER
        NOT NULL DEFAULT NEWID(),

    Nombre VARCHAR(100) NOT NULL,
    Apellido VARCHAR(100) NOT NULL,

    Username VARCHAR(50) NOT NULL,

    PasswordHash VARCHAR(255) NOT NULL,

    IdRol INT NOT NULL,
    IdPuesto INT NULL,

    NivelAcceso INT NOT NULL DEFAULT 1,

    Activo BIT NOT NULL DEFAULT 1,

    FechaRegistro DATETIME2 NOT NULL
        DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Usuarios
        PRIMARY KEY (IdUsuario),

    CONSTRAINT UQ_Usuarios_Username
        UNIQUE (Username),

    CONSTRAINT FK_Usuarios_Roles
        FOREIGN KEY (IdRol)
        REFERENCES Roles(IdRol),

    CONSTRAINT FK_Usuarios_Puestos
        FOREIGN KEY (IdPuesto)
        REFERENCES Puestos(IdPuesto)
);
GO

-- ============================================
-- 5. VERIFICAR INFORMACIÓN
-- ============================================

SELECT * FROM Roles;
SELECT * FROM Puestos;
SELECT * FROM Usuarios;
GO

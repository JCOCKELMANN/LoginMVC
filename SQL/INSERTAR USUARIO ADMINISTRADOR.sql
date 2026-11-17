
-- ============================================
-- 5. INSERTAR USUARIO ADMINISTRADOR
-- ============================================
USE DB_LoginMVC;
GO
INSERT INTO Usuarios (
    Nombre,
    Apellido,
    Username,
    PasswordHash,
    IdRol,
    IdPuesto,
    NivelAcceso,
    Activo
)
VALUES (
    'Administrador',
    'Sistema',
    'admin',
    'HASH_BCRYPT_PENDIENTE',
    (SELECT IdRol FROM Roles
     WHERE NombreRol = 'Administrador'),
    NULL,
    10,
    1
);
GO
